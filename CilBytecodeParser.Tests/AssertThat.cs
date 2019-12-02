/* CilBytecodeParser library unit tests
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CilBytecodeParser;
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

        public static void IsCorrect(CilGraph graph)
        {
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            if (nodes.Length == 0) return; //graph without nodes is useless, but still correct

            for (int i = 1; i < nodes.Length; i++)
            {
                Assert.AreSame(nodes[i - 1].Next, nodes[i], "'Next' property should point to the next node");
                Assert.AreSame(nodes[i].Previous, nodes[i - 1], "'Previous' property should point to the previous node");
                Assert.IsNotNull(nodes[i].Previous, "Nodes except first one should not have null as previous node");
                Assert.IsNotNull(nodes[i - 1].Next, "Nodes except last one should not have null as next node");
            }

            CilGraphNode first = nodes[0];
            Assert.IsNull(first.Previous, "The first node in the graph should have null as the previous node");

            CilGraphNode last = nodes[nodes.Length - 1];
            Assert.IsNull(last.Next, "The last node in the graph should have null as the next node");

        }
    }
}
