using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;

namespace Common.ThreadSwitcher
{
    public class ThreadSwitcher : ProducerConsumer<Func<Task>>, IThreadSwitcher
    {
        public ThreadSwitcher(string componentName, ILog log) : base(componentName, log)
        {
        }

        protected override Task Consume(Func<Task> action)
        {
            return action();
        }

        public void SwithchThread(Func<Task> actionThread)
        {
            Produce(actionThread);
        }
    }
}
