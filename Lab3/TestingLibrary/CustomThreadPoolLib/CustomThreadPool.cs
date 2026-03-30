using System;
using System.Collections.Generic;
using System.Threading;

namespace TestRunner
{
    public class CustomThreadPool : IDisposable
    {
        private readonly Queue<Action> _taskQueue = new Queue<Action>();
        private readonly List<Thread> _workers = new List<Thread>();

        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly int _idleTimeoutMs;
        private bool _stopping = false;

        public int CurrentThreadCount { get { lock (_taskQueue) return _workers.Count; } }
        public int QueueLength { get { lock (_taskQueue) return _taskQueue.Count; } }

        public CustomThreadPool(int min, int max, int idleMs = 3000)
        {
            _minThreads = min;
            _maxThreads = max;
            _idleTimeoutMs = idleMs;

            lock (_taskQueue)
            {
                for (int i = 0; i < _minThreads; i++) CreateWorker();
            }
        }

        public void Execute(Action task)
        {
            lock (_taskQueue)
            {
                if (_stopping) return;

                _taskQueue.Enqueue(task);

                if (_taskQueue.Count > 0 && _workers.Count < _maxThreads)
                {
                    CreateWorker();
                    LogPool($"[POOL] + Масштабирование ВВЕРХ. Потоков: {_workers.Count}");
                }

                Monitor.Pulse(_taskQueue);
            }
        }

        private void CreateWorker()
        {
            var thread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"PoolWorker-{Guid.NewGuid().ToString().Substring(0, 4)}"
            };
            _workers.Add(thread);
            thread.Start();
        }

        private void WorkerLoop()
        {
            while (true)
            {
                Action task = null;
                lock (_taskQueue)
                {
                    while (_taskQueue.Count == 0 && !_stopping)
                    {
                        if (!Monitor.Wait(_taskQueue, _idleTimeoutMs))
                        {
                            if (_workers.Count > _minThreads)
                            {
                                _workers.Remove(Thread.CurrentThread);
                                LogPool($"[POOL] - Сжатие (простой). Потоков: {_workers.Count}");
                                return; 
                            }
                        }
                    }

                    if (_stopping && _taskQueue.Count == 0) return;
                    if (_taskQueue.Count > 0) task = _taskQueue.Dequeue();
                }

                if (task != null)
                {
                    try { task(); }
                    catch (Exception ex) { LogPool($"[POOL ERROR] {ex.Message}"); }
                }
            }
        }

        private void LogPool(string msg)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {msg}");
                Console.ResetColor();
            }
        }

        public void Dispose()
        {
            lock (_taskQueue)
            {
                _stopping = true;
                Monitor.PulseAll(_taskQueue);
            }
        }
    }
}