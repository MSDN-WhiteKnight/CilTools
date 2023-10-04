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
using CilTools.Reflection;
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

        public static void AllMatch<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "Some items in collection does not match the condition";
            }

            foreach (T item in collection)
            {
                Assert.IsTrue(condition(item), message);
            }
        }

        public static void HasOnlyOneMatch(SyntaxNode root, Func<SyntaxNode, bool> condition, string message = "")
        {
            int c_matches = 0;

            Utils.VisitSyntaxTree(root, (node) =>
            {
                if (condition(node)) c_matches++;
            });

            Assert.AreEqual(1, c_matches, message);
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

        /// <summary>
        /// Asserts that tested string contains the specified pattern defined by start string, sequence of 
        /// <see cref="Text"/> objects and end string. This is useful when the tested string is large, use 
        /// <see cref="IsMatch(string, Text[], string)"/> for smaller strings.
        /// </summary>
        public static void ContainsMatch(string testedString, string start, Text[] middle, string end)
        {
            bool res = Text.ContainsMatch(testedString, start, middle, end);
            const string mes = "Input string does not contain the expected pattern.";
            Assert.IsTrue(res, mes);
        }

        static void IsCorrectRecursive(SyntaxNode node, SyntaxNode parent, int c)
        {
            if (c > 100000)
            {
                Assert.Fail("Recursion is too deep in AssertThat.IsCorrectRecursive");
            }

            Assert.IsNotNull(node.Parent, "Parent node should not be null");
            Assert.AreSame(parent, node.Parent);
            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);
            int n_children = 0;

            foreach (SyntaxNode child in node.EnumerateChildNodes())
            {
                child.ToText(wr);
                n_children++;

                //validate child node
                IsCorrectRecursive(child, node, c + 1);
            }

            if (n_children > 0)
            {
                //if it's not a token, verify that node's text value is a concatenation of child nodes' text values
                string str = sb.ToString();
                AssertThat.AreEqual(node.ToString(), str);
            }

            //verify that leading and trailing whitespace actually consist of whitespace only (or empty)
            AssertThat.AllMatch(node.LeadingWhitespace.ToCharArray(), (x) => char.IsWhiteSpace(x));
            AssertThat.AllMatch(node.TrailingWhitespace.ToCharArray(), (x) => char.IsWhiteSpace(x));
        }

        public static void IsSyntaxTreeCorrect(SyntaxNode root)
        {
            foreach (SyntaxNode child in root.EnumerateChildNodes())
            {
                IsCorrectRecursive(child,root, 0);
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

        static string CreateDiff(string expected, string actual, string funcName)
        {
            StringDiff diff = StringDiff.GetDiff(expected, actual);
            Debug.WriteLine(funcName + " diff:");
            Debug.WriteLine(diff.Visualize());
            string diffDescr = diff.ToString();

            string path = Utils.GetRandomFilePath("diff", 5, "html");
            FileStream fs = File.Open(path, FileMode.CreateNew, FileAccess.Write);
            StreamWriter wr = new StreamWriter(fs);

            using (wr)
            {
                diff.VisualizeHTML(wr, funcName);
            }

            return diffDescr;
        }

        /// <summary>
        /// Asserts that two specified strings are equal (using ordinal comparison). If they are not equal, prints diff 
        /// between strings into debug output.
        /// </summary>
        public static void AreEqual(string expected, string actual)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal)) return;

            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected, actual);
            }

            // If strings are not equal, show diff
            string diffDescr = string.Empty;

            try
            {
                diffDescr = CreateDiff(expected, actual, "AssertThat.AreEqual");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in CreateDiff:");
                Debug.WriteLine(ex.ToString());
            }

            string mes = string.Format(
                "AssertThat.AreEqual failed. Expected: \n{0}\nActual: \n{1}\n{2}",
                expected, actual, diffDescr);

            Fail(mes);
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
            string s1 = Utils.NormalizeWhitespace(expected);
            string s2 = Utils.NormalizeWhitespace(actual);

            //compare resulting strings
            if (!string.Equals(s1, s2, StringComparison.Ordinal))
            {
                string diffDescr = string.Empty;

                try
                {
                    diffDescr = CreateDiff(s1, s2, "AssertThat.AreLexicallyEqual");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in CreateDiff:");
                    Debug.WriteLine(ex.ToString());
                }

                string mes = string.Format(
                    "AssertThat.AreLexicallyEqual failed. Expected: \n{0}\nActual: \n{1}\n{2}",
                    expected, actual, diffDescr);

                Fail(mes);
            }
        }

        static string NormalizeBclNames(string input)
        {
            // Normalize IL to account for variations in BCL assembly names
            string s1 = input.Replace("[System.Private.CoreLib]", "[mscorlib]");
            s1 = s1.Replace("[netstandard]", "[mscorlib]");
            s1 = s1.Replace("[System.Runtime]", "[mscorlib]");
            s1 = s1.Replace("[System.Console]", "[mscorlib]");
            s1 = s1.Replace("[System.Collections]", "[mscorlib]");
            return s1;
        }

        /// <summary>
        /// Assert that two IL strings are equal, ignoring differences in whitespaces and BCL assembly names
        /// </summary>
        public static void CilEquals(string expected, string actual)
        {
            // Normalize IL to account for variations in BCL assembly names
            string s1 = NormalizeBclNames(expected);
            string s2 = NormalizeBclNames(actual);

            // Assert on the normalized strings
            AreLexicallyEqual(s1, s2);
        }

        /// <summary>
        /// Assert that tested string contains the specified CIL string, ignoring differences in whitespaces and 
        /// BCL assembly names
        /// </summary>
        public static void CilContains(string testedString, string stringToFind)
        {
            // Normalize IL to account for variations in BCL assembly names
            string s1 = NormalizeBclNames(stringToFind);
            string s2 = NormalizeBclNames(testedString);

            // Normalize strings to replace all whitespace sequences with a single whitespace
            s1 = Utils.NormalizeWhitespace(s1);
            s2 = Utils.NormalizeWhitespace(s2);

            // Assert on the normalized strings
            const string mes = "AssertThat.CilContains failed. Tested string does not contain the specified string.";
            Assert.IsTrue(s2.Contains(s1), mes);
        }

        /// <summary>
        /// Assert that specified custom attribute's type full name matches the specified string. Only works with custom 
        /// attribute objects implementing <see cref="ICustomAttribute"/>.
        /// </summary>
        public static void CustomAtrributeIsOfType(object attr, string expectedType)
        {
            Assert.AreEqual(expectedType, ((ICustomAttribute)attr).Constructor.DeclaringType.FullName);
        }

        /// <summary>
        /// Asserts that the specified markup strings are equal, disregarding differences in line endings
        /// </summary>
        public static void MarkupEquals(string expected, string actual)
        {
            //normalize line endings
            expected = expected.Replace("\r\n", "\n").Trim();
            actual = actual.Replace("\r\n", "\n").Trim();

            //assert
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Asserts that the specified markup string contains the specified substring, disregarding differences in line endings
        /// </summary>
        public static void MarkupContains(string str, string substr)
        {
            //remove line endings
            str = str.Replace("\r\n", string.Empty);
            str = str.Replace("\n", string.Empty).Trim();
            substr = substr.Replace("\r\n", string.Empty);
            substr = substr.Replace("\n", string.Empty).Trim();

            //assert
            Assert.IsTrue(str.Contains(substr));
        }
    }
}
