using System;
using System.Threading.Tasks;
using Common.Log;

namespace Common
{
    // Таймер, который исполняет метод Execute каждую минуту, причем в 00 её секунду
    public abstract class TimerEachMinute : IStarter
    {
        private readonly string _componentName;
        private readonly ILog _log;
        private readonly int _eachMinute;
        private readonly object _startLockObject = new object();

        protected TimerEachMinute(string componentName, ILog log, int eachMinute = 1)
        {
            _componentName = componentName;
            _log = log;
            _eachMinute = eachMinute;
        }


        private DateTime _nextTick = DateTime.UtcNow.RoundToMinute();


        private void LogFatalError(Exception exception)
        {
            try
            {
               _log.WriteFatalError(_componentName, "ThreadMethod", "", exception);
            }
            catch (Exception)
            {
                
            }
            
        }

        private bool _working;
        private async Task ThreadMethod()
        {
            while (_working)
            {
                try
                {
                    var nowDateTime = DateTime.UtcNow;
                    if (nowDateTime >= _nextTick)
                    {
                        try
                        {
                            await Execute(_nextTick);
                        }
                        finally
                        {
                            _nextTick = DateTime.UtcNow.RoundToMinute().AddMinutes(_eachMinute);
                        }
                    }

                    await Task.Delay(300);
                }
                catch (Exception exception)
                {
                    LogFatalError(exception);
                }

            }
        }

        protected abstract Task Execute(DateTime tickTime);

        public void Start()
        {
            lock (_startLockObject)
            {
                if (_working)
                    return;

                _working = true;

                Task.Run(async () => { await ThreadMethod(); });
            }
        }

        public void Stop()
        {
            _working = false;
        }

    }
}
