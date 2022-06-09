/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public enum TestCondition
    {
        Never = 0, Always = 1, WindowsOnly = 2, DebugBuildOnly = 3
    }

    public class ConditionalTestAttribute : TestMethodAttribute
    {
        TestCondition cond;
        string mes;

        public ConditionalTestAttribute(TestCondition condition, string message)
        {
            this.cond = condition;
            this.mes = message;
        }

        static bool ShouldRun(TestCondition cond)
        {
            switch (cond)
            {
                case TestCondition.Never: return false;
                case TestCondition.Always: return true;
                case TestCondition.WindowsOnly:
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT) return true;
                    else return false;
                case TestCondition.DebugBuildOnly:
#if DEBUG
                    return true;
#else
                    return false;
#endif
                default:
                    Assert.Fail("Unknown test condition");
                    return false;
            }
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (ShouldRun(this.cond))
            {
                TestResult[] rets = base.Execute(testMethod);
                return rets;
            }

            TestResult res = new TestResult();
            res.Outcome = UnitTestOutcome.Inconclusive;
            res.TestFailureException = new AssertInconclusiveException(this.mes);
            return new TestResult[] { res };
        }
    }
}
