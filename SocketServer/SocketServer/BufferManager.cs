using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    //思路就是一次性申请一个缓存片(byte[])，然后切成很多个缓存块，
    //通过ArraySegment把引用保存下来以供使用
    public class BufferManager
    {
        #region  Property
        public int ChunkSize
        {
            get
            {
                return _chunkSize;
            }
        }

        public int SegmentCount
        {
            get
            {
                return _segments.Count;
            }
        }

        public int SegmentChunksCount
        {
            get
            {
                return _segmentChunks;
            }
        }

        public int AvaiableBuffers
        {
            get
            {
                return _buffers.Count;
            }
        }

        public int TotalBufferSize
        {
            get
            {
                return _segments.Count * _segmentSize;
            }
        }
        #endregion

        public BufferManager(int segmentChunks, int chunkSize, int initialSegments, bool allowedToCreateMemory)
        {
            if (segmentChunks < 0)
            {
                throw new ArgumentException("segmentChunks");
            }
            if (chunkSize < 0)
            {
                throw new ArgumentException("chunkSize");
            }
            if (initialSegments < 0)
            {
                throw new ArgumentException("initialSegments");
            }

            _segmentChunks = segmentChunks;
            _chunkSize = chunkSize;
            _segmentSize = _segmentChunks * _chunkSize;

            _segments = new List<byte[]>();

            _allowedToCreateMemory = true;
            for (int i = 0; i<initialSegments;i++)
            {
                CreateNewSegment(true);
            }
            _allowedToCreateMemory = allowedToCreateMemory;
        }

        private void CreateNewSegment(bool forceCreation)
        {
            if(!_allowedToCreateMemory)
            {
                throw new UnableToCreateMemoryException();
            }
            lock(_creatingNewSegmentLock)
            {
                if(!forceCreation && _buffers.Count > _segmentChunks / 2)
                {
                    return;
                }
                byte[] bytes = new byte[_segmentSize];
                _segments.Add(bytes);

                for(int i =0;i<_segmentChunks;i++)
                {
                    var chunk = new ArraySegment<byte>(bytes, i * _chunkSize, _chunkSize);
                    _buffers.Push(chunk);
                }
            }
        }

        public ArraySegment<byte> BorrowBuffer()
        {
            int trial = 0;
            while(trial<TrialsCount)
            {
                ArraySegment<byte> result;
                if(_buffers.TryPop(out result))
                {
                    return result;
                }
                CreateNewSegment(false);
                trial++;
            }
            throw new UnableToAllocateBufferException();
        }

        public IEnumerable<ArraySegment<byte>> BrrowBuffer(int count)
        {
            var result = new ArraySegment<byte>[count];
            var trial = 0;
            var totalReceived = 0;

            try
            {
                while(trial<TrialsCount)
                {
                    ArraySegment<byte> piece;
                    while(totalReceived<count)
                    {
                        if (!_buffers.TryPop(out piece))
                        {
                            break;
                        }
                        result[totalReceived] = piece;
                        ++totalReceived;
                    }
                    if(totalReceived==count)
                    {
                        return result;
                    }
                    CreateNewSegment(false);
                    trial++;
                }
                throw new UnableToAllocateBufferException();
            }
            catch
            {
                if(totalReceived>0)
                {
                    ReturnBuffers(result.Take(totalReceived));
                }
                throw;
            }
        }

        public void ReturnBuffers(ArraySegment<byte> buffer)
        {
            if(ValidateBuffer(buffer))
            {
                _buffers.Push(buffer);
            }
        }

        public void ReturnBuffers(IEnumerable<ArraySegment<byte>> buffers)
        {
            if(_buffers == null)
            {
                throw new ArgumentNullException("buffer");
            }
            foreach(var buf in buffers)
            {
                if(ValidateBuffer(buf))
                {
                    _buffers.Push(buf);
                }
            }
        }

        private bool ValidateBuffer(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null ||buffer.Count==0||buffer.Array.Length<buffer.Offset+buffer.Count)
            {
                return false;
            }
            if(buffer.Count!=_chunkSize)
            {
                return false;
            }
            return true;
        }

        private const int TrialsCount = 100;
        // 一个缓存片里的可用缓存块的个数
        private readonly int    _segmentChunks;
        //缓存块的大小
        private readonly int    _chunkSize;
        //缓存片的大小
        private readonly int    _segmentSize;
        private readonly bool _allowedToCreateMemory;

        private readonly object _creatingNewSegmentLock = new object();
        //缓存片的持有集合
        private readonly List<byte[]> _segments;
        //用于存放缓存快的集合，引用指向缓存片
        private readonly ConcurrentStack<ArraySegment<byte>> _buffers = new ConcurrentStack<ArraySegment<byte>>();
    }
}
