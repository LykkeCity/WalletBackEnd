using System;
using System.Threading.Tasks;

namespace Common.ThreadSwitcher
{
    public class ThreadSwitcherMock : IThreadSwitcher
    {
        public void SwithchThread(Func<Task> actionThread)
        {
            actionThread();
        }
    }
}

