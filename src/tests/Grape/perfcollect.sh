#!/bin/bash 

#############################################################################################################
# .NET Performance Data Collection Script
#############################################################################################################

#############################################################################################################
#
# ***** HOW TO USE THIS SCRIPT *****
#
# This script can be used to collect and view performance data collected with perf_event on Linux.

# It's job is to make it simple to collect performance traces.
#
# How to collect a performance trace:
# 1. Prior to starting the .NET process, set the environment variable COMPlus_PerfMapEnabled=1.
#    This tells the runtime to emit information that enables perf_event to resolve JIT-compiled code symbols.
# 2. Setup your system to reproduce the performance issue you'd like to capture.  Data collection can be
#    started on already running processes.
# 2. Run this script: sudo ./perfcollect collect samplePerfTrace
#    This will start data collection.
# 3. Let the repro run as long as you need to capture the performance problem.
# 4. Hit CTRL+C to stop collection.
#    When collection is stopped, the script will create a trace.zip file matching the name specified on the
#    command line.  This file will contain the trace, JIT-compiled symbol information, and all debugging
#    symbols for binaries referenced by this trace that were available on the machine at collection time.
#
# How to view a performance trace:
# 1. Run this script: ./perfcollect view samplePerfTrace.trace.zip
#    This will extract the trace, place and register all symbol files and JIT-compiled symbol information
#    and start the perf_event viewer.  By default, you will be looking at a callee view - stacks are ordered
#    top down.  For a caller or bottom up view, specify '-graphtype caller'. 
#############################################################################################################

######################################
## FOR DEBUGGING ONLY
######################################
# set -x

######################################
## Collection Options
## NOTE: These values represent the collection defaults.
######################################

# Set when we parse command line arguments to determine if we should enable specific collection options.
collect_cpu=1
collect_threadTime=0

######################################
## .NET Event Categories
######################################

# Separate GCCollectOnly list because our LTTng implementation doesn't set tracepoint verbosity.
# Once tracepoint verbosity is set, we can set verbosity and collapse this wtih DotNETRuntime_GCKeyword.
declare -a DotNETRuntime_GCKeyword_GCCollectOnly=(
    DotNETRuntime:GCStart
    DotNETRuntime:GCStart_V1
    DotNETRuntime:GCStart_V2
    DotNETRuntime:GCEnd
    DotNETRuntime:GCEnd_V1
    DotNETRuntime:GCRestartEEEnd
    DotNETRuntime:GCRestartEEEnd_V1
    DotNETRuntime:GCHeapStats
    DotNETRuntime:GCHeapStats_V1
    DotNETRuntime:GCCreateSegment
    DotNETRuntime:GCCreateSegment_V1
    DotNETRuntime:GCFreeSegment
    DotNETRuntime:GCFreeSegment_V1
    DotNETRuntime:GCRestartEEBegin
    DotNETRuntime:GCRestartEEBegin_V1
    DotNETRuntime:GCSuspendEEEnd
    DotNETRuntime:GCSuspendEEEnd_V1
    DotNETRuntime:GCSuspendEEBegin
    DotNETRuntime:GCSuspendEEBegin_V1
    DotNETRuntime:GCCreateConcurrentThread
    DotNETRuntime:GCTerminateConcurrentThread
    DotNETRuntime:GCFinalizersEnd
    DotNETRuntime:GCFinalizersEnd_V1
    DotNETRuntime:GCFinalizersBegin
    DotNETRuntime:GCFinalizersBegin_V1
    DotNETRuntime:GCMarkStackRoots
    DotNETRuntime:GCMarkFinalizeQueueRoots
    DotNETRuntime:GCMarkHandles
    DotNETRuntime:GCMarkOlderGenerationRoots
    DotNETRuntime:FinalizeObject
    DotNETRuntime:GCTriggered
    DotNETRuntime:IncreaseMemoryPressure
    DotNETRuntime:DecreaseMemoryPressure
    DotNETRuntime:GCMarkWithType
    DotNETRuntime:GCPerHeapHistory_V3
    DotNETRuntime:GCGlobalHeapHistory_V2
    DotNETRuntime:GCCreateConcurrentThread_V1
    DotNETRuntime:GCTerminateConcurrentThread_V1
)


declare -a DotNETRuntime_GCKeyword=(
    DotNETRuntime:GCStart
    DotNETRuntime:GCStart_V1
    DotNETRuntime:GCStart_V2
    DotNETRuntime:GCEnd
    DotNETRuntime:GCEnd_V1
    DotNETRuntime:GCRestartEEEnd
    DotNETRuntime:GCRestartEEEnd_V1
    DotNETRuntime:GCHeapStats
    DotNETRuntime:GCHeapStats_V1
    DotNETRuntime:GCCreateSegment
    DotNETRuntime:GCCreateSegment_V1
    DotNETRuntime:GCFreeSegment
    DotNETRuntime:GCFreeSegment_V1
    DotNETRuntime:GCRestartEEBegin
    DotNETRuntime:GCRestartEEBegin_V1
    DotNETRuntime:GCSuspendEEEnd
    DotNETRuntime:GCSuspendEEEnd_V1
    DotNETRuntime:GCSuspendEEBegin
    DotNETRuntime:GCSuspendEEBegin_V1
    DotNETRuntime:GCAllocationTick
    DotNETRuntime:GCAllocationTick_V1
    DotNETRuntime:GCAllocationTick_V2
    DotNETRuntime:GCAllocationTick_V3
    DotNETRuntime:GCCreateConcurrentThread
    DotNETRuntime:GCTerminateConcurrentThread
    DotNETRuntime:GCFinalizersEnd
    DotNETRuntime:GCFinalizersEnd_V1
    DotNETRuntime:GCFinalizersBegin
    DotNETRuntime:GCFinalizersBegin_V1
    DotNETRuntime:GCMarkStackRoots
    DotNETRuntime:GCMarkFinalizeQueueRoots
    DotNETRuntime:GCMarkHandles
    DotNETRuntime:GCMarkOlderGenerationRoots
    DotNETRuntime:FinalizeObject
    DotNETRuntime:PinObjectAtGCTime
    DotNETRuntime:GCTriggered
    DotNETRuntime:IncreaseMemoryPressure
    DotNETRuntime:DecreaseMemoryPressure
    DotNETRuntime:GCMarkWithType
    DotNETRuntime:GCJoin_V2
    DotNETRuntime:GCPerHeapHistory_V3
    DotNETRuntime:GCGlobalHeapHistory_V2
    DotNETRuntime:GCCreateConcurrentThread_V1
    DotNETRuntime:GCTerminateConcurrentThread_V1
)

declare -a DotNETRuntime_TypeKeyword=(
    DotNETRuntime:BulkType
)

declare -a DotNETRuntime_GCHeapDumpKeyword=(
    DotNETRuntime:GCBulkRootEdge
    DotNETRuntime:GCBulkRootConditionalWeakTableElementEdge
    DotNETRuntime:GCBulkNode
    DotNETRuntime:GCBulkEdge
    DotNETRuntime:GCBulkRootCCW
    DotNETRuntime:GCBulkRCW
    DotNETRuntime:GCBulkRootStaticVar
)

declare -a DotNETRuntime_GCSampledObjectAllocationHighKeyword=(
    DotNETRuntime:GCSampledObjectAllocationHigh
)

declare -a DotNETRuntime_GCHeapSurvivalAndMovementKeyword=(
    DotNETRuntime:GCBulkSurvivingObjectRanges
    DotNETRuntime:GCBulkMovedObjectRanges
    DotNETRuntime:GCGenerationRange
)

declare -a DotNETRuntime_GCHandleKeyword=(
    DotNETRuntime:SetGCHandle
    DotNETRuntime:DestroyGCHandle
)

declare -a DotNETRuntime_GCSampledObjectAllocationLowKeyword=(
    DotNETRuntime:GCSampledObjectAllocationLow
)

