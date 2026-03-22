using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TestingLibrary;

namespace TestRunner
{
    class Program
    {
        private static readonly object _consoleLock = new object();
        private static int _passed, _failed;

        static async Task Main()
        {
            var testClasses = typeof(CinemaTests.CinemaBusinessTests).Assembly.GetTypes()
                .Where(t => t.IsDefined(typeof(TestClassAttribute))).ToList();

            Console.WriteLine("=== СРАВНЕНИЕ ===\n");

            Console.WriteLine(">>> ЗАПУСК 1: ПОСЛЕДОВАТЕЛЬНЫЙ");
            Stopwatch swSeq = Stopwatch.StartNew();
            await RunSuite(testClasses, parallel: false);
            swSeq.Stop();
            long timeSeq = swSeq.ElapsedMilliseconds;

            Console.WriteLine("\n" + new string('-', 60));

            Console.WriteLine(">>> ЗАПУСК 2: ПАРАЛЛЕЛЬНЫЙ");
            Stopwatch swPar = Stopwatch.StartNew();
            await RunSuite(testClasses, parallel: true);
            swPar.Stop();
            long timePar = swPar.ElapsedMilliseconds;

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"ИТОГОВОЕ ВРЕМЯ:");
            Console.WriteLine($"Последовательно: {timeSeq} ms");
            Console.WriteLine($"Параллельно:     {timePar} ms");
            Console.WriteLine(new string('=', 60));
            Console.ReadKey();
        }

        private static async Task RunSuite(List<Type> classes, bool parallel)
        {
            _passed = 0; _failed = 0;
            foreach (var type in classes)
            {
                var attr = type.GetCustomAttribute<TestClassAttribute>();
                int maxThreads = parallel ? attr.MaxDegreeOfParallelism : 1;
                var methods = type.GetMethods();
                var before = methods.FirstOrDefault(m => m.IsDefined(typeof(BeforeAttribute)));
                var after = methods.FirstOrDefault(m => m.IsDefined(typeof(AfterAttribute)));
                var tests = methods.Where(m => m.IsDefined(typeof(TestMethodAttribute)) || m.IsDefined(typeof(TestCaseAttribute)));

                using (var semaphore = new SemaphoreSlim(maxThreads))
                {
                    var tasks = new List<Task>();
                    foreach (var m in tests)
                    {
                        var cases = m.GetCustomAttributes<TestCaseAttribute>().ToList();
                        if (!cases.Any()) cases.Add(null);
                        foreach (var tc in cases)
                        {
                            Func<Task> logic = async () => {
                                await semaphore.WaitAsync();
                                try { await Execute(type, m, tc, before, after); }
                                finally { semaphore.Release(); }
                            };
                            if (parallel) tasks.Add(Task.Run(logic)); else await logic();
                        }
                    }
                    if (parallel) await Task.WhenAll(tasks);
                }
            }
            Console.WriteLine($"\n[ИТОГ] Успешно: {_passed}, Провалено: {_failed}");
        }

        private static async Task Execute(Type type, MethodInfo method, TestCaseAttribute tc, MethodInfo bef, MethodInfo aft)
        {
            var instance = Activator.CreateInstance(type);
            var timeout = method.GetCustomAttribute<TimeoutAttribute>();
            string name = $"{method.Name}{(tc != null ? "(" + string.Join(",", tc.Params) + ")" : "")}";

            try
            {
                bef?.Invoke(instance, null);
                Task t = Task.Run(async () => {
                    var res = method.Invoke(instance, tc?.Params);
                    if (res is Task task) await task;
                });

                if (timeout != null && await Task.WhenAny(t, Task.Delay(timeout.Milliseconds)) != t)
                    throw new TestingException($"Timeout {timeout.Milliseconds}ms");

                await t;
                aft?.Invoke(instance, null);
                Interlocked.Increment(ref _passed);
                Log(ConsoleColor.Green, "OK", name);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failed);
                Log(ConsoleColor.Red, "FAIL", $"{name}: {(ex.InnerException ?? ex).Message}");
            }
        }

        private static void Log(ConsoleColor c, string s, string m)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = c;
                Console.Write($" [{s}] ");
                Console.ResetColor();
                Console.WriteLine(m);
            }
        }
    }
}