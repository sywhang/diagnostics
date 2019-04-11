//
using System;
using System.Runtime.InteropServices;

// READ: https://github.com/Microsoft/perfview/blob/ef1b2562ed07b85a0e5386a711d91988ef395208/src/TraceEvent/EventPipe/EventSerialization.md
//

namespace Microsoft.Diagnostics.Tools.Counters
{
	// WARNING: KEEP THIS IN SYNC WITH 
	public enum FastSerializerTags
	{
	    Error              = 0, // To improve debugabilty, 0 is an illegal tag.
	    NullReference      = 1, // Tag for a null object forwardReference.
	    ObjectReference    = 2, // Followed by StreamLabel
	                            // 3 used to belong to ForwardReference, which got removed in V3
	    BeginObject        = 4, // Followed by Type object, object data, tagged EndObject
	    BeginPrivateObject = 5, // Like beginObject, but not placed in interning table on deserialiation
	    EndObject          = 6, // Placed after an object to mark its end.
	                            // 7 used to belong to ForwardDefinition, which got removed in V3
	    Byte               = 8,
	    Int16,
	    Int32,
	    Int64,
	    SkipRegion,
	    String,
	    Blob,
	    Limit                   // Just past the last valid tag, used for asserts.
	};

	internal class ParserProgress
	{
		public bool sawBeginTag;
		public bool sawTypeBeginTag;
		public bool sawNullRefTag;
		public bool sawVersionStr;
		public bool sawMinReqVersionStr;
		public bool sawFullNameLen;
		public bool sawFullNameStr;
		public bool sawTypeEndTag;
		public bool sawEventBlock;
		public bool sawEventBlockSize;
		public bool sawEndTag;

		public int version;
		public int minReqVersion;
		public int fullNameLength;
		public string fullName;
		public int eventBlockSize;
		public int remainingBytesToRead;

		public ParserProgress()
		{
			sawBeginTag = false;
			sawTypeBeginTag = false;
			sawNullRefTag = false;
			sawVersionStr = false;
			sawMinReqVersionStr = false;
			sawFullNameLen = false;
			sawFullNameStr = false; 
			sawTypeEndTag = false;
			sawEventBlock = false; 
			sawEventBlockSize = false;
			sawEndTag = false;
			remainingBytesToRead = 0;
		}

		public void Reset()
		{
			sawBeginTag = false;
			sawTypeBeginTag = false;
			sawNullRefTag = false;
			sawVersionStr = false;
			sawMinReqVersionStr = false;
			sawFullNameLen = false;
			sawFullNameStr = false; 
			sawTypeEndTag = false; 
			sawEventBlock = false;
			sawEventBlockSize = false;
			sawEndTag = false;
			remainingBytesToRead = 0;
		}
	}

/*

EventBlock Object:

    BeginObject Tag (begins the EventBlock Object)
    BeginObject Tag (begins the Type Object for EventBlock)
    NullReference Tag (represents the type of type, which is by convention null)
    4 byte integer Version field for type
    4 byte integer MinimumReaderVersion field for type
    SERIALIZED STRING for FullName Field for type (4 byte length + UTF8 bytes little endian)
    EndObject Tag (ends Type Object)
    DATA FIELDS FOR EVENTBLOCK OBJECT (size of blob + event bytes blob)
    End Object Tag (for EventBlock object)

*/


	public class EventBlockParser
	{

		private int curIdx; // Points to the current index inside the byte array that we are reading
		private bool inObject = false; // Indicates whether we are 

		private int nestedTag = 0; // Indicates how "deep" we are in BEGINOBJECT Tags.

		private bool sawEventTraceObj = false;
		private bool sawStreamHeader = false;
		private ParserProgress progress = new ParserProgress();
		private EventParser eventParser = new EventParser();

		public EventBlockParser() {}

		private void ParseEventPipeFile(Span<byte> buffer)
		{
			// TODO: Skipping over the crappy fields in eventpipefile.h::FastSerialize
			curIdx += (16 + 16 + 16);
		}

		private void ParseEvent(Span<byte> buffer, int bytesRead)
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

