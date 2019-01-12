using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Hotoke.Common
{
    public static class TaskUtility
    {
        private static readonly ConcurrentQueue<Action> _queue =
            new ConcurrentQueue<Action>();
        private static readonly int _immortalTaskLimit = 5;
        private static readonly InterlockedCount _immortalTaskCount = new InterlockedCount();
        private static readonly int _transientTaskLimit = Environment.ProcessorCount * 2;
        private static readonly InterlockedCount _transientTaskCount = new InterlockedCount();
        private static readonly InterlockedCount _idleTaskCount = new InterlockedCount();

        static TaskUtility()
        {
            if(int.TryParse(ConfigurationManager.AppSettings["immortaltasklimit"], out int immortalTaskLimit))
            {
                _immortalTaskLimit = immortalTaskLimit;
            }
        }

        public static void Run(Action action)
        {
            if(action == null)
            {
                return;
            }

            if(_idleTaskCount.Count > 0 || 
                (_immortalTaskCount.Count + _transientTaskCount.Count) >= (_immortalTaskLimit + _transientTaskLimit))
            {
                _queue.Enqueue(action);
                return;
            }

            if(_immortalTaskCount.Increment() <= _immortalTaskLimit)
            {
                Task.Run(() => CreateImmortalTask(action));
                return;
            }
            else
            {
                _immortalTaskCount.Decrement();
            }

            if(_transientTaskCount.Increment() <= _transientTaskLimit)
            {
                Task.Run(() => CreateTransientTask(action));
                return;
            }
            else
            {
                _transientTaskCount.Decrement();
            }

            _queue.Enqueue(action);
        }

        private static void CreateImmortalTask(Action action)
        {
            while(true)
            {
                if(action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch{}
                }

                if(_queue.Count <= 0)
                {
                    _idleTaskCount.Increment();
                    SpinWait.SpinUntil(() => _queue.Count > 0);
                    _idleTaskCount.Decrement();
                }

                _queue.TryDequeue(out action);
            }
        }

        private static void CreateTransientTask(Action action)
        {
            while(true)
            {
                if(action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch{}
                }

                if(_queue.Count <= 0)
                {
                    _transientTaskCount.Decrement();
                    return;
                }

                _queue.TryDequeue(out action);
            }
        }
    }
}