﻿namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	using System.Collections;
	using System.Collections.Generic;
	
	/*
		这个模块将多个数据包打捆在一起
		由于每个数据包都有最大上限， 向Bundle中写入大量数据将会在内部产生多个MemoryStream
		在send时会全部发送出去
	*/
	public class Bundle : ObjectPool<Bundle>
    {
		public MemoryStream stream = MemoryStream.createObject();
		public List<MemoryStream> streamList = new List<MemoryStream>();
		public int numMessage = 0;
		public int messageLength = 0;
		public Message msgtype = null;
		private int _curMsgStreamIndex = 0;
		
		public Bundle()
		{
		}
		
		public void newMessage(Message mt)
		{
			fini(false);
			
			msgtype = mt;
			numMessage += 1;

			writeUint16(msgtype.id);

			if(msgtype.msglen == -1)
			{
				writeUint16(0);
				messageLength = 0;
			}

			_curMsgStreamIndex = 0;
		}
		
		public void writeMsgLength()
		{
			if(msgtype.msglen != -1)
				return;

			MemoryStream stream = this.stream;
			if(_curMsgStreamIndex > 0)
			{
				stream = streamList[streamList.Count - _curMsgStreamIndex];
			}
			stream.data()[2] = (Byte)(messageLength & 0xff);
			stream.data()[3] = (Byte)(messageLength >> 8 & 0xff);
		}
		
		public void fini(bool issend)
		{
			if(numMessage > 0)
			{
				writeMsgLength();

				streamList.Add(stream);
				stream = MemoryStream.createObject();
			}
			
			if(issend)
			{
				numMessage = 0;
				msgtype = null;
			}

			_curMsgStreamIndex = 0;
		}
		
		public void send(NetworkInterface networkInterface)
		{
			fini(true);
			
			if(networkInterface.valid())
			{
				for(int i=0; i<streamList.Count; i++)
				{
					stream = streamList[i];
					networkInterface.send(stream);
				}
			}
			else
			{
				Dbg.ERROR_MSG("Bundle::send: networkInterface invalid!");  
			}

			// 把不用的MemoryStream放回缓冲池，以减少垃圾回收的消耗
			for (int i = 0; i < streamList.Count; ++i)
			{
				streamList[i].reclaimObject();
			}
			streamList.Clear();
			stream.clear();

			// 我们认为，发送完成，就视为这个bundle不再使用了，
			// 所以我们会把它放回对象池，以减少垃圾回收带来的消耗，
			// 如果需要继续使用，应该重新Bundle.createObject()，
			// 如果外面不重新createObject()而直接使用，就可能会出现莫名的问题，
			// 仅以此备注，警示使用者。
			Bundle.reclaimObject(this);
		}

		public void sendUdp(NetworkInterface networkInterface)
        {
            fini(true);

            if (networkInterface.valid())
            {
                for (int i = 0; i < streamList.Count; i++)
                {
                    stream = streamList[i];
                    networkInterface.sendUdp(stream);
                }
            }
            else
            {
                Dbg.ERROR_MSG("Bundle::send: networkInterface invalid!");
            }

            // 把不用的MemoryStream放回缓冲池，以减少垃圾回收的消耗
            for (int i = 0; i < streamList.Count; ++i)
            {
                streamList[i].reclaimObject();
            }
            streamList.Clear();
            stream.clear();

            // 我们认为，发送完成，就视为这个bundle不再使用了，
            // 所以我们会把它放回对象池，以减少垃圾回收带来的消耗，
            // 如果需要继续使用，应该重新Bundle.createObject()，
            // 如果外面不重新createObject()而直接使用，就可能会出现莫名的问题，
            // 仅以此备注，警示使用者。
            Bundle.reclaimObject(this);
        }

        /// <summary>
        ///检查stream剩余空间是否足够写入新数据,
        ///不足够就把当前stream加到队列尾，然后创建一个新的stream写入数据,
        ///足够就增加消息长度messageLength
        /// </summary>
        /// <param name="v"></param>
		public void checkStream(int v)
		{
            //不足够就把当前stream加到队列尾，然后创建一个新的stream写入数据
			if(v > stream.space())
			{
				streamList.Add(stream);
				stream = MemoryStream.createObject();
				++ _curMsgStreamIndex;
			}
            //足够就增加消息长度
			messageLength += v;
		}
		
		//---------------------------------------------------------------------------------
		public void writeInt8(SByte v)
		{
			checkStream(1);
			stream.writeInt8(v);
		}
	
		public void writeInt16(Int16 v)
		{
			checkStream(2);
			stream.writeInt16(v);
		}
			
		public void writeInt32(Int32 v)
		{
			checkStream(4);
			stream.writeInt32(v);
		}
	
		public void writeInt64(Int64 v)
		{
			checkStream(8);
			stream.writeInt64(v);
		}
		
		public void writeUint8(Byte v)
		{
			checkStream(1);
			stream.writeUint8(v);
		}
	
		public void writeUint16(UInt16 v)
		{
			checkStream(2);
			stream.writeUint16(v);
		}
			
		public void writeUint32(UInt32 v)
		{
			checkStream(4);
			stream.writeUint32(v);
		}
	
		public void writeUint64(UInt64 v)
		{
			checkStream(8);
			stream.writeUint64(v);
		}
		
		public void writeFloat(float v)
		{
			checkStream(4);
			stream.writeFloat(v);
		}
	
		public void writeDouble(double v)
		{
			checkStream(8);
			stream.writeDouble(v);
		}
		
		public void writeString(string v)
		{
			checkStream(v.Length + 1);
			stream.writeString(v);
		}
		
		public void writeBlob(byte[] v)
		{
			checkStream(v.Length + 4);
			stream.writeBlob(v);
		}
    }
} 