		    while(curIdx < bytesRead)
		    {
				int eventSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
				curIdx += 4;
				Console.WriteLine($"Event size: {eventSize}");

				int metadataId = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
				curIdx += 4;
				Console.WriteLine($"MetadataId: {metadataId}");
				
				int threadId = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
				curIdx += 4;
				Console.WriteLine($"ThreadId: {threadId}");

				long timestamp = BitConverter.ToInt64(buffer.Slice(curIdx, sizeof(long)));
				curIdx += 8;
				Console.WriteLine($"Timestamp: {timestamp}");

				Guid activityId = new Guid(buffer.Slice(curIdx, 16).ToArray());
				curIdx += 16;
				Guid RelatedActivityID = new Guid(buffer.Slice(curIdx, 16).ToArray());
				curIdx += 16;

				int payloadSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
				curIdx += 4;
				Console.WriteLine($"Payload Size: {payloadSize}");

				// TODO: Parse Payload
				/*
				Payload Description

				Following this header there is a Payload description. 
				This consists of

				    int FieldCount; // The number of fields in the payload

				Followed by FieldCount number of field Definitions

				    int TypeCode;	 // This is the System.Typecode enumeration
				    <PAYLOAD_DESCRIPTION>
				    string FieldName;    // The 2 byte Unicode, null terminated string representing the Name of the Field

				For primitive types and strings <PAYLOAD_DESCRIPTION> is not present, however if TypeCode == Object (1) 
				then <PAYLOAD_DESCRIPTION> another payload description (that is a field count, followed by a list of field definitions). 
				These can be nested to arbitrary depth.
				*/

				// int fieldCount = BitConverter.ToInt32(buffer.Slice(curIdx, sizeof(4)));
				// curIdx += 4;
				// Console.WriteLine($"Field count: {fieldCount}");

				// for (int i = 0; i < fieldCount; i++)
				// {
				// 	int typeCode = BitConverter.ToInt32(buffer.Slice(curIdx, sizeof(4)));
				// 	// payload description;
				// 	string fieldName = 
				// }

				curIdx += payloadSize;

				// After that is integer (4 bytes) representing the count of bytes needed to represent the stack addresses. If there is no stack this count is 0.
				int stackSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));	
				Console.WriteLine($"Stack size: {stackSize}");
				// TODO: probably can skip stack for now 
				curIdx += stackSize;
			}
		}

		private void ParseStreamHeader(Span<byte> buffer)
		{
			// This should be "!FastSerialization.1"
			// The first byte should be 4 byte integer containing the number 20 
			int streamHeaderLength = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
			curIdx += 4;

			if (streamHeaderLength != 20)
			{
				Console.WriteLine("THE STREAM HEADER LENGTH IS INCORRECT. BAIL OUT");
				return;
			}
			else 
			{
				Console.WriteLine("READ STREAM HEADER: 20 BYTES");
			}

			string streamHeader = System.Text.Encoding.UTF8.GetString(buffer.ToArray(), curIdx, 20);
			curIdx += 20;
			Console.WriteLine($"Stream header: {streamHeader}");
			Console.WriteLine("Done reading stream header");
			sawStreamHeader = true;
		}


		/*
		After the Trace Header comes the EventTrace object, which represents all the data about the Trace as a whole.

		    BeginObject Tag (begins the EventTrace Object)
		    BeginObject Tag (begins the Type Object for EventTrace)
		    NullReference Tag (represents the type of type, which is by convention null)
		    4 byte integer Version field for type
		    4 byte integer MinimumReaderVersion field for type
		    SERIALIZED STRING for FullName Field for type (4 byte length + UTF8 bytes)
		    EndObject Tag (ends Type Object)
		    DATA FIELDS FOR EVENTTRACE OBJECT
		    End Object Tag (for EventTrace object)
		*/
		private void ParseEventTraceObj(Span<byte> buffer)
		{
			// BeginObject Tag (begins the EventTrace Object)
			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
			{
				Console.WriteLine("EventTrace Object: Begin tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}

			// BeginObject Tag (begins the Type Object for EventTrace)
			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
			{
				Console.WriteLine("EventTrace Object Type Object: Begin Tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}

			// NullReference Tag (represents the type of type, which is by convention null)
			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.NullReference)
			{
				Console.WriteLine("EventTrace Object Type Object: NullReference Tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}

			// 4 byte integer Version field for type
			int version = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
			curIdx+=4;
			Console.WriteLine($"Version string: {version}");

			// 4 byte integer Version field for MinimumReaderVersion
			int minReaderVersion = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
			curIdx+=4;
			Console.WriteLine($"Min Reader Version string: {minReaderVersion}");

			// SERIALIZED STRING for FullName Field for type (4 byte length + UTF8 bytes)
			int fullNameLength = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
			curIdx += 4; 
			Console.WriteLine($"Full name length: {fullNameLength}");

			// FullName 
			string fullName = System.Text.Encoding.UTF8.GetString(buffer.Slice(curIdx, fullNameLength).ToArray());
			curIdx += fullNameLength;
			Console.WriteLine($"FullName: {fullName}");

			// EndObject Tag (represents the type of type, which is by convention null)
			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.EndObject)
			{
				Console.WriteLine("EventTrace Object Type Object: EndObject Tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}


			// event blob size
			// int eventBlobSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
			// curIdx += 4; 
			// Console.WriteLine($"Event blob: {eventBlobSize}");

			ParseEventPipeFile(buffer);

			// EndObject Tag (represents the type of type, which is by convention null)
			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.EndObject)
			{
				Console.WriteLine("EventTrace Object Type Object: EndObject Tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}

			// We're done reading the "first object". 
			sawEventTraceObj = true;
		}

		// Parse an entire block. This MAY or MAY NOT return an entire Event block.
		public void ParseBlock(byte[] bufferBytes, int bytesRead)
		{
			curIdx = 0;
			Span<byte> buffer = bufferBytes;

			if (!sawStreamHeader)
			{
				ParseStreamHeader(buffer);
			}

			if (!sawEventTraceObj)
			{
				ParseEventTraceObj(buffer);
			}

			if (curIdx >= bytesRead)
			{
				return;
			}

			while (curIdx < bytesRead)
			{
				if (!progress.sawBeginTag)
				{
					if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
					{
						Console.WriteLine("Event: BeginObject Tag");
						curIdx += 1;
						progress.sawBeginTag = true;
						if (curIdx >= bytesRead) return;
					}
					else
					{
						Console.WriteLine("Fail.....");
					}	
				}

				if (!progress.sawTypeBeginTag)
				{
					if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
					{
						Console.WriteLine("Event (Type): Begin Object Tag");
						curIdx += 1;
						progress.sawTypeBeginTag = true;
						if (curIdx >= bytesRead) return;
					}
					else
					{
						Console.WriteLine("Fail....");
					}
				}

				if (!progress.sawNullRefTag)
				{
					if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.NullReference)
					{
						Console.WriteLine("Event (Type): NULL REF Tag");
						curIdx += 1;
						progress.sawNullRefTag = true;
						if (curIdx >= bytesRead) return;
					}
					else
					{
						Console.WriteLine("Fail....");
					}
				}

				if (!progress.sawVersionStr)
				{
					int version = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
					curIdx += 4;
					progress.sawVersionStr = true;
					progress.version = version;
					Console.WriteLine($"Event version str: {version}");
					if (curIdx >= bytesRead) return;
				}

				if (!progress.sawMinReqVersionStr)
				{
					int minReqVersionStr = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
					curIdx += 4;
					progress.sawMinReqVersionStr = true;
					progress.minReqVersion = minReqVersionStr;
					Console.WriteLine($"Event min req reader version: {minReqVersionStr}");
					if (curIdx >= bytesRead) return;
				}
				
				if (!progress.sawFullNameLen)
				{
					int fullNameLength = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
					curIdx += 4;
					progress.sawFullNameLen = true; 
					progress.fullNameLength = fullNameLength;
					Console.WriteLine($"Event fullname length: {fullNameLength}");
					if (curIdx >= bytesRead) return;
				}

				if (!progress.sawFullNameStr)
				{
					string fullName = System.Text.Encoding.UTF8.GetString(buffer.Slice(curIdx, progress.fullNameLength).ToArray());
					curIdx += progress.fullNameLength;
					progress.sawFullNameStr = true;
					progress.fullName = fullName;
					Console.WriteLine($"FullName: {fullName}");
					if (curIdx >= bytesRead) return;
				}

				if (!progress.sawTypeEndTag)
				{
					if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.EndObject)
					{
						Console.WriteLine("Event (Type): End Object Tag");
						curIdx += 1;
						progress.sawTypeEndTag = true;
						if (curIdx >= bytesRead) return;
					}
					else
					{
						Console.WriteLine("Fail....");
					}
				}


				if (!progress.sawEventBlockSize)
				{
					// first read the size
					int blobSize = BitConverter.ToInt32(buffer.Slice(curIdx, 4));
					curIdx += 4;
					progress.sawEventBlockSize = true;
					progress.eventBlockSize = blobSize;
					Console.WriteLine($"Blob size: {blobSize}");
					if (curIdx >= bytesRead) return;
				}

				if (!progress.sawEventBlock)
				{
					curIdx = eventParser.ParseEvent(buffer, progress.eventBlockSize, bytesRead, curIdx);
					
					if (curIdx >= bytesRead) return; // If this is true, it means we're not done yet.

					progress.sawEventBlock = true;
					/*
					// TODO: Parse the blob
					if (curIdx + progress.eventBlockSize > bytesRead)
					{
						progress.remainingBytesToRead = curIdx + progress.eventBlockSize - bytesRead;
						Console.WriteLine($"Could not read all bytes - {progress.remainingBytesToRead} left to read");
					}
					curIdx += progress.eventBlockSize;
					progress.sawEventBlock = true;
					if (curIdx >= bytesRead) return;
					*/
				}

				if (progress.remainingBytesToRead > 0)
				{
					Console.WriteLine("Trying to read remaining bytes.");
					Console.WriteLine($"curIdx: {curIdx}");
					Console.WriteLine($"bytesRead: {bytesRead}");
					Console.WriteLine($"remBytesToRead: {progress.remainingBytesToRead}");
					curIdx += progress.remainingBytesToRead;
				}

				if (!progress.sawEndTag)
				{
					if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.EndObject)
					{
						Console.WriteLine("Event: End Object Tag");
						curIdx += 1;
						progress.sawEndTag = true;
						Console.WriteLine("DONE PARSING A SINGLE EVENTBLOCK OBJECT");
						progress.Reset(); // Reset progress here since we are done.
						if (curIdx >= bytesRead) return;
					}
					else
					{
						Console.WriteLine("Fail....");
					}
				}

			}
		}
	}


}