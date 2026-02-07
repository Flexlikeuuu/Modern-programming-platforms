using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TestingLibrary;
using System.Collections.Generic;

namespace TestRunner
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("=== CUSTOM TEST RUNNER: STREAMING SERVICE ===\n");

            var assembly = typeof(CinemaTests.CinemaBusinessTests).Assembly;
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);

            int passed = 0, failed = 0;

            foreach (var type in testClasses)
            {
                Console.WriteLine($"[CLASS] {type.Name}");
                var methods = type.GetMethods();

                var before = methods.FirstOrDefault(m => m.GetCustomAttributes<BeforeAttribute>().Any());
                var after = methods.FirstOrDefault(m => m.GetCustomAttributes<AfterAttribute>().Any());

                var tests = methods.Where(m =>
                    m.GetCustomAttributes<TestMethodAttribute>().Any() ||
                    m.GetCustomAttributes<TestCaseAttribute>().Any());

                foreach (var method in tests)
                {
                    var cases = method.GetCustomAttributes<TestCaseAttribute>().ToList();

                    if (!cases.Any()) cases.Add(null);

                    foreach (var tc in cases)
                    {
                        var instance = Activator.CreateInstance(type);
                        try
                        {
                            before?.Invoke(instance, null);

                            object result;
                            if (tc != null)
                                result = method.Invoke(instance, tc.Params);
                            else
                                result = method.Invoke(instance, null);

                            if (result is Task task) await task;

                            after?.Invoke(instance, null);

                            Console.ForegroundColor = ConsoleColor.Green;
                            var testAttr = method.GetCustomAttribute<TestMethodAttribute>();
                            string desc = testAttr?.Description ?? "";
                            string p = tc != null ? $"({string.Join(", ", tc.Params)})" : "";

                            Console.WriteLine($"  [PASS] {method.Name}{p} {desc}");
                            passed++;
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is TestingException te)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  [FAIL] {method.Name}: {te.Message}");
                            failed++;
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            var err = ex.InnerException ?? ex;
                            Console.WriteLine($"  [ERROR] {method.Name}: {err.GetType().Name} - {err.Message}");
                            failed++;
                        }
                        finally { Console.ResetColor(); }
                    }
                }
            }

            Console.WriteLine($"\nRESULTS: Passed: {passed}, Failed/Errors: {failed}");
            Console.ReadKey();
        }
    }
}