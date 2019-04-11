//
using System;
using System.Runtime.InteropServices;

// READ: https://github.com/Microsoft/perfview/blob/ef1b2562ed07b85a0e5386a711d91988ef395208/src/TraceEvent/EventPipe/EventSerialization.md
//

namespace Microsoft.Diagnostics.Tools.Counters
{
	internal class EventParserProgress
	{
		public bool sawEventSize;
		public bool sawMetadataId;
		public bool sawThreadId;
		public bool sawTimeStamp;
		public bool sawActivityId;
		public bool sawRelatedActivityId;
		public bool sawPayloadSize;
		public bool sawPayload;
		public bool sawStackSize;
		public bool sawStack;

		public int eventSize;
		public int payloadSize;
		public int stackSize;

		public EventParserProgress()
		{
			sawEventSize = false;
			sawMetadataId = false;
			sawThreadId = false;
			sawTimeStamp = false;
			sawActivityId = false;
			sawRelatedActivityId = false;
			sawPayloadSize = false;
			sawPayload = false;
			sawStackSize = false;
			sawStack = false;

			eventSize = 0;
			payloadSize = 0;
			stackSize = 0;
		}

		public void Reset()
		{
			sawEventSize = false;
			sawMetadataId = false;
			sawThreadId = false;
			sawTimeStamp = false;
			sawActivityId = false;
			sawRelatedActivityId = false;
			sawPayloadSize = false; 
			sawPayload = false;
			sawStackSize = false;
			sawStack = false;

			eventSize = 0;
			payloadSize = 0;
			stackSize = 0;
		}
	}

/*
        int EventSize;    // Size bytes of this header and the payload and stacks.  Does NOT encode the size of the EventSize field itself. 
        int MetaDataId;   // a number identifying the description of this event.  0 is special as described below
        int ThreadId;
        long TimeStamp;
        Guid ActivityID;
        Guid RelatedActivityID;
        int PayloadSize;
*/

	public class EventParser
	{
		private EventParserProgress progress = new EventParserProgress();

		public EventParser() {}

		public int ParseEvent(Span<byte> buffer, int eventBlobSize, int bytesRead, int curIdx)
		{ 
			/*
			    int EventSize;    // Size bytes of this header and the payload and stacks.  Does NOT encode the size of the EventSize field itself. 
		        int MetaDataId;   // a number identifying the description of this event.  0 is special as described below
		        int ThreadId;
		        long TimeStamp;
		        Guid ActivityID;
		        Guid RelatedActivityID;
		        int PayloadSize; 
			*/
		    int curSize = 0;
		    while(curSize < eventBlobSize && curIdx < bytesRead)
		    {
			    Console.WriteLine($"curIdx: {curIdx}");
			    Console.WriteLine($"curSize: {curSize}");

		    	if (!progress.sawEventSize)
		    	{
		    		int eventSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
		    		curIdx += 4;
		    		curSize += 4;
		    		Console.WriteLine($"\t\tEvent size: {eventSize}");
		    		progress.sawEventSize = true;
		    		progress.eventSize = eventSize;
					if (curSize >= eventBlobSize) return curSize;

		    	}

		    	if (!progress.sawMetadataId)
		    	{
		    		int metataId = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
		    		curIdx += 4;
		    		curSize += 4;
		    		Console.WriteLine($"\t\tmetadata Id: {metataId}");
		    		progress.sawMetadataId = true;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawThreadId)
		    	{
		    		int threadId = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
		    		curIdx += 4;
		    		curSize += 4;
		    		Console.WriteLine($"\t\tthread Id: {threadId}");
		    		progress.sawThreadId = true;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawTimeStamp)
		    	{
		    		long timestamp = BitConverter.ToInt64(buffer.Slice(curIdx, 8));
		    		curIdx += 8;
		    		curSize += 8;
		    		Console.WriteLine($"\t\ttimestamp: {timestamp}");
		    		progress.sawTimeStamp = true;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawActivityId)
		    	{
					Guid activityId = new Guid(buffer.Slice(curIdx, 16).ToArray());
					curIdx += 16;
					curSize += 16;
					progress.sawActivityId = true;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawRelatedActivityId)
		    	{
					Guid relatedActivityId = new Guid(buffer.Slice(curIdx, 16).ToArray());
					curIdx += 16;
					curSize += 16;
					progress.sawRelatedActivityId = true;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawPayloadSize)
		    	{
		    		int payloadSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
		    		curIdx += 4;
		    		curSize += 4;
					Console.WriteLine($"\t\tPayload Size: {payloadSize}");
		    		progress.sawPayloadSize = true;
		    		progress.payloadSize = payloadSize;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawPayload)
		    	{
		    		string payload = System.Text.Encoding.UTF8.GetString(buffer.ToArray(), curIdx, progress.payloadSize);
		    		Console.WriteLine($"\t\tPayload: " + payload);
		    		curIdx += progress.payloadSize;
		    		curSize += progress.payloadSize;
					progress.sawPayload = true;

					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawStackSize)
		    	{
		    		// After that is integer (4 bytes) representing the count of bytes needed to represent the stack addresses. If there is no stack this count is 0.
					int stackSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));	
					Console.WriteLine($"\t\tStack size: {stackSize}");
					// TODO: probably can skip stack for now 
					curIdx += 4;
					curSize += 4;
					progress.stackSize = stackSize;
					progress.sawStackSize = true;
					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	if (!progress.sawStack)
		    	{
		    		curIdx += progress.stackSize;
		    		curSize += progress.stackSize;
					progress.sawStack = true;

					if (curSize >= eventBlobSize) return curSize;
		    	}

		    	// Alignment crap
		    	if (curIdx % 4 != 0)
		    	{
		    		curIdx += (4 - (curIdx % 4));
		    	}

		    	if (curSize % 4 != 0)
		    	{
		    		curSize += (4 - (curSize % 4));
		    	}

		    	Console.WriteLine($"\t\tcurSize: {curSize}");

		    	Console.WriteLine($"\t\tDone reading a single EVENT");
		    	progress.Reset();
			}
			return curSize;
		}

	}


}