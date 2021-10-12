/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

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
                Logger.LogMessage("Input string: ");
                Logger.LogMessage(s);

                if (String.IsNullOrEmpty(message)) message = "Input string does not match the expected pattern.";

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
    }

    /// <summary>
    /// Represents an element of the strings sequence. The element could be either a literal 
    /// string or a mask allowing multiple string values.
    /// </summary>
    /// <remarks>
    /// This class is used in tests to verify that CIL code produced by disassembler 
    /// matches the expected pattern. It is a high-level wrapper for a regular expressions
    /// mechanism.
    /// </remarks>
    public abstract class Text
    {
        /// <summary>
        /// Converts this element to the equivalent regex representation
        /// </summary>
        public abstract string GetString();

        public override string ToString()
        {
            return GetString();
        }

        /// <summary>
        /// Defines a text element allowing any string values
        /// </summary>
        public static Text Any
        {
            get { return AnyCharsText.Value; }
        }

        /// <summary>
        /// Defines a text element allowing one or more whitespace characters
        /// </summary>
        public static Text Whitespace
        {
            get { return AtLeastOneWhitespaceText.Value; }
        }

        /// <summary>
        /// Checks whether a specified string matches a specified sequence of text elements
        /// </summary>
        public static bool IsMatch(string s, Text[] match)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < match.Length; i++)
            {
                sb.Append(match[i].GetString());
            }

            string pattern = sb.ToString();
            return Regex.IsMatch(s, pattern);
        }

        public static implicit operator Text(string s) => new Literal(s);
    }

    /// <summary>
    /// Represents a text element with the exact string value
    /// </summary>
    public class Literal : Text
    {
        string val;

        public Literal(string v)
        {
            this.val = Regex.Escape(v);
        }

        public override string GetString()
        {
            return this.val;
        }
    }

    /// <summary>
    /// Represents a text element allowing any sequence of characters
    /// </summary>
    public class AnyCharsText : Text
    {
        static AnyCharsText val=null;

        protected AnyCharsText() { }

        /// <summary>
        /// Provides the singleton value for the AnyCharsText class
        /// </summary>
        public static AnyCharsText Value
        {
            get
            {
                if (val == null) val = new AnyCharsText();
                return val;
            }
        }

        public override string GetString()
        {
            return "[\\s\\S]*";
        }
    }

    public class AtLeastOneWhitespaceText : Text 
    {
        static AtLeastOneWhitespaceText val = null;

        AtLeastOneWhitespaceText() { }

        /// <summary>
        /// Provides the singleton value for the AtLeastOneWhitespaceText class
        /// </summary>
        public static AtLeastOneWhitespaceText Value
        {
            get
            {
                if (val == null) val = new AtLeastOneWhitespaceText();
                return val;
            }
        }

        public override string GetString()
        {
            return "\\s+";
        }
    }
}
