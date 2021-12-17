/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Linq;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;
using CilTools.Metadata;
using CilTools.Syntax;

namespace CilTools.CommandLine
{
    class Program
    {
        static void PrintHelp()
        {
            string exeName = typeof(Program).Assembly.GetName().Name;
            Console.WriteLine(" Commands:");
            Console.WriteLine();
            Console.WriteLine("view - Print CIL code of the method or methods with the specified name");
            Console.WriteLine("[Usage: "+exeName+" view (assembly path) (type full name) (method name)]");
            Console.WriteLine();
            Console.WriteLine("help - Print available commands");
            Console.WriteLine();
        }
        
        static string GetErrorInfo()
        {
            string exeName = typeof(Program).Assembly.GetName().Name;
            return "Use \""+exeName+" help\" to print the list of available commands and their arguments.";
        }
        
    static void PrintNode(SyntaxNode node)
    {
        //recursively prints CIL syntax tree to console

        SyntaxNode[] children = node.GetChildNodes();

        if (children.Length == 0)
        {
            //if it a leaf node, print its content to console
            ConsoleColor originalColor=Console.ForegroundColor;
            
            //hightlight syntax elements

            if (node is KeywordSyntax) 
            {
                KeywordSyntax ks = (KeywordSyntax)node;
                
                if (ks.Kind == KeywordKind.Other) Console.ForegroundColor = ConsoleColor.Blue;
                else if (ks.Kind == KeywordKind.DirectiveName) Console.ForegroundColor = ConsoleColor.Magenta;
            }
            else if (node is IdentifierSyntax)
            {
                IdentifierSyntax id = (IdentifierSyntax)node;
                
                if (id.IsMemberName) Console.ForegroundColor = ConsoleColor.Cyan;
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
            
            node.ToText(Console.Out);
            
            //restore console color to default value
            Console.ForegroundColor = originalColor;
        }
        else
        {
            //if the node has child nodes, process them

            for (int i = 0; i < children.Length; i++)
            {
                PrintNode(children[i]);
            }
        }
    }

        static void PrintMethod(MethodBase method)
        {
            CilGraph graph = CilGraph.Create(method);                
            SyntaxNode root = graph.ToSyntaxTree();
            PrintNode(root);
        }
        
        static bool TryReadExpectedParameter(string[] args, int pos, string expected)
        {
            if(pos >= args.Length) return false;
            
            if(args[pos] == expected) return true;
            else return false;
        }
        
        static string ReadCommandParameter(string[] args, int pos)
        {
            return "";
        }
    
        static void Main(string[] args)
        {
            Console.WriteLine("*** CIL Tools command line ***");
            Console.WriteLine("Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight/CilTools)");
            Console.WriteLine();
            
            string asspath;
            string type;
            string method;
            Assembly ass;
            
                if (args.Length == 0)
                {
                    PrintHelp();
                    return;
                }
                else if(args[0]=="help" || args[0]=="/?" || args[0]=="-?")
                {
                    PrintHelp();
                    return;
                }
                else if(args[0]=="view")
                {
                    if(args.Length<4)
                    {
                        Console.WriteLine("Error: not enough arguments for 'view' command.");
                        Console.WriteLine(GetErrorInfo());
                        return;
                    }
                    
                    asspath = args[1];
                    type = args[2];
                    method = args[3];
                    Console.WriteLine("Assembly: "+asspath);
                    ass = Assembly.LoadFrom(asspath);
                }
                else
                {
                    Console.WriteLine("Error: unknown command "+args[0]);
                    Console.WriteLine(GetErrorInfo());
                    return;
                }

                Console.WriteLine("{0}.{1}", type, method);
                Console.WriteLine();

                Type t = ass.GetType(type);

                MemberInfo[] methods = t.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );

            MethodBase[] selectedMethods = methods.OfType<MethodBase>().Where((x) => { return x.Name == method; }).ToArray();
            
            for(int i=0;i<selectedMethods.Length;i++)
            {
                PrintMethod(selectedMethods[i]);
                Console.WriteLine();
            }
        }
    }
}