declare -a DotNETRuntime_ThreadingKeyword=(
    DotNETRuntime:WorkerThreadCreate
    DotNETRuntime:WorkerThreadTerminate
    DotNETRuntime:WorkerThreadRetire
    DotNETRuntime:WorkerThreadUnretire
    DotNETRuntime:IOThreadCreate
    DotNETRuntime:IOThreadCreate_V1
    DotNETRuntime:IOThreadTerminate
    DotNETRuntime:IOThreadTerminate_V1
    DotNETRuntime:IOThreadRetire
    DotNETRuntime:IOThreadRetire_V1
    DotNETRuntime:IOThreadUnretire
    DotNETRuntime:IOThreadUnretire_V1
    DotNETRuntime:ThreadpoolSuspensionSuspendThread
    DotNETRuntime:ThreadpoolSuspensionResumeThread
    DotNETRuntime:ThreadPoolWorkerThreadStart
    DotNETRuntime:ThreadPoolWorkerThreadStop
    DotNETRuntime:ThreadPoolWorkerThreadRetirementStart
    DotNETRuntime:ThreadPoolWorkerThreadRetirementStop
    DotNETRuntime:ThreadPoolWorkerThreadAdjustmentSample
    DotNETRuntime:ThreadPoolWorkerThreadAdjustmentAdjustment
    DotNETRuntime:ThreadPoolWorkerThreadAdjustmentStats
    DotNETRuntime:ThreadPoolWorkerThreadWait
    DotNETRuntime:ThreadPoolWorkingThreadCount
    DotNETRuntime:ThreadPoolIOPack
    DotNETRuntime:GCCreateConcurrentThread_V1
    DotNETRuntime:GCTerminateConcurrentThread_V1
)

declare -a DotNETRuntime_ThreadingKeyword_ThreadTransferKeyword=(
    DotNETRuntime:ThreadPoolEnqueue
    DotNETRuntime:ThreadPoolDequeue
    DotNETRuntime:ThreadPoolIOEnqueue
    DotNETRuntime:ThreadPoolIODequeue
    DotNETRuntime:ThreadCreating
    DotNETRuntime:ThreadRunning
)

declare -a DotNETRuntime_NoKeyword=(
    DotNETRuntime:ExceptionThrown
    DotNETRuntime:Contention
    DotNETRuntime:RuntimeInformationStart
    DotNETRuntime:EventSource
)

declare -a DotNETRuntime_ExceptionKeyword=(
    DotNETRuntime:ExceptionThrown_V1
    DotNETRuntime:ExceptionCatchStart
    DotNETRuntime:ExceptionCatchStop
    DotNETRuntime:ExceptionFinallyStart
    DotNETRuntime:ExceptionFinallyStop
    DotNETRuntime:ExceptionFilterStart
    DotNETRuntime:ExceptionFilterStop
    DotNETRuntime:ExceptionThrownStop
)

declare -a DotNETRuntime_ContentionKeyword=(
    DotNETRuntime:ContentionStart_V1
    DotNETRuntime:ContentionStop
    DotNETRuntime:ContentionStop_V1
)

declare -a DotNETRuntime_StackKeyword=(
    DotNETRuntime:CLRStackWalk
)

declare -a DotNETRuntime_AppDomainResourceManagementKeyword=(
    DotNETRuntime:AppDomainMemAllocated
    DotNETRuntime:AppDomainMemSurvived
)

declare -a DotNETRuntime_AppDomainResourceManagementKeyword_ThreadingKeyword=(
    DotNETRuntime:ThreadCreated
    DotNETRuntime:ThreadTerminated
    DotNETRuntime:ThreadDomainEnter
)

declare -a DotNETRuntime_InteropKeyword=(
    DotNETRuntime:ILStubGenerated
    DotNETRuntime:ILStubCacheHit
)

declare -a DotNETRuntime_JitKeyword_NGenKeyword=(
    DotNETRuntime:DCStartCompleteV2
    DotNETRuntime:DCEndCompleteV2
    DotNETRuntime:MethodDCStartV2
    DotNETRuntime:MethodDCEndV2
    DotNETRuntime:MethodDCStartVerboseV2
    DotNETRuntime:MethodDCEndVerboseV2
    DotNETRuntime:MethodLoad
    DotNETRuntime:MethodLoad_V1
    DotNETRuntime:MethodLoad_V2
    DotNETRuntime:MethodUnload
    DotNETRuntime:MethodUnload_V1
    DotNETRuntime:MethodUnload_V2
    DotNETRuntime:MethodLoadVerbose
    DotNETRuntime:MethodLoadVerbose_V1
    DotNETRuntime:MethodLoadVerbose_V2
    DotNETRuntime:MethodUnloadVerbose
    DotNETRuntime:MethodUnloadVerbose_V1
    DotNETRuntime:MethodUnloadVerbose_V2
)

declare -a DotNETRuntime_JitKeyword=(
    DotNETRuntime:MethodJittingStarted
    DotNETRuntime:MethodJittingStarted_V1
)

declare -a DotNETRuntime_JitTracingKeyword=(
    DotNETRuntime:MethodJitInliningSucceeded
    DotNETRuntime:MethodJitInliningFailed
    DotNETRuntime:MethodJitTailCallSucceeded
    DotNETRuntime:MethodJitTailCallFailed
)

declare -a DotNETRuntime_JittedMethodILToNativeMapKeyword=(
    DotNETRuntime:MethodILToNativeMap
)

declare -a DotNETRuntime_LoaderKeyword=(
    DotNETRuntime:ModuleDCStartV2
    DotNETRuntime:ModuleDCEndV2
    DotNETRuntime:DomainModuleLoad
    DotNETRuntime:DomainModuleLoad_V1
    DotNETRuntime:ModuleLoad
    DotNETRuntime:ModuleUnload
    DotNETRuntime:AssemblyLoad
    DotNETRuntime:AssemblyLoad_V1
    DotNETRuntime:AssemblyUnload
    DotNETRuntime:AssemblyUnload_V1
    DotNETRuntime:AppDomainLoad
    DotNETRuntime:AppDomainLoad_V1
    DotNETRuntime:AppDomainUnload
    DotNETRuntime:AppDomainUnload_V1
)

declare -a DotNETRuntime_LoaderKeyword=(
    DotNETRuntime:ModuleLoad_V1
    DotNETRuntime:ModuleLoad_V2
    DotNETRuntime:ModuleUnload_V1
    DotNETRuntime:ModuleUnload_V2
)

declare -a DotNETRuntime_SecurityKeyword=(
    DotNETRuntime:StrongNameVerificationStart
    DotNETRuntime:StrongNameVerificationStart_V1
    DotNETRuntime:StrongNameVerificationStop
    DotNETRuntime:StrongNameVerificationStop_V1
    DotNETRuntime:AuthenticodeVerificationStart
    DotNETRuntime:AuthenticodeVerificationStart_V1
    DotNETRuntime:AuthenticodeVerificationStop
    DotNETRuntime:AuthenticodeVerificationStop_V1
)

declare -a DotNETRuntime_DebuggerKeyword=(
    DotNETRuntime:DebugIPCEventStart
    DotNETRuntime:DebugIPCEventEnd
    DotNETRuntime:DebugExceptionProcessingStart
    DotNETRuntime:DebugExceptionProcessingEnd
)

declare -a DotNETRuntime_CodeSymbolsKeyword=(
    DotNETRuntime:CodeSymbols
)

declare -a DotNETRuntime_CompilationKeyword=(
    DotNETRuntime:TieredCompilationSettings
    DotNETRuntime:TieredCompilationPause
    DotNETRuntime:TieredCompilationResume
    DotNETRuntime:TieredCompilationBackgroundJitStart
    DotNETRuntime:TieredCompilationBackgroundJitStop
    DotNETRuntimeRundown:TieredCompilationSettingsDCStart
)

