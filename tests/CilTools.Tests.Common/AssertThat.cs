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

namespace CilTools.Tests.Common
{
    public static class AssertThat
    {
        public static void NotEmpty<T>(IEnumerable<T> collection,string message="")
        {
            Assert.IsTrue(collection.Count() > 0, message);
        }

        public static void HasOnlyOneMatch<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
        {
            IEnumerable<T> match = collection.Where(condition);
            Assert.AreEqual(1, match.Count(), message);
        }

        public static void HasAtLeastOneMatch<T>(IEnumerable<T> collection, Func<T, bool> condition, string message = "")
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

        public static void IsMatch(string s, MatchElement[] match, string message="")
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < match.Length; i++)
            {
                sb.Append(match[i].ToString());
            }

            string pattern = sb.ToString();
            if(String.IsNullOrEmpty(message)) message = "Input string does not match the expected pattern: " + pattern;

            Assert.IsTrue(Regex.IsMatch(s, pattern), message);
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

    public abstract class MatchElement
    {       
        public abstract string GetString();

        public override string ToString()
        {
            return GetString();
        }

        public static MatchElement Any
        {
            get { return AnyCharsElement.Value; }
        }
    }

    public class Literal : MatchElement
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

    public class AnyCharsElement : MatchElement
    {
        static AnyCharsElement val=null;

        protected AnyCharsElement() { }

        public static AnyCharsElement Value
        {
            get
            {
                if (val == null) val = new AnyCharsElement();
                return val;
            }
        }

        public override string GetString()
        {
            return "[\\s\\S]*";
        }
    }
}
