/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax.Generation;

namespace CilTools.Syntax
{
    /// <summary> 
    /// Provides a base class for nodes in the syntax tree. Classes that represent concrete language constructs 
    /// derive from this class. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This assembly provides syntax node implementations for a Common Intermediate Language (CIL) assembler grammar 
    /// (ECMA-335 specification, part II). They are used by the disassembler to return a parsed representation of the 
    /// produced code.
    /// </para>
    /// <para>
    /// The <c>SyntaxNode</c> class instance contains all information required to produce the source code of the 
    /// corresponding language construct, including whitespaces. The <see cref="EnumerateChildNodes"/> method returns all 
    /// child nodes of this node, or an empty collection if it is a leaf (terminal node). 
    /// Some child nodes may be exposed via specific properties as well. 
    /// The text representation for non-terminal node is a string concetanation of all its child nodes' text representations. 
    /// </para>
    /// <para>
    /// Use <see cref="CilGraph.ToSyntaxTree(DisassemblerParams)"/> method to get the syntax tree for the specified method.
    /// </para>
    /// </remarks>
    public abstract class SyntaxNode
    {
        /// <summary>
        /// Whitespace content at the beginning of this node's code
        /// </summary>
        protected string _lead = string.Empty;

        /// <summary>
        /// Whitespace content at the end of this node's code
        /// </summary>
        protected string _trail = string.Empty;

        /// <summary>
        /// Parent node for this node, or <c>null</c> if this node is root or not included in syntax tree
        /// </summary>
        protected SyntaxNode _parent;

        internal static readonly SyntaxNode[] EmptySyntax = new SyntaxNode[] { new GenericSyntax(String.Empty) };

        /// <summary>
        /// Gets an empty array of syntax nodes
        /// </summary>
        public static readonly SyntaxNode[] EmptyArray = new SyntaxNode[0];
        
        /// <summary>
        /// Writes text representation of this node into the specified TextWriter
        /// </summary>
        public abstract void ToText(TextWriter target);

        /// <summary>
        /// Enumerates child nodes of this node. For the leaf node, returns an empty collection.
        /// </summary>
        /// <returns>The collection of child syntax nodes</returns>
        public abstract IEnumerable<SyntaxNode> EnumerateChildNodes();

        /// <summary>
        /// Gets whitespace content at the beginning of this node's code
        /// </summary>
        /// <remarks>Besides the whitespace character itself, the returned string may contain line feed or carriage 
        /// return characters. For efficiency purposes, the whitespace content, both syntactically meaningful and 
        /// indentation-only, is stored within one of the adjacent nodes, not in the separate node.</remarks>
        public string LeadingWhitespace { get { return this._lead; } }

        /// <summary>
        /// Gets whitespace content at the end of this node's code
        /// </summary>
        /// <remarks>Besides the whitespace character itself, the returned string may contain line feed or carriage 
        /// return characters. For efficiency purposes, the whitespace content, both syntactically meaningful and 
        /// indentation-only, is stored within one of the adjacent nodes, not in the separate node.</remarks>
        public string TrailingWhitespace { get { return this._trail; } }

        /// <summary>
        /// Gets the parent node of this syntax node, or null if this node is root or not included in syntax tree.
        /// </summary>
        public SyntaxNode Parent { get { return this._parent; } }
        
        /// <summary>
        /// Gets the text representation of this node, including whitespace content
        /// </summary>
        /// <returns>The string containing CIL code of this syntax node</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(60);
            StringWriter wr = new StringWriter(sb);
            this.ToText(wr);
            wr.Flush();
            return sb.ToString();
        }

        /// <summary>
        /// Gets the array of this node's child nodes. For the leaf node, returns an empty array.
        /// </summary>
        /// <returns>The array of child syntax nodes</returns>
        public SyntaxNode[] GetChildNodes()
        {
            IEnumerable<SyntaxNode> ienum = this.EnumerateChildNodes();

            if (ienum is SyntaxNode[]) return (SyntaxNode[])ienum;

            List<SyntaxNode> ret = new List<SyntaxNode>(50);

            foreach (SyntaxNode node in ienum) ret.Add(node);

            return ret.ToArray();
        }
        
        /// <summary>
        /// Gets the CIL assembler syntax for the definition of the specified type 
        /// </summary>
        /// <param name="t">Type to get definition syntax</param>
        /// <returns>The collection of syntax nodes that make up type definition syntax</returns>
        /// <exception cref="ArgumentNullException">The specified type is null</exception>
        public static IEnumerable<SyntaxNode> GetTypeDefSyntax(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");

            return SyntaxGenerator.GetTypeDefSyntax(t, false, DisassemblerParams.Default, 0);
        }

        /// <summary>
        /// Gets the CIL assembler syntax for the definition of the specified type with specified disassembler parameters
        /// </summary>
        /// <param name="t">Type to get definition syntax</param>
        /// <param name="full">
        /// <c>true</c> to return full syntax (including method defnitions and nested types), <c>false</c> to return
        /// short syntax
        /// </param>
        /// <param name="disassemblerParams">
        /// Object that specifies additional options for the disassembling operation
        /// </param>
        /// <returns>The collection of syntax nodes that make up type definition syntax</returns>
        /// <exception cref="ArgumentNullException">The specified type is null</exception>
        public static IEnumerable<SyntaxNode> GetTypeDefSyntax(Type t, bool full, DisassemblerParams disassemblerParams)
        {
            if (t == null) throw new ArgumentNullException("t");
            
            return SyntaxGenerator.GetTypeDefSyntax(t, full, disassemblerParams, 0);
        }

        internal void SetParent(SyntaxNode parent)
        {
            this._parent = parent;
        }
    }
}