# Separate GCCollectOnly list because our LTTng implementation doesn't set tracepoint verbosity.
# Once tracepoint verbosity is set, we can set verbosity and collapse this wtih DotNETRuntimePrivate_GCPrivateKeyword.
declare -a DotNETRuntimePrivate_GCPrivateKeyword_GCCollectOnly=(
    DotNETRuntimePrivate:GCDecision
    DotNETRuntimePrivate:GCDecision_V1
    DotNETRuntimePrivate:GCSettings
    DotNETRuntimePrivate:GCSettings_V1
    DotNETRuntimePrivate:GCPerHeapHistory
    DotNETRuntimePrivate:GCPerHeapHistory_V1
    DotNETRuntimePrivate:GCGlobalHeapHistory
    DotNETRuntimePrivate:GCGlobalHeapHistory_V1
    DotNETRuntimePrivate:PrvGCMarkStackRoots
    DotNETRuntimePrivate:PrvGCMarkStackRoots_V1
    DotNETRuntimePrivate:PrvGCMarkFinalizeQueueRoots
    DotNETRuntimePrivate:PrvGCMarkFinalizeQueueRoots_V1
    DotNETRuntimePrivate:PrvGCMarkHandles
    DotNETRuntimePrivate:PrvGCMarkHandles_V1
    DotNETRuntimePrivate:PrvGCMarkCards
    DotNETRuntimePrivate:PrvGCMarkCards_V1
    DotNETRuntimePrivate:BGCBegin
    DotNETRuntimePrivate:BGC1stNonConEnd
    DotNETRuntimePrivate:BGC1stConEnd
    DotNETRuntimePrivate:BGC2ndNonConBegin
    DotNETRuntimePrivate:BGC2ndNonConEnd
    DotNETRuntimePrivate:BGC2ndConBegin
    DotNETRuntimePrivate:BGC2ndConEnd
    DotNETRuntimePrivate:BGCPlanEnd
    DotNETRuntimePrivate:BGCSweepEnd
    DotNETRuntimePrivate:BGCDrainMark
    DotNETRuntimePrivate:BGCRevisit
    DotNETRuntimePrivate:BGCOverflow
    DotNETRuntimePrivate:BGCAllocWaitBegin
    DotNETRuntimePrivate:BGCAllocWaitEnd
    DotNETRuntimePrivate:GCFullNotify
    DotNETRuntimePrivate:GCFullNotify_V1
    DotNETRuntimePrivate:PrvFinalizeObject
    DotNETRuntimePrivate:PinPlugAtGCTime
)

declare -a DotNETRuntimePrivate_GCPrivateKeyword=(
    DotNETRuntimePrivate:GCDecision
    DotNETRuntimePrivate:GCDecision_V1
    DotNETRuntimePrivate:GCSettings
    DotNETRuntimePrivate:GCSettings_V1
    DotNETRuntimePrivate:GCOptimized
    DotNETRuntimePrivate:GCOptimized_V1
    DotNETRuntimePrivate:GCPerHeapHistory
    DotNETRuntimePrivate:GCPerHeapHistory_V1
    DotNETRuntimePrivate:GCGlobalHeapHistory
    DotNETRuntimePrivate:GCGlobalHeapHistory_V1
    DotNETRuntimePrivate:GCJoin
    DotNETRuntimePrivate:GCJoin_V1
    DotNETRuntimePrivate:PrvGCMarkStackRoots
    DotNETRuntimePrivate:PrvGCMarkStackRoots_V1
    DotNETRuntimePrivate:PrvGCMarkFinalizeQueueRoots
    DotNETRuntimePrivate:PrvGCMarkFinalizeQueueRoots_V1
    DotNETRuntimePrivate:PrvGCMarkHandles
    DotNETRuntimePrivate:PrvGCMarkHandles_V1
    DotNETRuntimePrivate:PrvGCMarkCards
    DotNETRuntimePrivate:PrvGCMarkCards_V1
    DotNETRuntimePrivate:BGCBegin
    DotNETRuntimePrivate:BGC1stNonConEnd
    DotNETRuntimePrivate:BGC1stConEnd
    DotNETRuntimePrivate:BGC2ndNonConBegin
    DotNETRuntimePrivate:BGC2ndNonConEnd
    DotNETRuntimePrivate:BGC2ndConBegin
    DotNETRuntimePrivate:BGC2ndConEnd
    DotNETRuntimePrivate:BGCPlanEnd
    DotNETRuntimePrivate:BGCSweepEnd
    DotNETRuntimePrivate:BGCDrainMark
    DotNETRuntimePrivate:BGCRevisit
    DotNETRuntimePrivate:BGCOverflow
    DotNETRuntimePrivate:BGCAllocWaitBegin
    DotNETRuntimePrivate:BGCAllocWaitEnd
    DotNETRuntimePrivate:GCFullNotify
    DotNETRuntimePrivate:GCFullNotify_V1
    DotNETRuntimePrivate:PrvFinalizeObject
    DotNETRuntimePrivate:PinPlugAtGCTime
)

declare -a DotNETRuntimePrivate_StartupKeyword=(
    DotNETRuntimePrivate:EEStartupStart
    DotNETRuntimePrivate:EEStartupStart_V1
    DotNETRuntimePrivate:EEStartupEnd
    DotNETRuntimePrivate:EEStartupEnd_V1
    DotNETRuntimePrivate:EEConfigSetup
    DotNETRuntimePrivate:EEConfigSetup_V1
    DotNETRuntimePrivate:EEConfigSetupEnd
    DotNETRuntimePrivate:EEConfigSetupEnd_V1
    DotNETRuntimePrivate:LdSysBases
    DotNETRuntimePrivate:LdSysBases_V1
    DotNETRuntimePrivate:LdSysBasesEnd
    DotNETRuntimePrivate:LdSysBasesEnd_V1
    DotNETRuntimePrivate:ExecExe
    DotNETRuntimePrivate:ExecExe_V1
    DotNETRuntimePrivate:ExecExeEnd
    DotNETRuntimePrivate:ExecExeEnd_V1
    DotNETRuntimePrivate:Main
    DotNETRuntimePrivate:Main_V1
    DotNETRuntimePrivate:MainEnd
    DotNETRuntimePrivate:MainEnd_V1
    DotNETRuntimePrivate:ApplyPolicyStart
    DotNETRuntimePrivate:ApplyPolicyStart_V1
    DotNETRuntimePrivate:ApplyPolicyEnd
    DotNETRuntimePrivate:ApplyPolicyEnd_V1
    DotNETRuntimePrivate:LdLibShFolder
    DotNETRuntimePrivate:LdLibShFolder_V1
    DotNETRuntimePrivate:LdLibShFolderEnd
    DotNETRuntimePrivate:LdLibShFolderEnd_V1
    DotNETRuntimePrivate:PrestubWorker
    DotNETRuntimePrivate:PrestubWorker_V1
    DotNETRuntimePrivate:PrestubWorkerEnd
    DotNETRuntimePrivate:PrestubWorkerEnd_V1
    DotNETRuntimePrivate:GetInstallationStart
    DotNETRuntimePrivate:GetInstallationStart_V1
    DotNETRuntimePrivate:GetInstallationEnd
    DotNETRuntimePrivate:GetInstallationEnd_V1
    DotNETRuntimePrivate:OpenHModule
    DotNETRuntimePrivate:OpenHModule_V1
    DotNETRuntimePrivate:OpenHModuleEnd
    DotNETRuntimePrivate:OpenHModuleEnd_V1
    DotNETRuntimePrivate:ExplicitBindStart
    DotNETRuntimePrivate:ExplicitBindStart_V1
    DotNETRuntimePrivate:ExplicitBindEnd
    DotNETRuntimePrivate:ExplicitBindEnd_V1
    DotNETRuntimePrivate:ParseXml
    DotNETRuntimePrivate:ParseXml_V1
    DotNETRuntimePrivate:ParseXmlEnd
    DotNETRuntimePrivate:ParseXmlEnd_V1
    DotNETRuntimePrivate:InitDefaultDomain
    DotNETRuntimePrivate:InitDefaultDomain_V1
    DotNETRuntimePrivate:InitDefaultDomainEnd
    DotNETRuntimePrivate:InitDefaultDomainEnd_V1
    DotNETRuntimePrivate:InitSecurity
    DotNETRuntimePrivate:InitSecurity_V1
    DotNETRuntimePrivate:InitSecurityEnd
    DotNETRuntimePrivate:InitSecurityEnd_V1
    DotNETRuntimePrivate:AllowBindingRedirs
    DotNETRuntimePrivate:AllowBindingRedirs_V1
    DotNETRuntimePrivate:AllowBindingRedirsEnd
    DotNETRuntimePrivate:AllowBindingRedirsEnd_V1
    DotNETRuntimePrivate:EEConfigSync
    DotNETRuntimePrivate:EEConfigSync_V1
    DotNETRuntimePrivate:EEConfigSyncEnd
    DotNETRuntimePrivate:EEConfigSyncEnd_V1
    DotNETRuntimePrivate:FusionBinding
    DotNETRuntimePrivate:FusionBinding_V1
    DotNETRuntimePrivate:FusionBindingEnd
    DotNETRuntimePrivate:FusionBindingEnd_V1
    DotNETRuntimePrivate:LoaderCatchCall
    DotNETRuntimePrivate:LoaderCatchCall_V1
    DotNETRuntimePrivate:LoaderCatchCallEnd
    DotNETRuntimePrivate:LoaderCatchCallEnd_V1
    DotNETRuntimePrivate:FusionInit
    DotNETRuntimePrivate:FusionInit_V1
    DotNETRuntimePrivate:FusionInitEnd
    DotNETRuntimePrivate:FusionInitEnd_V1
    DotNETRuntimePrivate:FusionAppCtx
    DotNETRuntimePrivate:FusionAppCtx_V1
    DotNETRuntimePrivate:FusionAppCtxEnd
    DotNETRuntimePrivate:FusionAppCtxEnd_V1
    DotNETRuntimePrivate:Fusion2EE
    DotNETRuntimePrivate:Fusion2EE_V1
    DotNETRuntimePrivate:Fusion2EEEnd
    DotNETRuntimePrivate:Fusion2EEEnd_V1
    DotNETRuntimePrivate:SecurityCatchCall
    DotNETRuntimePrivate:SecurityCatchCall_V1
    DotNETRuntimePrivate:SecurityCatchCallEnd
    DotNETRuntimePrivate:SecurityCatchCallEnd_V1
)

