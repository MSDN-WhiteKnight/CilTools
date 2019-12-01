/* CilBytecodeParser library unit tests
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilBytecodeParser.Tests
{
    public static class AssertThat
    {
        public static void NotEmpty<T>(IEnumerable<T> collection,string message)
        {
            Assert.IsTrue(collection.Count() > 0, message);
        }

        public static void HasOnlyOneMatch<T>(IEnumerable<T> collection, Func<T,bool> condition, string message)
        {
            IEnumerable<T> match = collection.Where(condition);
            Assert.AreEqual(1, match.Count(), message);
        }

        public static void HasAtLeastOneMatch<T>(IEnumerable<T> collection, Func<T, bool> condition, string message)
        {
            IEnumerable<T> match = collection.Where(condition);
            Assert.IsTrue(match.Count() > 0, message);
        }
    }
}
