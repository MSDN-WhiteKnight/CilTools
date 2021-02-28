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
        /// content, both syntactically meaningful and indentation-only, is stored within one of the adjacent nodes, not in the separate node.
        /// </remarks>
        public string LeadingWhitespace { get { return this._lead; } }

        /// <summary>
        /// Gets whitespace content at the end of this node's code
        /// </summary>
        /// <remarks>
        /// Besides the whitespace character itself, the returned string may contain line feed or carriage return characters. For efficiency purposes, the whitespace 
        /// content, both syntactically meaningful and indentation-only, is stored within one of the adjacent nodes, not in the separate node.
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

        internal static SyntaxNode[] GetAttributesSyntax(MemberInfo m)
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

        internal static string GetConstantValueString(Type t,object constant)
        {
            StringBuilder sb = new StringBuilder(100);
            StringWriter output = new StringWriter(sb);

            if (constant != null)
            {
                if (constant.GetType() == typeof(string))
                {
                    output.Write('"');
                    output.Write(CilAnalysis.EscapeString(constant.ToString()));
                    output.Write('"');
                }
                else if (constant.GetType() == typeof(char))
                {
                    output.Write("char");
                    output.Write('(');
                    ushort val = Convert.ToUInt16(constant);
                    output.Write("0x");
                    output.Write(val.ToString("X4", CultureInfo.InvariantCulture));
                    output.Write(')');
                }
                else //most of the types...
                {
                    output.Write(CilAnalysis.GetTypeName(t));
                    output.Write('(');
                    output.Write(Convert.ToString(constant, System.Globalization.CultureInfo.InvariantCulture));
                    output.Write(')');
                }
            }
            else output.Write("nullref");
            output.Flush();
            string content = sb.ToString();
            return content;
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

                    string valstr = GetConstantValueString(pars[i].ParameterType, pars[i].RawDefaultValue);
                    output.WriteLine(valstr);
                    output.Flush();

                    string content = sb.ToString();
                    DirectiveSyntax dir = new DirectiveSyntax(
                        " ", "param", new SyntaxNode[] { new GenericSyntax(content) }
                        );
                    ret.Add(dir);
                }
            }//end for

            return ret.ToArray();
        }

        /// <summary>
        /// Gets the CIL assembler syntax for the definition of the specified type 
        /// </summary>
        /// <param name="t">Type to get definition syntax</param>
        /// <returns>The collection of syntax nodes that make up type definition syntax</returns>
        public static IEnumerable<SyntaxNode> GetTypeDefSyntax(Type t)
        {
            List<SyntaxNode> content = new List<SyntaxNode>(10);

            //type standard attributes
            if (t.IsInterface)
            {
                content.Add(new KeywordSyntax(String.Empty, "interface", " ", KeywordKind.Other));
            }

            if (t.IsNested)
            {
                content.Add(new KeywordSyntax(String.Empty, "nested", " ", KeywordKind.Other));

                if (t.IsNestedPublic)
                {
                    content.Add(new KeywordSyntax(String.Empty, "public", " ", KeywordKind.Other));
                }
                else if (t.IsNestedAssembly)
                {
                    content.Add(new KeywordSyntax(String.Empty, "assembly", " ", KeywordKind.Other));
                }
                else if (t.IsNestedFamily)
                {
                    content.Add(new KeywordSyntax(String.Empty, "family", " ", KeywordKind.Other));
                }
                else if (t.IsNestedFamORAssem)
                {
                    content.Add(new KeywordSyntax(String.Empty, "famorassem", " ", KeywordKind.Other));
                }
                else if (t.IsNestedFamANDAssem)
                {
                    content.Add(new KeywordSyntax(String.Empty, "famandassem", " ", KeywordKind.Other));
                }
                else
                {
                    content.Add(new KeywordSyntax(String.Empty, "private", " ", KeywordKind.Other));
                }
            }
            else
            {
                if (t.IsPublic)
                {
                    content.Add(new KeywordSyntax(String.Empty, "public", " ", KeywordKind.Other));
                }
                else
                {
                    content.Add(new KeywordSyntax(String.Empty, "private", " ", KeywordKind.Other));
                }
            }

            if (t.IsAbstract)
            {
                content.Add(new KeywordSyntax(String.Empty, "abstract", " ", KeywordKind.Other));
            }

            if (t.IsAutoLayout)
            {
                content.Add(new KeywordSyntax(String.Empty, "auto", " ", KeywordKind.Other));
            }
            else if (t.IsLayoutSequential)
            {
                content.Add(new KeywordSyntax(String.Empty, "sequential", " ", KeywordKind.Other));
            }
            else if (t.IsExplicitLayout)
            {
                content.Add(new KeywordSyntax(String.Empty, "explicit", " ", KeywordKind.Other));
            }

            if (t.IsAnsiClass)
            {
                content.Add(new KeywordSyntax(String.Empty, "ansi", " ", KeywordKind.Other));
            }
            else if (t.IsUnicodeClass)
            {
                content.Add(new KeywordSyntax(String.Empty, "unicode", " ", KeywordKind.Other));
            }
            else if (t.IsAutoClass)
            {
                content.Add(new KeywordSyntax(String.Empty, "autochar", " ", KeywordKind.Other));
            }

            if (t.IsSealed)
            {
                content.Add(new KeywordSyntax(String.Empty, "sealed", " ", KeywordKind.Other));
            }

            if (t.IsSerializable)
            {
                content.Add(new KeywordSyntax(String.Empty, "serializable", " ", KeywordKind.Other));
            }

            if (t.IsSpecialName)
            {
                content.Add(new KeywordSyntax(String.Empty, "specialname", " ", KeywordKind.Other));
            }

            if ((t.Attributes & TypeAttributes.RTSpecialName) != 0)
            {
                content.Add(new KeywordSyntax(String.Empty, "rtspecialname", " ", KeywordKind.Other));
            }

            if ((t.Attributes & TypeAttributes.BeforeFieldInit) != 0)
            {
                content.Add(new KeywordSyntax(String.Empty, "beforefieldinit", " ", KeywordKind.Other));
            }

            //type name
            string tname = "";
            if (!t.IsNested && !String.IsNullOrEmpty(t.Namespace)) tname += t.Namespace + ".";
            tname += t.Name;
            content.Add(new IdentifierSyntax(String.Empty, tname, String.Empty, true,t));

            //generic parameters
            if (t.IsGenericType)
            {
                content.Add(new PunctuationSyntax(String.Empty, "<", String.Empty));
                Type[] targs = t.GetGenericArguments();

                for (int i = 0; i < targs.Length; i++)
                {
                    if (i >= 1) content.Add(new PunctuationSyntax(String.Empty, ",", " "));

                    if (targs[i].IsGenericParameter)
                    {
                        content.Add(
                            new IdentifierSyntax(String.Empty, targs[i].Name, String.Empty, false, targs[i])
                            );
                    }
                    else content.Add(new GenericSyntax(CilAnalysis.GetTypeName(targs[i])));
                }

                content.Add(new PunctuationSyntax(String.Empty, ">", String.Empty));
            }

            content.Add(new GenericSyntax(Environment.NewLine));

            //base type
            if ((t.IsClass || t.IsValueType) && t.BaseType!=null)
            {
                content.Add(new KeywordSyntax(String.Empty, "extends", " ", KeywordKind.Other));
                content.Add(new MemberRefSyntax(CilAnalysis.GetTypeFullNameSyntax(t.BaseType).ToArray(), t.BaseType));
                content.Add(new GenericSyntax(Environment.NewLine));
            }

            //interfaces
            Type[] interfaces = null;

            try
            {
                interfaces = t.GetInterfaces();
            }
            catch (NotImplementedException ex) 
            {
                Diagnostics.OnError(t, new CilErrorEventArgs(ex, ""));
            }

            if (interfaces != null && interfaces.Length > 0)
            {
                content.Add(new KeywordSyntax(String.Empty, "implements", " ", KeywordKind.Other));

                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (i >= 1)
                    {
                        content.Add(new PunctuationSyntax(String.Empty, ",", Environment.NewLine));
                    }

                    content.Add(
                        new MemberRefSyntax(CilAnalysis.GetTypeNameSyntax(interfaces[i]).ToArray(), interfaces[i])
                        );
                }

                content.Add(new GenericSyntax(Environment.NewLine));
            }

            DirectiveSyntax header = new DirectiveSyntax( String.Empty, "class",content.ToArray());
            yield return header;
            
            //body
            content.Clear();

            //custom attributes
            try
            {
                SyntaxNode[] arr = SyntaxNode.GetAttributesSyntax(t);

                for (int i = 0; i < arr.Length; i++)
                {
                    content.Add(arr[i]);
                }
            }
            catch (InvalidOperationException)
            {
                content.Add(new CommentSyntax(" ", "NOTE: Custom attributes are not shown."));
            }
            catch (NotImplementedException)
            {
                content.Add(new CommentSyntax(" ", "NOTE: Custom attributes are not shown."));
            }

            content.Add(new GenericSyntax(Environment.NewLine));

            //members
            FieldInfo[] fields = t.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );

            for (int i = 0; i < fields.Length; i++)
            {
                List<SyntaxNode> inner = new List<SyntaxNode>(10);

                if (fields[i].IsPublic)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "public", " ", KeywordKind.Other));
                }
                else if (fields[i].IsAssembly)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "assembly", " ", KeywordKind.Other)); //internal
                }
                else if (fields[i].IsFamily)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "family", " ", KeywordKind.Other)); //protected
                }
                else if(fields[i].IsFamilyOrAssembly)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "famorassem", " ", KeywordKind.Other)); //protected internal
                }
                else if (fields[i].IsFamilyAndAssembly)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "famandassem", " ", KeywordKind.Other));
                }
                else 
                {
                    inner.Add(new KeywordSyntax(String.Empty, "private", " ", KeywordKind.Other));
                }

                if (fields[i].IsStatic)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "static", " ", KeywordKind.Other));
                }

                if (fields[i].IsInitOnly)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "initonly", " ", KeywordKind.Other));
                }

                if (fields[i].IsLiteral)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "literal", " ", KeywordKind.Other));
                }

                if (fields[i].IsNotSerialized)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "notserialized", " ", KeywordKind.Other));
                }

                if (fields[i].IsSpecialName)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "specialname", " ", KeywordKind.Other));
                }

                if ((fields[i].Attributes & FieldAttributes.RTSpecialName)!=0)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "rtspecialname", " ", KeywordKind.Other));
                }

                inner.Add( new MemberRefSyntax(
                    CilAnalysis.GetTypeNameSyntax(fields[i].FieldType).ToArray(), fields[i].FieldType
                    ));

                inner.Add(new IdentifierSyntax(" ", fields[i].Name, String.Empty, true, fields[i]));

                object constval = DBNull.Value;

                try { constval = fields[i].GetRawConstantValue(); }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
                catch (InvalidOperationException) { }

                if (constval != DBNull.Value)
                {
                    string valstr = GetConstantValueString(fields[i].FieldType, constval);
                    inner.Add(new PunctuationSyntax(" ", "=", " "));
                    inner.Add(new GenericSyntax(valstr));
                }

                inner.Add(new GenericSyntax(Environment.NewLine));

                DirectiveSyntax field = new DirectiveSyntax(" ", "field", inner.ToArray());
                content.Add(field);
            }

            //add comment to indicate that not all members are listed here
            content.Add(new CommentSyntax(Environment.NewLine+" ", "..."));
            content.Add(new GenericSyntax(Environment.NewLine));

            BlockSyntax body = new BlockSyntax(String.Empty, SyntaxNode.EmptyArray, content.ToArray());

            for (int i = 0; i < body._children.Count; i++) body._children[i]._parent = body;

            yield return body;
        }
    }
}
