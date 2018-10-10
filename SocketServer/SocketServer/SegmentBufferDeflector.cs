using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public class SegmentBufferDeflector
    {
        public static void ShiftBuffer(
            BufferManager bufferManager,
            int shiftStart,
            ref ArraySegment<byte> sessionBuffer,
            ref int sessionBufferCount)
        {
            if((sessionBufferCount - shiftStart)<shiftStart)
            {
                Array.Copy(sessionBuffer.Array, sessionBuffer.Offset + shiftStart,
                    sessionBuffer.Array,sessionBuffer.Offset,sessionBufferCount-shiftStart);
                sessionBufferCount = sessionBufferCount - shiftStart;
            }
            else
            {
                ArraySegment<byte> copyBuffer = bufferManager.BorrowBuffer();
                if(copyBuffer.Count <(sessionBufferCount - shiftStart))
                {
                    bufferManager.ReturnBuffers(copyBuffer);
                    copyBuffer = new ArraySegment<byte>(new byte[sessionBufferCount - shiftStart]);
                }

                Array.Copy(sessionBuffer.Array, sessionBuffer.Offset + shiftStart, copyBuffer.Array, copyBuffer.Offset, sessionBufferCount - shiftStart);
                Array.Copy(copyBuffer.Array, copyBuffer.Offset, sessionBuffer.Array, sessionBuffer.Offset, sessionBufferCount - shiftStart);
                sessionBufferCount = sessionBufferCount - shiftStart;

                bufferManager.ReturnBuffers(copyBuffer);
            }
        }
    }
}
