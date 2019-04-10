//
using System;
using System.Runtime.InteropServices;

// READ: https://github.com/Microsoft/perfview/blob/ef1b2562ed07b85a0e5386a711d91988ef395208/src/TraceEvent/EventPipe/EventSerialization.md
//

namespace Microsoft.Diagnostics.Tools.Counters
{
	// WARNING: KEEP THIS IN SYNC WITH 
	enum FastSerializerTags
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

		public EventBlockParser() {}

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


		private void ParseEventTraceObj(Span<byte> buffer)
		{
			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
			{
				Console.WriteLine("EventTrace Object: Begin tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}

			if (buffer.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
			{
				Console.WriteLine("EventTrace Object Type Object: Begin Tag");
				curIdx++;
			}
			else
			{
				Console.WriteLine("CRAP");
			}

			if (buffer.Slice(curIdx))
		}

		// Parse an entire block. This MAY or MAY NOT return an entire Event block.
		public void ParseBlock(byte[] buffer)
		{
			curIdx = 0;
			Span<byte> bufferBytes = buffer;

			if (!sawStreamHeader)
			{
				ParseStreamHeader(buffer);
			}

			if (!sawEventTraceObj)
			{
				ParseEventTraceObj(buffer);
			}

			while(curIdx < bufferBytes.Length)
			{
				if (nestedTag == 0)
				{
					Span<byte> tag = bufferBytes.Slice(curIdx, 1); // BEGIN_TAG
					curIdx += 1;

					if (tag[0] == (byte)FastSerializerTags.BeginObject)
					{
						Console.WriteLine("BEGIN TAG - EventBlock");
						nestedTag++;
						inObject = true; // We're inside an object now.
					}
					else
					{
						Console.WriteLine("SOME TRASH CRAP CAME IN... WHAT TO DO?");
					}
				}

				else
				{
					if (bufferBytes.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.BeginObject)
					{
						// We have another begin object tag
						Console.WriteLine("BEGIN TAG - EventBlock Type");
						curIdx+=1;
						nestedTag++;
					}
					if (bufferBytes.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.NullReference)
					{
						// Read NULLREFERENCE tag.
						Console.WriteLine("NULLREF - EventBlock Type of Type");
						curIdx+=1;
					}
					int version = BitConverter.ToInt32(bufferBytes.Slice(curIdx, 4));
					curIdx += 4; 
					Console.WriteLine($"Version string: {version}");

					int minReqVersion = BitConverter.ToInt32(bufferBytes.Slice(curIdx, 4));
					curIdx += 4; 
					Console.WriteLine($"MinReqVersion string: {minReqVersion}");

					int fullNameLength = BitConverter.ToInt32(bufferBytes.Slice(curIdx, 4));
					curIdx += 4; 
					Console.WriteLine($"FullNameLength: {fullNameLength}");

					string fullName = System.Text.Encoding.UTF8.GetString(bufferBytes.ToArray(), curIdx, fullNameLength*2);
					curIdx += (fullNameLength*2);
					Console.WriteLine($"FullName: {fullName}");

					/*
					Span<char> fullNameSpan = MemoryMarshal.Cast<byte, char>(bufferBytes.Slice(curIdx, fullNameLength*2));// UTF-8 encoded strings
					curIdx += (fullNameLength*2);
					*/

					if (bufferBytes.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.EndObject)
					{
						// We have another begin object tag
						Console.WriteLine("END TAG - EventBlock Type");
						curIdx+=1;
						nestedTag--;
					}

					int blobLength = BitConverter.ToInt32(bufferBytes.Slice(curIdx, 4));
					curIdx += 4; 
					Console.WriteLine($"blobLength: {blobLength}");

					string blob = bufferBytes.Slice(curIdx, blobLength).ToString(); // UTF-8 encoded strings
					curIdx += blobLength;
					Console.WriteLine($"blob: {blob}");

					if (bufferBytes.Slice(curIdx, 1)[0] == (byte)FastSerializerTags.EndObject)
					{
						// We have another begin object tag
						Console.WriteLine("END TAG - EventBlock");
						curIdx+=1;
						nestedTag--;
					}
				}
			}
		}
	}


}