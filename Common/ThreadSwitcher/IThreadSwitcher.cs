using System;
using System.Threading.Tasks;

namespace Common.ThreadSwitcher
{
    public interface IThreadSwitcher
    {
        void SwithchThread(Func<Task> actionThread);
    }
}
