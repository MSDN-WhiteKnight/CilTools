/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Tests.Common
{
    public static class LongTestsRunner
    {
        static object[] emptyArgs = new object[0];

        /// <summary>
        /// Runs the specified test method on the specified object
        /// </summary>
        /// <param name="mi">Test method to run</param>
        /// <param name="instance">
        /// Object for instance tests, or <c>null</c> for static test
        /// </param>
        /// <returns>0 if passed, 1 if failed</returns>
        static int InvokeTestMethod(MethodInfo mi, object instance)
        {
            try 
            {
                mi.Invoke(instance, emptyArgs);
                return 0;
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is AssertFailedException)
                {
                    Console.WriteLine("Test assertion failed");                    
                }
                else
                {
                    Console.WriteLine("Test thrown an exception");
                }

                Console.WriteLine("Test: " + mi.Name);
                Console.WriteLine(ex.InnerException.Message);
                Console.WriteLine(ex.InnerException.StackTrace);
                Console.WriteLine();

                return 1;
            }
        }

        /// <summary>
        /// Runs all long-running tests (classes and methods with <see cref="LongTestAttribute"/>) 
        /// in the specified assembly
        /// </summary>
        /// <param name="testAssembly">Test assembly (standard reflection impl)</param>
        /// <returns>The number of failed tests</returns>        
        public static int Run(Assembly testAssembly)
        {
            int failed = 0;
            int total = 0;

            Console.WriteLine("Test assembly: " + testAssembly.GetName().Name);
            Type[] types = testAssembly.GetTypes();
            Console.WriteLine("Running tests...");

            //find test classes

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].GetCustomAttribute<LongTestAttribute>() == null)
                {
                    continue;
                }

                //find static tests

                MethodInfo[] methods = types[i].GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                    );

                for (int j = 0; j < methods.Length; j++)
                {
                    if (methods[j].GetCustomAttribute<LongTestAttribute>() != null)
                    {
                        total++;
                        failed += InvokeTestMethod(methods[j], null);
                    }
                }

                //find instance tests

                methods = types[i].GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );

                object instance = null;
                if(methods.Length>0)instance=Activator.CreateInstance(types[i]);

                for (int j = 0; j < methods.Length; j++)
                {
                    if (methods[j].GetCustomAttribute<LongTestAttribute>() != null)
                    {
                        total++;
                        failed += InvokeTestMethod(methods[j], instance);
                    }
                }
            }

            ConsoleColor color = Console.ForegroundColor;

            if (total == 0)
            {
                Console.WriteLine("No tests found");
            }
            else if (failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All tests passed");
                Console.ForegroundColor = color;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Some tests failed!");
                Console.ForegroundColor = color;
                Console.WriteLine("Failed: {0} Passed: {1}",failed,total-failed);
            }

            Console.WriteLine("Total tests: "+total.ToString());

            return failed;
        }
    }
}
