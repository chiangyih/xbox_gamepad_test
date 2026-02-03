using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace xbox_gamepad.Tests
{
    /// <summary>
    /// Standalone test runner - executes all test methods marked with [TestCase]
    /// </summary>
    public static class TestRunner
    {
        public static void Run()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("Xbox Wireless Controller - Automated Tests");
            Console.WriteLine("===========================================\n");

            var testClass = new XboxControllerTests();
            var methods = testClass.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var testMethods = methods.Where(m =>
                m.GetCustomAttributes(typeof(TestCaseAttribute), false).Length > 0).ToList();

            int passed = 0;
            int failed = 0;
            var results = new List<(string name, bool success, string? error)>();

            foreach (var method in testMethods)
            {
                try
                {
                    Console.Write($"Running {method.Name}... ");
                    method.Invoke(testClass, null);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("? PASSED");
                    Console.ResetColor();
                    passed++;
                    results.Add((method.Name, true, null));
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.InnerException?.Message ?? ex.Message;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("? FAILED");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Error: {errorMsg}");
                    Console.ResetColor();
                    failed++;
                    results.Add((method.Name, false, errorMsg));
                }
            }

            Console.WriteLine("\n===========================================");
            Console.WriteLine("Test Summary");
            Console.WriteLine("===========================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"? Passed: {passed}");
            Console.ResetColor();
            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"? Failed: {failed}");
                Console.ResetColor();
            }
            Console.WriteLine($"Total: {passed + failed}");
            if (passed + failed > 0)
            {
                Console.WriteLine($"Success Rate: {(passed * 100 / (passed + failed))}%");
            }
            Console.WriteLine("===========================================\n");

            if (failed > 0)
            {
                Environment.Exit(1);
            }
        }
    }
}
