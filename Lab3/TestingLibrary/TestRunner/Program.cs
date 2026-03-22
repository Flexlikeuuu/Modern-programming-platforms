using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TestingLibrary;
using CinemaTests;

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
        private int _activeThreads = 0;

        public int CurrentThreadCount => _workers.Count;
        public int QueueLength { get { lock (_taskQueue) return _taskQueue.Count; } }

        public CustomThreadPool(int min, int max, int idleMs = 3000)
        {
            _minThreads = min;
            _maxThreads = max;
            _idleTimeoutMs = idleMs;

            for (int i = 0; i < _minThreads; i++) CreateWorker();
        }

        public void Execute(Action task)
        {
            lock (_taskQueue)
            {
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
            var thread = new Thread(WorkerLoop) { IsBackground = true, Name = $"PoolWorker-{Guid.NewGuid().ToString().Substring(0, 4)}" };
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
                    Interlocked.Increment(ref _activeThreads);
                    try { task(); }
                    catch (Exception ex) { LogPool($"[POOL ERROR] {ex.Message}"); }
                    Interlocked.Decrement(ref _activeThreads);
                }
            }
        }

        private void LogPool(string msg)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        public void Dispose()
        {
            lock (_taskQueue) { _stopping = true; Monitor.PulseAll(_taskQueue); }
        }
    }

    class Program
    {
        private static readonly object _consoleLock = new object();
        private static int _passed, _failed;
        private static CustomThreadPool _pool;

        static async Task Main()
        {
            Console.Title = "Custom ThreadPool Test Runner";

            _pool = new CustomThreadPool(min: 2, max: 8, idleMs: 3000);

            var testTypes = typeof(CinemaBusinessTests).Assembly.GetTypes()
                .Where(t => t.IsDefined(typeof(TestClassAttribute))).ToList();

            var allTests = PrepareTestList(testTypes);

            Console.WriteLine($"Всего тестов подготовлено: {allTests.Count}");
            Console.WriteLine("------------------------------------------------------\n");

            Stopwatch sw = Stopwatch.StartNew();

            Console.WriteLine(">>> ЭТАП 1: ПИКОВАЯ НАГРУЗКА (30 тестов)");
            for (int i = 0; i < 30; i++) _pool.Execute(allTests[i % allTests.Count]);

            await Task.Delay(2000);

            Console.WriteLine("\n>>> ЭТАП 2: ОЖИДАНИЕ БЕЗДЕЙСТВИЯ (сжатие пула)...");
            await Task.Delay(5000);

            Console.WriteLine("\n>>> ЭТАП 3: РЕДКИЕ ЗАДАЧИ (подача каждые 200мс)");
            for (int i = 0; i < 25; i++)
            {
                _pool.Execute(allTests[i % allTests.Count]);
                await Task.Delay(200);
            }

            while (_pool.QueueLength > 0) await Task.Delay(500);
            await Task.Delay(1000);

            sw.Stop();

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"ИТОГИ МОДЕЛИРОВАНИЯ:");
            Console.WriteLine($"Всего запусков: {_passed + _failed}");
            Console.WriteLine($"Успешно:  {_passed}");
            Console.WriteLine($"Провалено: {_failed}");
            Console.WriteLine($"Общее время: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Текущих потоков в пуле: {_pool.CurrentThreadCount}");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static List<Action> PrepareTestList(List<Type> classes)
        {
            var list = new List<Action>();
            foreach (var type in classes)
            {
                var methods = type.GetMethods();
                var before = methods.FirstOrDefault(m => m.IsDefined(typeof(BeforeAttribute)));
                var after = methods.FirstOrDefault(m => m.IsDefined(typeof(AfterAttribute)));
                var tests = methods.Where(m => m.IsDefined(typeof(TestMethodAttribute)) || m.IsDefined(typeof(TestCaseAttribute)));

                foreach (var m in tests)
                {
                    var cases = m.GetCustomAttributes<TestCaseAttribute>().ToList();
                    if (!cases.Any()) cases.Add(null);
                    foreach (var tc in cases)
                    {
                        list.Add(() => RunSingleTest(type, m, tc, before, after));
                    }
                }
            }
            return list;
        }

        private static void RunSingleTest(Type type, MethodInfo method, TestCaseAttribute tc, MethodInfo bef, MethodInfo aft)
        {
            var instance = Activator.CreateInstance(type);
            string name = $"{method.Name}{(tc != null ? "(" + string.Join(",", tc.Params) + ")" : "")}";
            var timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();

            try
            {
                bef?.Invoke(instance, null);

                var task = Task.Run(async () => {
                    var res = method.Invoke(instance, tc?.Params);
                    if (res is Task t) await t;
                });

                int timeout = timeoutAttr?.Milliseconds ?? 5000;
                if (!task.Wait(timeout)) throw new TestingException($"Timeout {timeout}ms");

                aft?.Invoke(instance, null);

                Interlocked.Increment(ref _passed);
                Log(ConsoleColor.Green, "OK", $"{name} (Thread: {Thread.CurrentThread.ManagedThreadId})");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failed);
                var message = ex.InnerException?.Message ?? ex.Message;
                Log(ConsoleColor.Red, "FAIL", $"{name}: {message}");
            }
        }

        private static void Log(ConsoleColor c, string status, string msg)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[{DateTime.Now:HH:mm:ss.fff}]");
                Console.ForegroundColor = c;
                Console.Write($" [{status}] ");
                Console.ResetColor();
                Console.WriteLine(msg);
            }
        }
    }
}