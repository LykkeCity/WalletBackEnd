using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _gate;

        private readonly Task<Releaser> _releaser;
        private readonly Task<Releaser> _releaserWhenCanceled;

        public AsyncLock()
        {
            this._gate = new SemaphoreSlim(1, 1);
            this._releaser = Task.FromResult(new Releaser(this, false));
            this._releaserWhenCanceled = Task.FromResult(new Releaser(this, true));
        }

        public void Dispose()
        {
            try
            {
                this._gate.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public Task<Releaser> LockAsync()
        {
            return this.LockAsync(CancellationToken.None);
        }

        public Task<Releaser> LockAsync(CancellationToken cancellation)
        {
            Task wait = this._gate.WaitAsync(cancellation);

            return wait.IsCompleted
                ? (wait.IsCanceled ? this._releaserWhenCanceled : this._releaser)
                       : wait.ContinueWith(
                           (t, state) => new Releaser((AsyncLock)state, t.IsCanceled), 
                           this, 
                           CancellationToken.None, 
                           TaskContinuationOptions.ExecuteSynchronously, 
                           TaskScheduler.Default);
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;
            private readonly bool _isCanceled;

            internal Releaser(AsyncLock toRelease, bool isCanceled)
            {
                if (toRelease == null) throw new ArgumentNullException(nameof(toRelease));

                this._toRelease = toRelease;
                this._isCanceled = isCanceled;
            }

            public void Dispose()
            {
                if (!this._isCanceled)
                {
                    this._toRelease._gate.Release();
                }
            }
        }
    }
}