declare -a DotNETRuntimePrivate_StackKeyword=(
    DotNETRuntimePrivate:CLRStackWalkPrivate
)

declare -a DotNETRuntimePrivate_PerfTrackPrivateKeyword=(
    DotNETRuntimePrivate:ModuleRangeLoadPrivate
)

declare -a DotNETRuntimePrivate_BindingKeyword=(
    DotNETRuntimePrivate:BindingPolicyPhaseStart
    DotNETRuntimePrivate:BindingPolicyPhaseEnd
    DotNETRuntimePrivate:BindingNgenPhaseStart
    DotNETRuntimePrivate:BindingNgenPhaseEnd
    DotNETRuntimePrivate:BindingLookupAndProbingPhaseStart
    DotNETRuntimePrivate:BindingLookupAndProbingPhaseEnd
    DotNETRuntimePrivate:LoaderPhaseStart
    DotNETRuntimePrivate:LoaderPhaseEnd
    DotNETRuntimePrivate:BindingPhaseStart
    DotNETRuntimePrivate:BindingPhaseEnd
    DotNETRuntimePrivate:BindingDownloadPhaseStart
    DotNETRuntimePrivate:BindingDownloadPhaseEnd
    DotNETRuntimePrivate:LoaderAssemblyInitPhaseStart
    DotNETRuntimePrivate:LoaderAssemblyInitPhaseEnd
    DotNETRuntimePrivate:LoaderMappingPhaseStart
    DotNETRuntimePrivate:LoaderMappingPhaseEnd
    DotNETRuntimePrivate:LoaderDeliverEventsPhaseStart
    DotNETRuntimePrivate:LoaderDeliverEventsPhaseEnd
    DotNETRuntimePrivate:FusionMessageEvent
    DotNETRuntimePrivate:FusionErrorCodeEvent
)

declare -a DotNETRuntimePrivate_SecurityPrivateKeyword=(
    DotNETRuntimePrivate:EvidenceGenerated
    DotNETRuntimePrivate:ModuleTransparencyComputationStart
    DotNETRuntimePrivate:ModuleTransparencyComputationEnd
    DotNETRuntimePrivate:TypeTransparencyComputationStart
    DotNETRuntimePrivate:TypeTransparencyComputationEnd
    DotNETRuntimePrivate:MethodTransparencyComputationStart
    DotNETRuntimePrivate:MethodTransparencyComputationEnd
    DotNETRuntimePrivate:FieldTransparencyComputationStart
    DotNETRuntimePrivate:FieldTransparencyComputationEnd
    DotNETRuntimePrivate:TokenTransparencyComputationStart
    DotNETRuntimePrivate:TokenTransparencyComputationEnd
)

declare -a DotNETRuntimePrivate_PrivateFusionKeyword=(
    DotNETRuntimePrivate:NgenBindEvent
)

declare -a DotNETRuntimePrivate_NoKeyword=(
    DotNETRuntimePrivate:FailFast
)

declare -a DotNETRuntimePrivate_InteropPrivateKeyword=(
    DotNETRuntimePrivate:CCWRefCountChange
)

declare -a DotNETRuntimePrivate_GCHandlePrivateKeyword=(
    DotNETRuntimePrivate:PrvSetGCHandle
    DotNETRuntimePrivate:PrvDestroyGCHandle
)

declare -a DotNETRuntimePrivate_LoaderHeapPrivateKeyword=(
    DotNETRuntimePrivate:AllocRequest
)

declare -a DotNETRuntimePrivate_MulticoreJitPrivateKeyword=(
    DotNETRuntimePrivate:MulticoreJit
    DotNETRuntimePrivate:MulticoreJitMethodCodeReturned
)

declare -a DotNETRuntimePrivate_DynamicTypeUsageKeyword=(
    DotNETRuntimePrivate:IInspectableRuntimeClassName
    DotNETRuntimePrivate:WinRTUnbox
    DotNETRuntimePrivate:CreateRCW
    DotNETRuntimePrivate:RCWVariance
    DotNETRuntimePrivate:RCWIEnumerableCasting
    DotNETRuntimePrivate:CreateCCW
    DotNETRuntimePrivate:CCWVariance
    DotNETRuntimePrivate:ObjectVariantMarshallingToNative
    DotNETRuntimePrivate:GetTypeFromGUID
    DotNETRuntimePrivate:GetTypeFromProgID
    DotNETRuntimePrivate:ConvertToCallbackEtw
    DotNETRuntimePrivate:BeginCreateManagedReference
    DotNETRuntimePrivate:EndCreateManagedReference
    DotNETRuntimePrivate:ObjectVariantMarshallingToManaged
)

######################################
## Global Variables
######################################

# Declare an array of events to collect.
declare -a eventsToCollect

# Use Perf_Event
usePerf=1

# Use LTTng
useLTTng=1

# LTTng Installed
lttngInstalled=0

# Collect hardware events
collect_HWevents=0

# Set to 1 when the CTRLC_Handler gets invoked.
handlerInvoked=0

# Log file
declare logFile
logFilePrefix='/tmp/perfcollect'
logEnabled=1

######################################
## Logging Functions
######################################
LogAppend()
{
    if (( $logEnabled == 1 ))
    then
        echo $* >> $logFile
    fi
}

RunSilent()
{
    if (( $logEnabled == 1 ))
    then
        echo "Running \"$*\"" >> $logFile
        $* >> $logFile 2>&1
        echo "" >> $logFile
    else
        $* > /dev/null 2>&1
    fi
}

InitializeLog()
{
    # Pick the log file name.
    logFile="$logFilePrefix.log"
    while [ -f $logFile ];
    do
        logFile="$logFilePrefix.$RANDOM.log"
    done

    # Start the log
    date=`date`
    echo "Log started at ${date}" > $logFile
    echo '' >> $logFile

    # The system information.
    LogAppend 'Machine info: '  `uname -a`
    LogAppend 'perf version:'   `$perfcmd --version`
    LogAppend 'LTTng version: ' `lttng --version`
    LogAppend
}

CloseLog()
{
    LogAppend "END LOG FILE"
    LogAppend "NOTE: It is normal for the log file to end right before the trace is actually compressed.  This occurs because the log file is part of the archive, and thus can't be written anymore."

    # The log itself doesn't need to be closed,
    # but we need to tell the script not to log anymore.
    logEnabled=0
}


######################################
## Helper Functions
######################################

##
# Console text color modification helpers.
##
RedText()
{
    tput setaf 1
}

GreenText()
{
    tput setaf 2
}

BlueText()
{
    tput setaf 6
}
YellowText()
{
    tput setaf 3
}

ResetText()
{
    tput sgr0
}

# $1 == Status message
WriteStatus()
{
        LogAppend $*
    BlueText
    echo $1
    ResetText
}

# $1 == Message.
WriteWarning()
{
    LogAppend $*
    YellowText
    echo $1
    ResetText
}

