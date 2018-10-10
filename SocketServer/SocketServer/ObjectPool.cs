using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public abstract class ObjectPool<T> : IDisposable
    {
        private readonly ConcurrentBag<T> _bag;
        private readonly object _bagLock = new object();
        private bool _isDisposed;

        protected abstract T Create();
        public ObjectPool()
        {
            _bag = new ConcurrentBag<T>();
        }

        public int Count
        {
            get
            {
                return _bag.Count;
            }
        }

        public bool IsDisposed
        {
            get
            {
                lock (_bagLock)
                {
                    return _isDisposed;
                }
            }
        }

        public void Add(T item)
        {
            if(item == null)
            {
                throw new ArgumentNullException("Item is null");
            }
            _bag.Add(item);
        }

        public T Take()
        {
            T item;
            return _bag.TryTake(out item) ? item : Create();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
              lock(_bagLock)
            {
                if(_isDisposed)
                {
                    return;
                }

                if(disposing)
                {
                    for(int i =0;i<this.Count;i++)
                    {
                        var item = this.Take() as IDisposable;
                        if(item !=null)
                        {
                            item.Dispose();
                        }
                    }
                    _isDisposed = true;
                }
            }
        }
    }
}
