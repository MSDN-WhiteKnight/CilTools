/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents node in the syntax tree of Common Intermediate Language (CIL) assembler code. Classes that represent concrete language constructs derive from this class. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>SyntaxNode</c> class instance contains all information required to produce the source code of the corresponding language construct, including whitespaces. 
    /// The <see cref="EnumerateChildNodes"/> method returns all child nodes of this node, or an empty collection if it is a leaf (terminal node). 
    /// Some child nodes may be exposed via specific properties as well. 
    /// The text representation for non-terminal node is a string concetanation of all its child nodes' text representations. 
    /// </para>
    /// <para>Use <see cref="CilTools.BytecodeAnalysis.CilGraph.ToSyntaxTree"/> method to get the syntax tree for the specified method.</para>
    /// </remarks>
    public abstract class SyntaxNode
    {
        internal string _lead=String.Empty;
        internal string _trail = String.Empty;
        internal SyntaxNode _parent;

        internal static readonly SyntaxNode[] EmptySyntax = new SyntaxNode[] { new GenericSyntax(String.Empty) };

        internal static readonly SyntaxNode[] EmptyArray = new SyntaxNode[] { };

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
        /// <remarks>
        /// Besides the whitespace character itself, the returned string may contain line feed or carriage return characters. For efficiency purposes, the whitespace 
        /// content, both syntactically meaningful and indentation-only, is stored withing one of the adjasent nodes, not in the separate node.
        /// </remarks>
        public string LeadingWhitespace { get { return this._lead; } }

        /// <summary>
        /// Gets whitespace content at the end of this node's code
        /// </summary>
        /// <remarks>
        /// Besides the whitespace character itself, the returned string may contain line feed or carriage return characters. For efficiency purposes, the whitespace 
        /// content, both syntactically meaningful and indentation-only, is stored withing one of the adjasent nodes, not in the separate node.
        /// </remarks>
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

        internal static SyntaxNode[] GetAttributesSyntax(MethodBase m)
        {
            object[] attrs = m.GetCustomAttributes(false);
            List<SyntaxNode> ret = new List<SyntaxNode>(attrs.Length);
            string content;
            StringBuilder sb;

            for (int i = 0; i < attrs.Length; i++)
            {
                //from metadata
                if (attrs[i] is ICustomAttribute)
                {
                    ICustomAttribute ca = (ICustomAttribute)attrs[i];

                    List<SyntaxNode> children = new List<SyntaxNode>();
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(ca.Constructor);
                    children.Add(mref);
                    children.Add(new PunctuationSyntax(" ", "=", " "));
                    children.Add(new PunctuationSyntax("", "(", " "));
                    sb = new StringBuilder(ca.Data.Length*3);

                    for (int j = 0; j < ca.Data.Length; j++)
                    {
                        sb.Append(ca.Data[j].ToString("X2", CultureInfo.InvariantCulture));
                        sb.Append(' ');
                    }

                    children.Add(new GenericSyntax(sb.ToString()));
                    children.Add(new PunctuationSyntax("", ")", Environment.NewLine));

                    DirectiveSyntax dir = new DirectiveSyntax(" ", "custom", children.ToArray());
                    ret.Add(dir);
                    continue;
                }

                //from reflection
                Type t = attrs[i].GetType();
                ConstructorInfo[] constr = t.GetConstructors();
                string s_attr;
                sb = new StringBuilder(100);
                StringWriter output = new StringWriter(sb);

                if (constr.Length == 1)
                {
                    s_attr = CilAnalysis.MethodToString(constr[0]);
                    int parcount = constr[0].GetParameters().Length;

                    if (parcount == 0 && t.GetFields(BindingFlags.Public & BindingFlags.Instance).Length == 0 &&
                        t.GetProperties(BindingFlags.Public | BindingFlags.Instance).
                        Where((x) => x.DeclaringType != typeof(Attribute) && x.CanWrite == true).Count() == 0
                        )
                    {
                        //Atribute prolog & zero number of arguments (ECMA-335 II.23.3 Custom attributes)
                        List<SyntaxNode> children = new List<SyntaxNode>();
                        MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(constr[0]);
                        children.Add(mref);
                        children.Add(new PunctuationSyntax(" ", "=", " "));
                        children.Add(new PunctuationSyntax("", "(", " "));
                        children.Add(new GenericSyntax("01 00 00 00"));
                        children.Add(new PunctuationSyntax(" ", ")", Environment.NewLine));
                        
                        DirectiveSyntax dir = new DirectiveSyntax(" ", "custom", children.ToArray());
                        ret.Add(dir);
                    }
                    else
                    {
                        output.Write(".custom ");
                        output.Write(s_attr);
                        output.Flush();
                        content = sb.ToString();
                        CommentSyntax node = new CommentSyntax(" ", content);
                        ret.Add(node);
                    }
                }
                else
                {
                    output.Write(".custom ");
                    s_attr = CilAnalysis.GetTypeSpecString(t);
                    output.Write(s_attr);
                    output.Flush();
                    content = sb.ToString();
                    CommentSyntax node = new CommentSyntax(" ", content);
                    ret.Add(node);
                }
            }//end for

            return ret.ToArray();
        }

        internal static SyntaxNode[] GetDefaultsSyntax(MethodBase m)
        {
            ParameterInfo[] pars = m.GetParameters();
            List<SyntaxNode> ret = new List<SyntaxNode>(pars.Length);

            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].IsOptional && pars[i].RawDefaultValue != DBNull.Value)
                {
                    StringBuilder sb = new StringBuilder(100);
                    StringWriter output = new StringWriter(sb);
                    output.Write(' ');
                    output.Write('[');
                    output.Write((i + 1).ToString());
                    output.Write("] = ");

                    if (pars[i].RawDefaultValue != null)
                    {
                        if (pars[i].RawDefaultValue.GetType() == typeof(string))
                        {
                            output.Write('"');
                            output.Write(CilAnalysis.EscapeString(pars[i].RawDefaultValue.ToString()));
                            output.Write('"');
                        }
                        else if (pars[i].RawDefaultValue.GetType() == typeof(char))
                        {
                            output.Write("char");
                            output.Write('(');
                            ushort val = Convert.ToUInt16(pars[i].RawDefaultValue);
                            output.Write("0x");
                            output.Write(val.ToString("X4",CultureInfo.InvariantCulture));
                            output.Write(')');
                        }
                        else //most of the types...
                        {
                            output.Write(CilAnalysis.GetTypeName(pars[i].ParameterType));
                            output.Write('(');
                            output.Write(Convert.ToString(pars[i].RawDefaultValue, System.Globalization.CultureInfo.InvariantCulture));
                            output.Write(')');
                        }
                    }
                    else output.Write("nullref");
                    output.WriteLine();
                    output.Flush();

                    string content = sb.ToString();
                    DirectiveSyntax dir = new DirectiveSyntax(" ", "param", new SyntaxNode[] { new GenericSyntax(content) });
                    ret.Add(dir);
                }
            }//end for

            return ret.ToArray();
        }
    }
}
