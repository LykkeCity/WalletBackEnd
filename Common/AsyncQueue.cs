using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class AsyncQueue<T> 
    {
        private readonly Queue<T> _objectQueue = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T>> _waitersQueue = new Queue<TaskCompletionSource<T>>();

        private readonly object _gate = new object();



        public Task ForEachAsync(Func<T, Task> asyncAction)
        {
            if (asyncAction == null) throw new ArgumentNullException("asyncAction");

            return ForEachAsync(asyncAction, CancellationToken.None);
        }

        public async Task ForEachAsync(Func<T, Task> asyncAction, CancellationToken ct)
        {
            if (asyncAction == null) throw new ArgumentNullException("asyncAction");
            if (ct == null) throw new ArgumentNullException("ct");

            while (!ct.IsCancellationRequested)
            {
                await DequeueAsync(ct).ContinueWith(t => asyncAction(t.Result), ct).Unwrap();
            }

            ct.ThrowIfCancellationRequested();
        }

        public Task<T> DequeueAsync()
        {
            return DequeueAsync(CancellationToken.None);
        }

        public Task<T> DequeueAsync(CancellationToken ct)
        {
            if (ct == null) throw new ArgumentNullException("ct");

            lock (_gate)
            {
                if (_objectQueue.Count > 0)
                {
                    return Task.FromResult(_objectQueue.Dequeue());
                }

                var tcs = new TaskCompletionSource<T>();

                    ct.Register(state => ((TaskCompletionSource<T>)state).TrySetCanceled(), tcs);

                _waitersQueue.Enqueue(tcs);

                return tcs.Task;
            }
        }

        public void Enqueue(T objToEnqueue)
        {
            TaskCompletionSource<T> waiterToRelease = null;
            T objToDequeue = default(T);

            lock (_gate)
            {
                if (_waitersQueue.Count > 0)
                {
                    Debug.Assert(_objectQueue.Count == 0, "_objectQueue.Count == 0");

                    waiterToRelease = _waitersQueue.Dequeue();
                    objToDequeue = objToEnqueue;
                }
                else
                {
                    _objectQueue.Enqueue(objToEnqueue);
                }
            }

            if (waiterToRelease != null)
            {
                waiterToRelease.TrySetResult(objToDequeue);
            }
        }
    }
}