# $1 == Message.
FatalError()
{
    RedText
    echo "ERROR: $1"
    ResetText
    PrintUsage
    exit 1
}

EnsureRoot()
{
    # Warn non-root users.
    if [ `whoami` != "root" ]
    then
        RedText
        echo "This script must be run as root."
        ResetText
        exit 1;
    fi
}

######################################
# Command Discovery
######################################
DiscoverCommands()
{
    perfcmd=`GetCommandFullPath "perf"`
    if [ "$(IsDebian)" == "1" ]
    then
        # Test perf to see if it successfully runs or fails because it doesn't match the kernel version.
        $perfcmd --version > /dev/null 2>&1
        if [ "$?" == "1" ]
        then
            perf49Cmd=`GetCommandFullPath "perf_4.9"`
            $perf49Cmd --version > /dev/null 2>&1
            if [ "$?" == "0" ]
            then
                perfcmd=$perf49Cmd
            else
                perf419Cmd=`GetCommandFullPath "perf_4.19"`
                $perf419Cmd --version > /dev/null 2>&1
                if [ "$?" == "0" ]
                then
                    perfcmd=$perf419Cmd
                else
                    perf316Cmd=`GetCommandFullPath "perf_3.16"`
                    $perf316Cmd --version > /dev/null 2>&1
                    if [ "$?" == "0" ]
                    then
                        perfcmd=$perf316Cmd
                    fi
                fi
            fi
        fi
    fi
    lttngcmd=`GetCommandFullPath "lttng"`
    zipcmd=`GetCommandFullPath "zip"`
    unzipcmd=`GetCommandFullPath "unzip"`
}

GetCommandFullPath()
{
    echo `command -v $1`
}

######################################
# Prerequisite Installation
######################################
IsRHEL()
{
    local rhel=0
    if [ -f /etc/redhat-release ]
    then
        rhel=1
    fi

    echo $rhel
}

InstallPerf_RHEL()
{
    # Disallow non-root users.
    EnsureRoot

    # Install perf
    yum install perf zip unzip
}

IsDebian()
{
    local debian=0
    local uname=`uname -a`
    if [[ $uname =~ .*Debian.* ]]
    then
        debian=1
    elif [ -f /etc/debian_version ]
    then
        debian=1
    fi

    echo $debian
}

InstallPerf_Debian()
{
    # Disallow non-root users.
    EnsureRoot

    # Check for the existence of the linux-tools package.
    pkgName='linux-tools'
    pkgCount=`apt-cache search $pkgName | grep -c $pkgName`
    if [ "$pkgCount" == "0" ]
    then
        pkgName='linux-perf'
        pkgCount=`apt-cache search $pkgName | grep -c $pkgName`
        if [ "$pkgCount" == "0" ]
        then
            FatalError "Unable to find a perf package to install."
        fi
    fi

    # Install zip and perf.
    apt-get install -y zip binutils $pkgName
}

IsSUSE()
{
    local suse=0
    if [ -f /usr/bin/zypper ]
    then
        suse=1
    fi

    echo $suse
}

InstallPerf_SUSE()
{
    # Disallow non-root users.
    EnsureRoot

    # Install perf.
    zypper install perf zip unzip
}

IsUbuntu()
{
    local ubuntu=0
    if [ -f /etc/lsb-release ]
    then
        local flavor=`cat /etc/lsb-release | grep DISTRIB_ID`
        if [ "$flavor" == "DISTRIB_ID=Ubuntu" ]
        then
            ubuntu=1
        fi
    fi
        
    echo $ubuntu
}

InstallPerf_Ubuntu()
{
    # Disallow non-root users.
    EnsureRoot

    # Install packages.
    BlueText
    echo "Installing perf_event packages."
    ResetText
    apt-get install -y linux-tools-common linux-tools-`uname -r` linux-cloud-tools-`uname -r` zip software-properties-common
}

InstallPerf()
{
    if [ "$(IsUbuntu)" == "1" ]
    then
        InstallPerf_Ubuntu
    elif [ "$(IsSUSE)" == "1" ]
    then
        InstallPerf_SUSE
    elif [ "$(IsDebian)" == "1" ]
    then
        InstallPerf_Debian
    elif [ "$(IsRHEL)" == "1" ]
    then
        InstallPerf_RHEL
    else
        FatalError "Auto install unsupported for this distribution.  Install perf manually to continue."
    fi
}

InstallLTTng_RHEL()
{
    # Disallow non-root users.
    EnsureRoot

    packageRepo="https://packages.efficios.com/repo.files/EfficiOS-RHEL7-x86-64.repo"

    # Prompt for confirmation, since we need to add a new repository.
    BlueText
    echo "LTTng installation requires that a new package repo be added to your yum configuration."
    echo "The package repo url is: $packageRepo"
    echo ""
    read -p "Would you like to add the LTTng package repo to your YUM configuration? [Y/N]" resp
    ResetText
    if [ "$resp" == "Y" ] || [ "$resp" == "y" ]
    then
        # Make sure that wget is installed.
        BlueText
        echo "Installing wget.  Required to add package repo."
        ResetText
        yum install wget

        # Connect to the LTTng package repo.
        wget -P /etc/yum.repos.d/ $packageRepo

        # Import package signing key.
        rpmkeys --import https://packages.efficios.com/rhel/repo.key

        # Update the yum package database.
        yum updateinfo

        # Install LTTng
        yum install lttng-tools lttng-ust kmod-lttng-modules babeltrace
    fi
}

InstallLTTng_Debian()
{
    # Disallow non-root users.
    EnsureRoot

    # Install LTTng
    apt-get install -y lttng-tools liblttng-ust-dev
}

InstallLTTng_SUSE()
{
    # Disallow non-root users.
    EnsureRoot

    # Package repo url
    packageRepo="http://download.opensuse.org/repositories/devel:/tools:/lttng/openSUSE_13.2/devel:tools:lttng.repo"

    # Prompt for confirmation, since we need to add a new repository.
    BlueText
    echo "LTTng installation requires that a new package repo be added to your zypper configuration."
    echo "The package repo url is: $packageRepo"
    echo ""
    read -p "Would you like to add the LTTng package repo to your zypper configuration? [Y/N]" resp
    ResetText
    if [ "$resp" == "Y" ] || [ "$resp" == "y" ]
    then

        # Add package repo.
        BlueText
        echo "Adding LTTng repo and running zypper refresh."
        ResetText
        zypper addrepo $packageRepo
        zypper refresh

        # Install packages.
        BlueText
        echo "Installing LTTng packages."
        ResetText
        zypper install lttng-tools lttng-modules lttng-ust-devel
    fi
}

InstallLTTng_Ubuntu()
{
    # Disallow non-root users.
    EnsureRoot

    # Add the PPA feed as a repository.
    BlueText
    echo "LTTng can be installed using default Ubuntu packages via the Ubuntu package feeds or using the latest"
    echo "stable package feed published by LTTng (PPA feed).  It is recommended that LTTng be installed using the PPA feed."
    echo ""
    ResetText
    echo "    If you select yes, then the LTTng PPA feed will be added to your apt configuration."
    echo "    If you select no, then LTTng will be installed from an existing feed (either default Ubuntu or PPA if previously added."
    echo ""
    BlueText
    read -p "Would you like to add the LTTng PPA feed to your apt configuration? [Y/N]" resp
    ResetText
    if [ "$resp" == "Y" ] || [ "$resp" == "y" ]
    then
        BlueText
        echo "Adding LTTng PPA feed and running apt-get update."
        ResetText
        apt-add-repository ppa:lttng/ppa
        apt-get update
    fi

    # Install packages.
    BlueText
    echo "Installing LTTng packages."
    ResetText
    apt-get install -y lttng-tools lttng-modules-dkms liblttng-ust0
}

InstallLTTng()
{
    if [ "$(IsUbuntu)" == "1" ]
    then
        InstallLTTng_Ubuntu
    elif [ "$(IsSUSE)" == "1" ]
    then
        InstallLTTng_SUSE
    elif [ "$(IsDebian)" == "1" ]
    then
        InstallLTTng_Debian
    elif [ "$(IsRHEL)" == "1" ]
    then
        InstallLTTng_RHEL
    else
        FatalError "Auto install unsupported for this distribution.  Install lttng and lttng-ust packages manually."
    fi
}

