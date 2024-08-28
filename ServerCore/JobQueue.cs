using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    public class JobQueue : IJobQueue
    {
        Queue<Action> _jobQueue = new Queue<Action>();
        object _lock = new object();
        bool _flush = false; // _flush가 false면 queue를 비우고 있는 중이 아님.

        public void Push(Action job)
        {
            bool flush = false;

            lock (_lock)
            {
                _jobQueue.Enqueue(job);
                if (_flush == false) // queue를 비우고 있는 중이 아니라 비워줘야 함.
                    flush = _flush = true; // 이 스레드에서 Flush()를 할 거라고 변수로 알림.
                    // _flush는 다른 스레드에게 알리기 위한 변수. flush는 본인이 Flush()해야 함을 알리는 변수.
                    // 이렇게 다른 변수를 쓰는 이유는, Flush()를 lock 바깥에서 안전하게 해주기 위해서. 
                    // 작업량이 많은 Flush를 lock을 잡아두고 하면 병목현상 발생.
            }

            if (flush)
                Flush();
        }

        void Flush()
        {
            while (true)
            {
                Action action = Pop();
                if (action == null)
                    return;

                action.Invoke();
            }
        }

        Action Pop()
        {
            lock (_lock)
            {
                if (_jobQueue.Count == 0)
                {
                    _flush = false;
                    return null;
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}
