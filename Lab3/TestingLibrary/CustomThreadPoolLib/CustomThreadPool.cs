using System;
using System.Collections.Generic;
using System.Threading;

namespace TestRunner
{
    public class CustomThreadPool : IDisposable
    {
        private readonly Queue<Action> _taskQueue = new Queue<Action>();
        private readonly List<WorkerThread> _threads = new List<WorkerThread>();

        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly int _idleTimeoutMs;
        private bool _disposed = false;

        public int CurrentThreadCount => _threads.Count;
        public int QueueLength => _taskQueue.Count;

        public CustomThreadPool(int minThreads, int maxThreads, int idleTimeoutMs = 5000)
        {
            _minThreads = minThreads;
            _maxThreads = maxThreads;
            _idleTimeoutMs = idleTimeoutMs;

            for (int i = 0; i < _minThreads; i++)
                CreateWorker();
        }

        public void Execute(Action task)
        {
            lock (_taskQueue)
            {
                _taskQueue.Enqueue(task);
                Monitor.Pulse(_taskQueue);

                if (_taskQueue.Count > 0 && _threads.Count < _maxThreads)
                {
                    CreateWorker();
                    Console.WriteLine($"[POOL] Масштабирование ВВЕРХ: {_threads.Count} потоков");
                }
            }
        }

        private void CreateWorker()
        {
            var worker = new WorkerThread(this);
            _threads.Add(worker);
            worker.Start();
        }

        private class WorkerThread
        {
            private readonly CustomThreadPool _pool;
            private readonly Thread _thread;
            private DateTime _lastWorkTime;

            public WorkerThread(CustomThreadPool pool)
            {
                _pool = pool;
                _thread = new Thread(Run) { IsBackground = true };
                _lastWorkTime = DateTime.Now;
            }

            public void Start() => _thread.Start();

            private void Run()
            {
                while (true)
                {
                    Action task = null;
                    lock (_pool._taskQueue)
                    {
                        while (_pool._taskQueue.Count == 0)
                        {
                            if (!_pool._disposed && Monitor.Wait(_pool._taskQueue, _pool._idleTimeoutMs))
                                continue;

                            if (_pool._threads.Count > _pool._minThreads || _pool._disposed)
                            {
                                _pool._threads.Remove(this);
                                Console.WriteLine($"[POOL] Сжатие: поток завершен. Осталось: {_pool._threads.Count}");
                                return;
                            }
                        }
                        task = _pool._taskQueue.Dequeue();
                    }

                    try
                    {
                        task?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[POOL ERROR] Ошибка в потоке: {ex.Message}");
                    }
                    _lastWorkTime = DateTime.Now;
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            lock (_taskQueue) Monitor.PulseAll(_taskQueue);
        }
    }
}