SupportsAutoInstall()
{
    local supportsAutoInstall=0
    if [ "$(IsUbuntu)" == "1" ] || [ "$(IsSUSE)" == "1" ]
    then
        supportsAutoInstall=1
    fi
    
    echo $supportsAutoInstall
}

EnsurePrereqsInstalled()
{
    # Discover commands and then determine if they're all present.
    DiscoverCommands

    # If perf is not installed, then bail, as it is currently required.
    if [ "$perfcmd" == "" ]
    then
        RedText
        echo "Perf not installed."
        if  [ "$(SupportsAutoInstall)" == "1" ]
        then
            echo "Run ./perfcollect install"
            echo "or install perf manually."
        else
            echo "Install perf to proceed."
        fi
        ResetText
        exit 1
    fi

    # If LTTng is installed, consider using it.
    if [ "$lttngcmd" == "" ] && [ "$useLTTng" == "1" ]
    then
        RedText
        echo "LTTng not installed."
        if  [ "$(SupportsAutoInstall)" == "1" ]
        then
            echo "Run ./perfcollect install"
            echo "or install LTTng manually."
        else
            echo "Install LTTng to proceed."
        fi
        ResetText
        exit 1

    fi

    # If zip or unzip are not installing, then bail.
    if [ "$zipcmd" == "" ] || [ "$unzipcmd" == "" ]
    then
        RedText
        echo "Zip and unzip are not installed."
        if [ "$(SupportsAutoInstall)" == "1" ]
        then
            echo "Run ./perfcollect install"
            echo "or install zip and unzip manually."
        else
            echo "Install zip and unzip to proceed."
        fi
        ResetText
        exit 1
    fi
}

######################################
# Argument Processing
######################################
action=''
inputTraceName=''
collectionPid=''
processFilter=''
graphType=''
perfOpt=''
viewer='perf'
gcCollectOnly=''
gcOnly=''
gcWithHeap=''
events=''

