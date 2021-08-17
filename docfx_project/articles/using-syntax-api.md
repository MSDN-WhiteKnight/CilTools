# Using CIL Tools syntax API

CIL Tools syntax API enables you to convert a method's bytecode into the syntax tree, the high-level structure representing its syntax in IlAsm that can be programmatically processed. The syntax tree and all of its nodes are convertible to the corresonding textual CIL assembler code. The node of the syntax tree either represent the token that have no child nodes, just the textual content, or the node with child nodes for a more complex construct. For a node with child nodes, the textual content is a combination of all its child nodes content. Syntax API is useful to support things such as syntax highlighting or code navigation.

The syntax API is a part of CilTools.BytecodeAnalysis library, and is located under <xref:CilTools.Syntax> namespace. See [Using CilTools.BytecodeAnalysis](./using-bytecode-analysis.md) for a guidance how to get started using the library.

To get the syntax tree for the specified method, create a <xref:CilTools.BytecodeAnalysis.CilGraph> and call <xref:CilTools.BytecodeAnalysis.CilGraph.ToSyntaxTree> on it. This method returns the <xref:CilTools.Syntax.MethodDefSyntax> object whose child nodes, obtained by `GetChildNodes`, make up the CIL method definition. The syntax nodes are derived from the common base class <xref:CilTools.Syntax.SyntaxNode>. To check specific details about what the node do, check its type and cast it to one of the subclasses in `CilTools.Syntax` namespace. To get the textual content of the node, you can use either the `ToText` method that outputs it into the specified `TextWriter`, or the `ToString` method.

The following example shows how to display the CIL code for a method, highlighting keywords with the different color (it might not work properly on non-Windows platforms):

```csharp
using System;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;

class Program
{
    public static int CalcSumOfDigits(int x)
    {
        //sample method: calculate the sum of decimal digits in integer

        if (x < 0) x = -x;

        int sum = 0; 
        int remainder;
        
        while (true)
        {
            remainder = x % 10;
            x = x / 10;            
            sum += remainder;
            if (x <= 0) break;
        }

        return sum;
    }

    static void VisitNode(SyntaxNode node)
    {
        //recursively prints CIL syntax tree to console

        SyntaxNode[] children = node.GetChildNodes();

        if (children.Length == 0)
        {
            //if it a leaf node, print its content to console

            if (node is KeywordSyntax)
            {
                Console.ForegroundColor = ConsoleColor.Cyan; //hightlight keywords
            }
            
            node.ToText(Console.Out);
            Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
            //if the node has child nodes, process them

            for (int i = 0; i < children.Length; i++)
            {
                VisitNode(children[i]);
            }
        }
    }

    static void Main(string[] args)
    {
        //get CIL syntax tree for the method definition
        CilGraph graph = CilGraph.Create(typeof(Program).GetMethod("CalcSumOfDigits"));
        MethodDefSyntax mds = graph.ToSyntaxTree();

        //print it to the console
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        VisitNode(mds);

        Console.ReadKey();
    }
}

```
