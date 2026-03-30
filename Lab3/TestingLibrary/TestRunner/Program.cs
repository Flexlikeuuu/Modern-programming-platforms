using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using TestingLibrary;
using CinemaTests;

namespace TestRunner
{
    class Program
    {
        private static readonly object _consoleLock = new object();
        private static int _passed, _failed;
        private static CustomThreadPool _pool;

        static void Main()
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

            WaitForQueueEmpty(2000);

            Console.WriteLine("\n>>> ЭТАП 2: ОЖИДАНИЕ БЕЗДЕЙСТВИЯ (сжатие пула)...");
            Thread.Sleep(5000);

            Console.WriteLine("\n>>> ЭТАП 3: РЕДКИЕ ЗАДАЧИ (подача каждые 200мс)");
            for (int i = 0; i < 25; i++)
            {
                _pool.Execute(allTests[i % allTests.Count]);
                Thread.Sleep(200);
            }

            WaitForQueueEmpty(500);
            Thread.Sleep(1000);

            sw.Stop();

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"ИТОГИ МОДЕЛИРОВАНИЯ:");
            Console.WriteLine($"Всего запусков: {_passed + _failed}");
            Console.WriteLine($"Успешно:  {_passed}");
            Console.WriteLine($"Провалено: {_failed}");
            Console.WriteLine($"Общее время: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Текущих потоков в пуле: {_pool.CurrentThreadCount}");
            Console.WriteLine($"Очередь задач: {_pool.QueueLength}");
            Console.WriteLine(new string('=', 60));

            _pool.Dispose();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void WaitForQueueEmpty(int checkIntervalMs)
        {
            while (_pool.QueueLength > 0)
            {
                Thread.Sleep(checkIntervalMs);
            }
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

            Exception testException = null;
            bool completed = false;
            bool timedOut = false;

            try
            {
                bef?.Invoke(instance, null);

                var completedEvent = new ManualResetEvent(false);
                Exception executionException = null;

                var testThread = new Thread(() =>
                {
                    try
                    {
                        var res = method.Invoke(instance, tc?.Params);
                        if (res is Task task)
                        {
                            task.GetAwaiter().GetResult();
                        }
                        completed = true;
                    }
                    catch (Exception ex)
                    {
                        executionException = ex;
                    }
                    finally
                    {
                        completedEvent.Set();
                    }
                });

                testThread.IsBackground = true;
                testThread.Start();

                int timeout = timeoutAttr?.Milliseconds ?? 5000;
                bool waitCompleted = completedEvent.WaitOne(timeout);

                if (!waitCompleted)
                {
                    timedOut = true;
                    testThread.Interrupt();
                    throw new TestingException($"Timeout {timeout}ms");
                }

                if (executionException != null)
                {
                    throw executionException;
                }

                aft?.Invoke(instance, null);

                Interlocked.Increment(ref _passed);
                Log(ConsoleColor.Green, "OK", $"{name} (Thread: {Thread.CurrentThread.ManagedThreadId})");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failed);
                var message = ex.InnerException?.Message ?? ex.Message;
                if (timedOut)
                {
                    message = $"Timeout {timeoutAttr?.Milliseconds ?? 5000}ms";
                }
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