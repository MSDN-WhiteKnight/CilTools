﻿/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common.Attributes
{
    public enum TestCondition
    {
        Never = 0, Always = 1, WindowsOnly = 2, DebugBuildOnly = 3, ReleaseBuildOnly = 4, NetFrameworkOnly = 5
    }

    public class ConditionalTestAttribute : TestMethodAttribute
    {
        TestCondition cond;
        string mes;

#if DEBUG
        const bool IS_DEBUG_BUILD = true;
#else
        const bool IS_DEBUG_BUILD = false;
#endif

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
                    return IS_DEBUG_BUILD;
                case TestCondition.ReleaseBuildOnly:
                    return !IS_DEBUG_BUILD;
                case TestCondition.NetFrameworkOnly:
                    AssemblyName an = typeof(object).Assembly.GetName();
                    return an.Name.Equals("mscorlib", StringComparison.Ordinal);
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
