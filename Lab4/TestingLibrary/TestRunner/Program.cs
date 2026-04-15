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
        private static int _passed, _failed, _total;
        private static CustomThreadPool _pool;
        private static readonly object _consoleLock = new object();
        private static Stopwatch _sw;

        static void Main()
        {
            Console.Title = "Unit Test Runner Lab 4";
            _pool = new CustomThreadPool(3, 8);
            _pool.PoolStateChanged += (msg, count, color) => LogSystem(color, msg);

            Console.WriteLine("======================================================");
            Console.WriteLine("   CUSTOM TESTING FRAMEWORK - ЛАБОРАТОРНАЯ РАБОТА 4");
            Console.WriteLine("======================================================");
            Console.WriteLine("Выберите фильтр: 1-Все, 2-Приоритет 1, 3-Категория Billing");
            Console.Write("Ваш выбор: ");

            char choice = Console.ReadKey().KeyChar;
            Func<MethodInfo, bool> filter = choice switch
            {
                '2' => m => m.GetCustomAttribute<PriorityAttribute>()?.Level == 1,
                '3' => m => m.GetCustomAttribute<CategoryAttribute>()?.Name == "Billing",
                _ => m => true
            };

            var testList = Prepare(new[] { typeof(CinemaBusinessTests) }, filter);
            _total = testList.Count;

            Console.WriteLine($"\n\n[START] Начинаем выполнение {_total} тест-кейсов...");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"STATUS",-8} | {"THREAD",-12} | {"PRIO",-4} | {"TEST CASE (PARAMS)"}");
            Console.WriteLine(new string('-', 80));

            _sw = Stopwatch.StartNew();
            foreach (var test in testList) _pool.Execute(test);

            while ((_passed + _failed) < _total) Thread.Sleep(50);
            _sw.Stop();

            PrintFinal();
            _pool.Dispose();
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static List<Action> Prepare(Type[] types, Func<MethodInfo, bool> filterDelegate)
        {
            var list = new List<Action>();
            foreach (var type in types)
            {
                var methods = type.GetMethods().Where(m =>
                    (m.IsDefined(typeof(TestMethodAttribute)) || m.IsDefined(typeof(TestCaseAttribute))) && filterDelegate(m));

                var bef = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(BeforeAttribute)));
                var aft = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(AfterAttribute)));

                foreach (var m in methods)
                {
                    var cases = new List<object[]>();
                    cases.AddRange(m.GetCustomAttributes<TestCaseAttribute>().Select(a => a.Params));

                    var md = m.GetCustomAttribute<MemberDataAttribute>();
                    if (md != null)
                    {
                        var method = type.GetMethod(md.MethodName);
                        cases.AddRange((IEnumerable<object[]>)method.Invoke(null, null));
                    }

                    if (cases.Count == 0) cases.Add(null);

                    foreach (var args in cases)
                    {
                        var p = m.GetCustomAttribute<PriorityAttribute>()?.Level.ToString() ?? "-";
                        list.Add(() => Exec(type, m, args, bef, aft, p));
                    }
                }
            }
            return list;
        }

        private static void Exec(Type t, MethodInfo m, object[] args, MethodInfo b, MethodInfo a, string pr)
        {
            var inst = Activator.CreateInstance(t);
            string name = $"{m.Name}({(args != null ? string.Join(", ", args) : "")})";
            try
            {
                b?.Invoke(inst, null);
                var res = m.Invoke(inst, args);
                if (res is Task task) task.GetAwaiter().GetResult();
                a?.Invoke(inst, null);
                Interlocked.Increment(ref _passed);
                LogLine(ConsoleColor.Green, "PASSED", pr, name);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failed);
                LogLine(ConsoleColor.Red, "FAILED", pr, $"{name} -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static void LogLine(ConsoleColor c, string st, string pr, string name)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = c;
                Console.Write($"{st,-8} ");
                Console.ResetColor();
                Console.WriteLine($"| {Thread.CurrentThread.Name,-12} | {pr,-4} | {name}");
            }
        }

        private static void LogSystem(ConsoleColor c, string msg)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = c;
                Console.WriteLine($" >>> {msg}");
                Console.ResetColor();
            }
        }

        private static void PrintFinal()
        {
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("ФИНАЛЬНЫЙ ОТЧЕТ ТЕСТИРОВАНИЯ");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Запущено тестов: {_total}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Успешно:         {_passed}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Провалено:       {_failed}");
            Console.ResetColor();
            Console.WriteLine($"Время работы:    {_sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Процент успеха:  {(_passed * 100.0 / _total):F1}%");
            Console.WriteLine(new string('=', 60));
        }
    }
}