/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Metadata;
using CilTools.Syntax;
using CilTools.Visualization;
using CilView.Common;
using CilView.Core.Syntax;

namespace CilTools.CommandLine
{
    static class Visualizer
    {
        public static void PrintNode(SyntaxNode node, bool noColor, TextWriter target)
        {
            //recursively prints CIL syntax tree to console

            SyntaxNode[] children = node.GetChildNodes();

            if (children.Length == 0)
            {
                //if it a leaf node, print its content to console

                if (noColor)
                {
                    //no syntax highlighting
                    node.ToText(target);
                    return;
                }

                //hightlight syntax elements
                ConsoleColor originalColor = Console.ForegroundColor;

                if (node is KeywordSyntax)
                {
                    KeywordSyntax ks = (KeywordSyntax)node;

                    if (ks.Kind == KeywordKind.Other) Console.ForegroundColor = ConsoleColor.Cyan;
                    else if (ks.Kind == KeywordKind.DirectiveName) Console.ForegroundColor = ConsoleColor.Magenta;
                }
                else if (node is IdentifierSyntax)
                {
                    IdentifierSyntax id = (IdentifierSyntax)node;

                    if (id.IsMemberName) Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (node is LiteralSyntax)
                {
                    LiteralSyntax lit = (LiteralSyntax)node;
                    if (lit.Value is string) Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (node is CommentSyntax)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                node.ToText(target);

                //restore console color to default value
                Console.ForegroundColor = originalColor;
            }
            else
            {
                //if the node has child nodes, process them

                for (int i = 0; i < children.Length; i++)
                {
                    PrintNode(children[i], noColor, target);
                }
            }
        }

        static void PrintMethod(MethodBase method, bool noColor, TextWriter target)
        {
            CilGraph graph = CilGraph.Create(method);
            SyntaxNode root = graph.ToSyntaxTree();
            PrintNode(root, noColor, target);
        }

        static void PrintType(Type t, bool full, bool noColor, TextWriter target)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, full, new DisassemblerParams());

            foreach (SyntaxNode node in nodes)
            {
                PrintNode(node, noColor, target);
            }
        }

        public static int VisualizeMethod(string asspath, string type, string method, bool noColor, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);
                Type t = ass.GetType(type);

                if (t == null)
                {
                    Console.WriteLine("Error: Type {0} not found in assembly {1}", type, asspath);
                    return 1;
                }

                MemberInfo[] methods = t.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );

                MethodBase[] selectedMethods = methods.OfType<MethodBase>().Where((x) => { return x.Name == method; }).ToArray();

                if (selectedMethods.Length == 0)
                {
                    Console.WriteLine("Error: Type {0} does not declare methods with the specified name", type);
                    return 1;
                }

                for (int i = 0; i < selectedMethods.Length; i++)
                {
                    PrintMethod(selectedMethods[i], noColor, target);
                    target.WriteLine();
                }

                retCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                retCode = 1;
            }
            finally
            {
                reader.Dispose();
            }

            return retCode;
        }

        public static int VisualizeType(string asspath, string type, bool full, bool noColor, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);
                Type t = ass.GetType(type);

                if (t == null)
                {
                    Console.WriteLine("Error: Type {0} not found in assembly {1}", type, asspath);
                    return 1;
                }

                PrintType(t, full, noColor, target);
                retCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                retCode = 1;
            }
            finally
            {
                reader.Dispose();
            }

            return retCode;
        }
        
        public static int VisualizeAssembly(string asspath, bool noColor, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);                
                IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);

                foreach (SyntaxNode node in nodes)
                {
                    PrintNode(node, noColor, target);
                }
                
                retCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                retCode = 1;
            }
            finally
            {
                reader.Dispose();
            }

            return retCode;
        }
        
        public static int DisassembleMethod(string asspath, string type, string method, bool html, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);
                Type t = ass.GetType(type);

                if (t == null)
                {
                    Console.WriteLine("Error: Type {0} not found in assembly {1}", type, asspath);
                    return 1;
                }

                MemberInfo[] methods = Utils.GetAllMembers(t);
                Func<MethodBase, bool> predicate = (x) => Utils.StringEquals(x.Name, method);
                MethodBase[] selectedMethods = methods.OfType<MethodBase>().Where(predicate).ToArray();

                if (selectedMethods.Length == 0)
                {
                    Console.WriteLine("Error: Type {0} does not declare methods with the specified name", type);
                    return 1;
                }

                OutputFormat fmt;
                DisassemblerParams dpars = new DisassemblerParams();

                if (html)
                {
                    SyntaxWriter.WriteDocumentStart(target);
                    fmt = OutputFormat.Html;
                }
                else
                {
                    SyntaxWriter.WriteHeader(target);
                    fmt = OutputFormat.Plaintext;
                }
                
                for (int i = 0; i < selectedMethods.Length; i++)
                {
                    SyntaxWriter.DisassembleMethod(selectedMethods[i], dpars, fmt, target);

                    target.WriteLine();
                }

                if (html) SyntaxWriter.WriteDocumentEnd(target);

                retCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                retCode = 1;
            }
            finally
            {
                reader.Dispose();
            }

            return retCode;
        }
    }
}