ProcessArguments()
{
    # No arguments
    if [ "$#" == "0" ]
    then
        PrintUsage
        exit 0
    fi

    # Set the action
    action=$1

    # Actions with no arguments.
    if [ "$action" == "livetrace" ]
    then
        return
    fi
    
    # Not enough arguments.
    if [ "$#" -le "1" ]
    then
        FatalError "Not enough arguments have been specified."
    fi

    # Validate action name.
    if [ "$action" != "collect" ] && [ "$action" != "view" ]
    then
        FatalError "Invalid action specified."
    fi

    # Set the data file.
    inputTraceName=$2
    if [ "$inputTraceName" == "" ]
    then
        FatalError "Invalid trace name specified."
    fi

    # Process remaining arguments.
    # First copy the args into an array so that we can walk the array.
    args=( "$@" )
    for (( i=2; i<${#args[@]}; i++ ))
    do
        # Get the arg.
        local arg=${args[$i]}

        # Convert the arg to lower case.
        arg=`echo $arg | tr '[:upper:]' '[:lower:]'`

        # Get the arg value.
        if [ ${i+1} -lt $# ]
        then
            local value=${args[$i+1]}

            # Convert the value to lower case.
            value=`echo $value | tr '[:upper:]' '[:lower:]'`
        fi

        # Match the arg to a known value.
        if [ "-pid" == "$arg" ]
        then
            collectionPid=$value
            i=$i+1
        elif [ "-processfilter" == "$arg" ]
        then
            processFilter=$value
            i=$i+1
        elif [ "-graphtype" == "$arg" ]
        then
            graphType=$value
            i=$i+1
        elif [ "-threadtime" == "$arg" ]
        then
            collect_threadTime=1
        elif [ "-hwevents" == "$arg" ]
        then
            collect_HWevents=1
        elif [ "-perfopt" == "$arg" ]
        then
            perfOpt=$value
            i=$i+1
        elif [ "-viewer" == "$arg" ]
        then
            viewer=$value
            i=$i+1

            # Validate the viewer.
            if [ "$viewer" != "perf" ] && [ "$viewer" != "lttng" ]
            then
                FatalError "Invalid viewer specified.  Valid values are 'perf' and 'lttng'."
            fi
        elif [ "-nolttng" == "$arg" ]
        then
            useLTTng=0
        elif [ "-gccollectonly" == "$arg" ]
        then
            gcCollectOnly=1
        elif [ "-gconly" == "$arg" ]
        then
            gcOnly=1
        elif [ "-gcwithheap" == "$arg" ]
        then
            gcWithHeap=1
        elif [ "-events" == "$arg" ]
        then
            events=$value
            i=$i+1
        elif [ "-collectsec" == "$arg" ]
        then
            duration=$value
            i=$i+1
        else
            echo "Unknown arg ${arg}, ignored..."
        fi
    done
    
}



##
# LTTng collection
##
lttngSessionName=''
lttngTraceDir=''
CreateLTTngSession()
{
    if [ "$action" == "livetrace" ]
    then
        output=`lttng create --live`
    else
        output=`lttng create`
    fi

    lttngSessionName=`echo $output | grep -o "Session.*created." | sed 's/\(Session \| created.\)//g'`
    lttngTraceDir=`echo $output | grep -o "Traces.*" | sed 's/\(Traces will be written in \|\)//g'`
}

SetupLTTngSession()
{
    # Setup per-event context information.
    RunSilent "lttng add-context --userspace --type vpid"
    RunSilent "lttng add-context --userspace --type vtid"
    RunSilent "lttng add-context --userspace --type procname"

    if [ "$action" == "livetrace" ]
    then
        RunSilent "lttng enable-event --userspace --tracepoint DotNETRuntime:EventSource"
    elif [ "$gcCollectOnly" == "1" ]
    then
        usePerf=0
        EnableLTTngEvents ${DotNETRuntime_GCKeyword_GCCollectOnly[@]}
        EnableLTTngEvents ${DotNETRuntimePrivate_GCPrivateKeyword_GCCollectOnly[@]}
        EnableLTTngEvents ${DotNETRuntime_ExceptionKeyword[@]}
    elif [ "$gcOnly" == "1" ]
    then
        usePerf=0
        EnableLTTngEvents ${DotNETRuntime_GCKeyword[@]}
        EnableLTTngEvents ${DotNETRuntimePrivate_GCPrivateKeyword[@]}
        EnableLTTngEvents ${DotNETRuntime_JitKeyword[@]}
        EnableLTTngEvents ${DotNETRuntime_LoaderKeyword[@]}
        EnableLTTngEvents ${DotNETRuntime_ExceptionKeyword[@]}
    elif [ "$gcWithHeap" == "1" ]
    then
        usePerf=0
        EnableLTTngEvents ${DotNETRuntime_GCKeyword[@]}
        EnableLTTngEvents ${DotNETRuntime_GCHeapSurvivalAndMovementKeyword[@]}
    else
        if [ "$events" == "" ]
        then
            # Enable the default set of events.
            EnableLTTngEvents ${DotNETRuntime_ThreadingKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_ThreadingKeyword_ThreadTransferKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_NoKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_ExceptionKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_ContentionKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_JitKeyword_NGenKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_JitKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_LoaderKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_GCKeyword_GCCollectOnly[@]}
            EnableLTTngEvents ${DotNETRuntimePrivate_GCPrivateKeyword_GCCollectOnly[@]}
            EnableLTTngEvents ${DotNETRuntimePrivate_BindingKeyword[@]}
            EnableLTTngEvents ${DotNETRuntimePrivate_MulticoreJitPrivateKeyword[@]}
            EnableLTTngEvents ${DotNETRuntime_CompilationKeyword[@]}
        elif [ "$events" == "threading" ]
        then
            EnableLTTngEvents ${DotNETRuntime_ThreadingKeyword[@]}
        fi
    fi
}

DestroyLTTngSession()
{
    RunSilent "lttng destroy $lttngSessionName"
}

StartLTTngCollection()
{
    CreateLTTngSession
    SetupLTTngSession

    RunSilent "lttng start $lttngSessionName"
}

StopLTTngCollection()
{
    RunSilent "lttng stop $lttngSessionName"

    DestroyLTTngSession
}

# $@ == event names to be enabled
EnableLTTngEvents()
{
    args=( "$@" )
    for (( i=0; i<${#args[@]}; i++ ))
    do
        RunSilent "lttng enable-event -s $lttngSessionName -u --tracepoint ${args[$i]}"
    done
}

##
# Helper that processes collected data.
# This helper is called when the CTRL+C signal is handled.
##
ProcessCollectedData()
{
    # Make a new target directory.
    local traceSuffix=".trace"
    local traceName=$inputTraceName
    local directoryName=$traceName$traceSuffix
    mkdir $directoryName

    # Save LTTng trace files.
    if [ "$useLTTng" == "1" ]
    then
        LogAppend "Saving LTTng trace files."

        if [ -d $lttngTraceDir ]
        then
            RunSilent "mkdir lttngTrace"
            RunSilent "cp -r $lttngTraceDir lttngTrace"
        fi
    fi

    if [ "$usePerf" == 1 ]
    then
        # Get any perf-$pid.map files that were used by the
        # trace and store them alongside the trace.
        LogAppend "Saving perf.map files."
            RunSilent "$perfcmd buildid-list --with-hits"
        local mapFiles=`$perfcmd buildid-list --with-hits | grep /tmp/perf- | cut -d ' ' -f 2`
        for mapFile in $mapFiles
        do
            if [ -f $mapFile ]
            then
                LogAppend "Saving $mapFile"

                # Change permissions on the file before saving, as perf will need to access the file later
                # in this script when running perf script.
                RunSilent "chown root $mapFile"
                RunSilent "cp $mapFile ."

                local perfinfoFile=${mapFile/perf/perfinfo}

                LogAppend "Attempting to find ${perfinfoFile}"

                if [ -f $perfinfoFile ]
                then
                    LogAppend "Saving $perfinfoFile"
                    RunSilent "chown root $perfinfoFile"
                    RunSilent "cp $perfinfoFile ."
                else
                    LogAppend "Skipping ${perfinfoFile}."
                fi
            else
                LogAppend "Skipping $mapFile.  Some managed symbols may not be resolvable, but trace is still valid."
            fi
        done

        WriteStatus "Generating native image symbol files"

        # Get the list of loaded images and use the path to libcoreclr.so to find crossgen.
        # crossgen is expected to sit next to libcoreclr.so.
        local buildidList=`$perfcmd buildid-list | grep libcoreclr.so | cut -d ' ' -f 2`
        local crossgenCmd=''
        local crossgenDir=''
        for file in $buildidList
        do
            crossgenDir=`dirname "${file}"`
            if [ -f ${crossgenDir}/crossgen ]
            then
                crossgenCmd=${crossgenDir}/crossgen
                LogAppend "Found crossgen at ${crossgenCmd}"
                break
            fi
        done

        OLDIFS=$IFS

        imagePaths=""

        if [ "$crossgenCmd" != "" ]
        then
                local perfinfos=`ls . | grep perfinfo | cut -d ' ' -f 2`
                for perfinfo in $perfinfos
                do
                    if [ -f $perfinfo ]
                    then
                        IFS=";"
                        while read command dll guid; do
                            if [ $command ]; then
                                if [ $command = "ImageLoad" ]; then
                                    if [ -f $dll ]; then
                                        imagePaths="${dll}:${imagePaths}"
                                    fi
                                fi
                            fi
                        done < $perfinfo
                        IFS=$OLDIFS
                    fi
                done

                IFS=":"
                LogAppend "Generating PerfMaps for native images"
                for path in $imagePaths
                do
                    if [ `echo ${path} | grep ^.*\.dll$` ]
                    then
                        IFS=""
                        LogAppend "Generating PerfMap for ${path}"
                        LogAppend "Running ${crossgenCmd} /r $imagePaths /CreatePerfMap . ${path}"
                        ${crossgenCmd} /r $imagePaths /CreatePerfMap . ${path} >> $logFile 2>&1
                        IFS=":"
                    else
                        LogAppend "Skipping ${path}"
                    fi
                done

            WriteStatus "...FINISHED"

        else
        if [ "$buildidList" != "" ]
        then
            LogAppend "crossgen not found, skipping native image map generation."
            WriteStatus "...SKIPPED"
            WriteWarning "Crossgen not found.  Framework symbols will be unavailable."
            WriteWarning "See https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/linux-performance-tracing.md#resolving-framework-symbols for details."
        else
            WriteWarning "libcoreclr.so not found in perf data. Please verify that your .NET Core process is running and consuming CPU."
        fi
        fi

        IFS=$OLDIFS

        # Create debuginfo files (separate symbols) for all modules in the trace.
        WriteStatus "Saving native symbols"

        # Get the list of DSOs with hits in the trace file (those that are actually used).
        # Filter out /tmp/perf-$pid.map files and files that end in .dll.
        local dsosWithHits=`$perfcmd buildid-list --with-hits | grep -v /tmp/perf- | grep -v .dll$`
        for dso in $dsosWithHits
        do
            # Build up tuples of buildid and binary path.
            local processEntry=0
            if [ -f $dso ]
            then
                local pathToBinary=$dso
                processEntry=1
            else
                local buildid=$dso
                pathToBinary=''
            fi

            # Once we have a tuple for a binary path that exists, process it.
            if [ "$processEntry" == "1" ]
            then
                # Get the binary name without path.
                local binaryName=`basename $pathToBinary`

                # Build the debuginfo file name.
                local destFileName=$binaryName.debuginfo

                # Build the destination directory for the debuginfo file.
                local currentDir=`pwd`
                local destDir=$currentDir/debuginfo/$buildid

                # Build the full path to the debuginfo file.
                local destPath=$destDir/$destFileName

                # Check to see if the DSO contains symbols, and if so, build the debuginfo file.
                local noSymbols=`objdump -t $pathToBinary | grep "no symbols" -c`
                if [ "$noSymbols" == "0" ]
                then
                    LogAppend "Generating debuginfo for $binaryName with buildid=$buildid"
                    RunSilent "mkdir -p $destDir"
                    RunSilent "objcopy --only-keep-debug $pathToBinary $destPath"
                else
                    LogAppend "Skipping $binaryName with buildid=$buildid.  No symbol information."
                fi
            fi
        done

        WriteStatus "...FINISHED"

            WriteStatus "Exporting perf.data file"

        # Merge sched_stat and sched_switch events.
        outputDumpFile="perf.data.txt"
        mergedFile="perf.data.merged"
        RunSilent "$perfcmd inject -v -s -i perf.data -o $mergedFile"

            # I've not found a good way to get the behavior that we want here - running the command and redirecting the output
            # when passing the command line to a function.  Thus, this case is hardcoded.

        # There is a breaking change where the capitalization of the -f parameter changed.
        LogAppend "Running $perfcmd script -i $mergedFile -F comm,pid,tid,cpu,time,period,event,ip,sym,dso,trace > $outputDumpFile"
        $perfcmd script -i $mergedFile -F comm,pid,tid,cpu,time,period,event,ip,sym,dso,trace > $outputDumpFile 2>>$logFile
        LogAppend

        if [ $? -ne 0 ]
        then
            LogAppend "Running $perfcmd script -i $mergedFile -f comm,pid,tid,cpu,time,period,event,ip,sym,dso,trace > $outputDumpFile"
            $perfcmd script -i $mergedFile -f comm,pid,tid,cpu,time,period,event,ip,sym,dso,trace > $outputDumpFile 2>>$logFile
            LogAppend
        fi

        # If the dump file is zero length, try to collect without the period field, which was added recently.
        if [ ! -s $outputDumpFile ]
        then
            LogAppend "Running $perfcmd script -i $mergedFile -f comm,pid,tid,cpu,time,event,ip,sym,dso,trace > $outputDumpFile"
            $perfcmd script -i $mergedFile -f comm,pid,tid,cpu,time,event,ip,sym,dso,trace > $outputDumpFile 2>>$logFile
            LogAppend
        fi

        WriteStatus "...FINISHED"
    fi

    WriteStatus "Compressing trace files"
    
    # Move all collected files to the new directory.
    RunSilent "mv * $directoryName"

    # Close the log - this stops all writing to the log, so that we can move it into the archive.
    CloseLog

    # Move the log file to the new directory and rename it to the standard log name.
    RunSilent "mv $logFile $directoryName/perfcollect.log"

    # Compress the data.
    local archiveSuffix=".zip"
    local archiveName=$directoryName$archiveSuffix
    RunSilent "$zipcmd -r $archiveName $directoryName"

    # Move back to the original directory.
    popd > /dev/null

    # Move the archive.
    RunSilent "mv $tempDir/$archiveName ."

    WriteStatus "...FINISHED"

    WriteStatus "Cleaning up artifacts"

    # Delete the temp directory.
    RunSilent "rm -rf $tempDir"

    WriteStatus "...FINISHED"

    # Tell the user where the trace is.
    WriteStatus
    WriteStatus "Trace saved to $archiveName"
}

##
# Handle the CTRL+C signal.
##
CTRLC_Handler()
{
    # Mark the handler invoked.
    handlerInvoked=1
}

EndCollect()
{
    if [ "$useLTTng" == "1" ]
    then
        StopLTTngCollection
    fi

    # Update the user.
    WriteStatus
    WriteStatus "...STOPPED."
    WriteStatus
    WriteStatus "Starting post-processing.  This may take some time."
    WriteStatus

    # The user must CTRL+C to stop collection.
    # When this happens, we catch the signal and finish our work.
    ProcessCollectedData
}

##
# Print usage information.
##
PrintUsage()
{
    echo "This script uses perf_event and LTTng to collect and view performance traces for .NET applications."
    echo "For detailed collection and viewing steps, view this script in a text editor or viewer."
    echo ""
    echo "./perfcollect <action> <tracename>"
    echo "Valid Actions: collect view livetrace install"
    echo ""
    echo "collect options:"
    echo "By default, collection includes CPU samples collected every ms."
    echo "    -pid          : Only collect data from the specified process id."
    echo "    -threadtime       : Collect context switch events."
    echo "  -hwevents         : Collect (some) hardware counters."
    echo ""
    echo "view options:"
    echo "    -processfilter      : Filter data by the specified process name."
    echo "    -graphtype      : Specify the type of graph.  Valid values are 'caller' and 'callee'.  Default is 'callee'."
    echo "    -viewer          : Specify the data viewer.  Valid values are 'perf' and 'lttng'.  Default is 'perf'."
    echo ""
    echo "livetrace:"
    echo "    Print EventSource events directly to the console.  Root privileges not required."
    echo ""
    echo "install:"
    echo "    Useful for first-time setup.  Installs/upgrades perf_event and LTTng."
    echo ""
}

##
# Validate and set arguments.
##

BuildPerfRecordArgs()
{
    # Start with default collection arguments that record all CPUs (-a) and collect call stacks (-g)
    collectionArgs="record -g"

    # Filter to a single process if desired
    if [ "$collectionPid" != "" ]
    then
        collectionArgs="$collectionArgs --pid=$collectionPid"
    else
        collectionArgs="$collectionArgs -a"
    fi

    # Enable CPU Collection
    if [ $collect_cpu -eq 1 ]
    then
        collectionArgs="$collectionArgs -F 999"
        eventsToCollect=( "${eventsToCollect[@]}" "cpu-clock" )
    fi

    # Enable HW counters event collection
    if [ $collect_HWevents -eq 1 ]
    then
        collectionArgs="$collectionArgs -e cycles,instructions,branches,cache-misses"
    fi
    
    # Enable context switches.
    if [ $collect_threadTime -eq 1 ]
    then
        eventsToCollect=( "${eventsToCollect[@]}" "sched:sched_stat_sleep" "sched:sched_switch" "sched:sched_process_exit" )
    fi

    # Build up the set of events.
    local eventString=""
    local comma=","
    for (( i=0; i<${#eventsToCollect[@]}; i++ ))
    do
        # Get the arg.
        eventName=${eventsToCollect[$i]}

        # Build up the comma separated list.
        if [ "$eventString" == "" ]
        then
            eventString=$eventName
        else
            eventString="$eventString$comma$eventName"
        fi

    done

    if [ ! -z ${duration} ]
    then
        durationString="sleep ${duration}"
    fi

    # Add the events onto the collection command line args.    
    collectionArgs="$collectionArgs -e $eventString $durationString"
}

DoCollect()
{
    # Ensure the script is run as root.
    EnsureRoot

    # Build collection args.
    # Places the resulting args in $collectionArgs
    BuildPerfRecordArgs

    # Trap CTRL+C
    trap CTRLC_Handler SIGINT

    # Create a temp directory to use for collection.
    local tempDir=`mktemp -d`
        LogAppend "Created temp directory $tempDir"

    # Switch to the directory.
    pushd $tempDir > /dev/null

    # Start LTTng collection.
    if [ "$useLTTng" == "1" ]
    then
        StartLTTngCollection
    fi

    # Tell the user that collection has started and how to exit.
    if [ "$duration" != "" ]
    then
        WriteStatus "Collection started. Collection will automatically stop in $duration second(s).  Press CTRL+C to stop early."
    else
            WriteStatus "Collection started. Press CTRL+C to stop."
    fi

    # Start perf record.
    if [ "$usePerf" == "1" ]
    then
        RunSilent $perfcmd $collectionArgs
    else
        # Wait here until CTRL+C handler gets called when user types CTRL+C.
                LogAppend "Waiting for CTRL+C handler to get called."

        waitTime=0
        for (( ; ; ))
        do
            if [ "$handlerInvoked" == "1" ]
            then
                break;
            fi

            # Wait and then check to see if the handler has been invoked or we've crossed the duration threshold.
            sleep 1
            waitTime=$waitTime+1
            if (( duration > 0 && duration <= waitTime ))
            then
                break;
            fi
        done
    fi
    EndCollect
}

DoLiveTrace()
{
    # Start the session
    StartLTTngCollection

    # View the event stream (until the user hits CTRL+C)
    WriteStatus "Listening for events from LTTng.  Hit CTRL+C to stop."
    $lttngcmd view

    # Stop the LTTng sessoin
    StopLTTngCollection
}

# $1 == Path to directory containing trace files
PropSymbolsAndMapFilesForView()
{
    # Get the current directory
    local currentDir=`pwd`    

    # Copy map files to /tmp since they aren't supported by perf buildid-cache.
    local mapFiles=`find -name *.map`
    for mapFile in $mapFiles
    do
        echo "Copying $mapFile to /tmp."
        cp $mapFile /tmp
    done

    # Cache all debuginfo files saved with the trace in the buildid cache.
    local debugInfoFiles=`find $currentDir -name *.debuginfo`
    for debugInfoFile in $debugInfoFiles
    do
        echo "Caching $debugInfoFile in buildid cache using perf buildid-cache."
        $perfcmd buildid-cache --add=$debugInfoFile
    done
}

DoView()
{
    # Generate a temp directory to extract the trace files into.
    local tempDir=`mktemp -d`
    
    # Extract the trace files.
    $unzipcmd $inputTraceName -d $tempDir
    
    # Move the to temp directory.
    pushd $tempDir
    cd `ls`

    # Select the viewer.
    if [ "$viewer" == "perf" ]
    then
        # Prop symbols and map files.
        PropSymbolsAndMapFilesForView `pwd`

        # Choose the view
        if [ "$graphType" == "" ]
        then
            graphType="callee"
        elif [ "$graphType" != "callee" ] && [ "$graphType" != "caller"]
        then
            FatalError "Invalid graph type specified.  Valid values are 'callee' and 'caller'."
        fi

        # Filter to specific process names if desired.
        if [ "$processFilter" != "" ]
        then
            processFilter="--comms=$processFilter"
        fi

        # Execute the viewer.
        $perfcmd report -n -g graph,0.5,$graphType $processFilter $perfOpt
    elif [ "$viewer" == "lttng" ]
    then
        babeltrace lttngTrace/ | more
    fi
    
    # Switch back to the original directory.
    popd

    # Delete the temp directory.
    rm -rf $tempDir
}

#####################################
## Main Script Start
#####################################

# Install perf if requested.  Do this before all other validation.
if [ "$1" == "install" ]
then
    InstallPerf
    InstallLTTng
    exit 0
fi

# Ensure prerequisites are installed.
EnsurePrereqsInstalled

# Initialize the log.
InitializeLog

# Process arguments.
ProcessArguments $@

# Take the appropriate action.
if [ "$action" == "collect" ]
then
    DoCollect
elif [ "$action" == "view" ]
then
    DoView
elif [ "$action" == "livetrace" ]
then
    DoLiveTrace
fi