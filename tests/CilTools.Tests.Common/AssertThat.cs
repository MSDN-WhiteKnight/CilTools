/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public static class AssertThat
    {
        static void Fail(string message) 
        {
            throw new AssertFailedException(message);
        }

        public static void NotEmpty<T>(IEnumerable<T> collection,string message="")
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "The collection should be non-empty.";
            }
            
            if (collection.Count() <= 0) 
            {
                Fail("AssertThat.NotEmpty failed. " + message);
            }
        }

        public static void HasOnlyOneMatch<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
        {
            IEnumerable<T> match = collection.Where(condition);
            int c = match.Count();

            if (c != 1)
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = "The collection should contain only one occurance of the matching element. " +
                        "Actual number of occurances: " + c.ToString();
                }

                Fail("AssertThat.HasOnlyOneMatch failed. " + message);
            }
        }

        public static void HasAtLeastOneMatch<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "The collection should contain at least one occurance of the matching element.";
            }

            IEnumerable<T> match = collection.Where(condition);

            if (match.Count() <= 0)
            {
                Fail("AssertThat.HasAtLeastOneMatch failed. " + message);
            }
        }

        /// <summary>
        /// Asserts that the specified collection has no elements matching the specified predicate
        /// </summary>        
        public static void HasNoMatches<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "The collection should NOT contain any occurances of matching elements.";
            }

            IEnumerable<T> match = collection.Where(condition);

            if (match.Count() > 0)
            {
                Fail("AssertThat.HasNoMatches failed. " + message);
            }
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

        /// <summary>
        /// Asserts that the specified string matches the specified pattern defined as a 
        /// sequence of <see cref="Text"/> objects.
        /// </summary>
        public static void IsMatch(string s, Text[] match, string message="")
        {
            bool res = Text.IsMatch(s, match);

            if (res == false) 
            {
                Trace.WriteLine("Input string: ");
                Trace.WriteLine(s);

                try
                {
                    string baseline = Text.GetMinMatchingText(match).Trim();
                    StringDiff diff = StringDiff.GetDiff(baseline, s.Trim());
                    Debug.WriteLine("Diff:");
                    Debug.WriteLine(diff.ToString());
                    Debug.WriteLine(diff.Visualize());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error when trying to diff");
                    Debug.WriteLine(ex.ToString());
                }

                if (string.IsNullOrEmpty(message)) message = "Input string does not match the expected pattern.";

                Fail("AssertThat.IsMatch failed. " + message);
            }
        }

        static void IsCorrectRecusive(SyntaxNode node,SyntaxNode parent, int c)
        {
            if (c > 100000)
            {
                Assert.Fail("Recursion is too deep in AssertThat.IsCorrectRecusive");
            }

            Assert.IsNotNull(node.Parent, "Parent node should not be null");
            Assert.AreSame(parent, node.Parent);

            foreach (SyntaxNode child in node.EnumerateChildNodes())
            {
                IsCorrectRecusive(child, node, c+1);
            }
        }

        public static void IsSyntaxTreeCorrect(SyntaxNode root)
        {
            foreach (SyntaxNode child in root.EnumerateChildNodes())
            {
                IsCorrectRecusive(child,root, 0);
            }
        }

        public static void IsSyntaxTreeCorrect(IEnumerable<SyntaxNode> nodes)
        {
            foreach (SyntaxNode node in nodes)
            {
                IsSyntaxTreeCorrect(node);
            }
        }

        public static void Throws<T>(Action action)
        {
            try 
            {
                action();
                Assert.Fail("Method did not throw exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(T),ex.GetType());
            }
        }

        public static void DoesNotThrow(Action action)
        {
            try
            {
                action(); 
            }
            catch (Exception ex)
            {
                //this is technically useless, as exception automatically fails test
                //but i use it for more expressiveness in case i'm testing that some
                //property or method should not throw

                Assert.Fail(
                    "The callback expected not to throw, but actually throws " + 
                    ex.ToString());
            }
        }

        /// <summary>
        /// Asserts that two specified strings consist of the same lexical element sequences 
        /// (that is, they are equal after all adjacent whitespace character sequences are replaced with a single whitespace).
        /// </summary>
        public static void AreLexicallyEqual(string expected, string actual)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal))
            {
                return;
            }

            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected, actual);
            }

            //normalize strings to replace all whitespace sequences with a single whitespace
            char[] splitter = new char[] { ' ', '\t', '\r', '\n' };
            string[] arr1 = expected.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb1 = new StringBuilder(expected.Length);

            for (int i = 0; i < arr1.Length; i++)
            {
                sb1.Append(arr1[i]);

                if (i < arr1.Length - 1) sb1.Append(' ');
            }

            string[] arr2 = actual.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb2 = new StringBuilder(actual.Length);

            for (int i = 0; i < arr2.Length; i++)
            {
                sb2.Append(arr2[i]);

                if (i < arr2.Length - 1) sb2.Append(' ');
            }

            //compare resulting strings
            string s1 = sb1.ToString();
            string s2 = sb2.ToString();

            if (!string.Equals(s1, s2, StringComparison.Ordinal))
            {
                string diffDescr = string.Empty;

                try
                {
                    StringDiff diff = StringDiff.GetDiff(s1, s2);
                    Debug.WriteLine("AssertThat.AreLexicallyEqual diff:");
                    Debug.WriteLine(diff.Visualize());
                    diffDescr = diff.ToString();

                    string path = Utils.GetRandomFilePath("diff", 5, "html");
                    FileStream fs = File.Open(path, FileMode.CreateNew, FileAccess.Write);
                    StreamWriter wr = new StreamWriter(fs);

                    using (wr)
                    {
                        diff.VisualizeHTML(wr, "AssertThat.AreLexicallyEqual");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in GetDiff:");
                    Debug.WriteLine(ex.ToString());
                }

                string mes = string.Format(
                    "AssertThat.AreLexicallyEqual failed. Expected: \n{0}\nActual: \n{1}\n{2}",
                    expected, actual, diffDescr);

                Fail(mes);
            }
        }

        /// <summary>
        /// Assert that two IL strings are equal, ignoring differences in whitespaces and BCL assembly names
        /// </summary>
        public static void CilEquals(string expected, string actual)
        {
            // Normalize IL to account for variations in BCL assembly names
            string s1 = expected.Replace("[System.Private.CoreLib]", "[mscorlib]");
            s1 = s1.Replace("[netstandard]", "[mscorlib]");
            s1 = s1.Replace("[System.Console]", "[mscorlib]");

            string s2 = actual.Replace("[System.Private.CoreLib]", "[mscorlib]");
            s2 = s2.Replace("[netstandard]", "[mscorlib]");
            s2 = s2.Replace("[System.Console]", "[mscorlib]");

            // Assert on the normalized strings
            AreLexicallyEqual(s1, s2);
        }
    }
}
