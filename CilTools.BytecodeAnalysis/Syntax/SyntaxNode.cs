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
    /// <para>
    /// Use <see cref="CilGraph.ToSyntaxTree(DisassemblerParams)"/> method to get the syntax tree for the specified method.
    /// </para>
    /// </remarks>
    public abstract class SyntaxNode
    {
        internal string _lead=String.Empty;
        internal string _trail = String.Empty;
        internal SyntaxNode _parent;

        internal static readonly SyntaxNode[] EmptySyntax = new SyntaxNode[] { new GenericSyntax(String.Empty) };

        internal static readonly SyntaxNode[] EmptyArray = new SyntaxNode[] { };

        const BindingFlags allMembers =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

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

        internal static SyntaxNode[] GetAttributesSyntax(MemberInfo m, int indent)
        {
            object[] attrs = m.GetCustomAttributes(false);
            List<SyntaxNode> ret = new List<SyntaxNode>(attrs.Length);
            string content;
            StringBuilder sb;
            string strIndent = "".PadLeft(indent,' ');

            for (int i = 0; i < attrs.Length; i++)
            {
                //from metadata
                if (attrs[i] is ICustomAttribute)
                {
                    ICustomAttribute ca = (ICustomAttribute)attrs[i];

                    List<SyntaxNode> children = new List<SyntaxNode>();
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(ca.Constructor,false);
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

                    DirectiveSyntax dir = new DirectiveSyntax(strIndent, "custom", children.ToArray());
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
                    int parcount = constr[0].GetParameters().Length;

                    if (parcount == 0 && t.GetFields(BindingFlags.Public & BindingFlags.Instance).Length == 0 &&
                        t.GetProperties(BindingFlags.Public | BindingFlags.Instance).
                        Where((x) => x.DeclaringType != typeof(Attribute) && x.CanWrite == true).Count() == 0
                        )
                    {
                        //Atribute prolog & zero number of arguments (ECMA-335 II.23.3 Custom attributes)
                        List<SyntaxNode> children = new List<SyntaxNode>();
                        MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(constr[0], false);
                        children.Add(mref);
                        children.Add(new PunctuationSyntax(" ", "=", " "));
                        children.Add(new PunctuationSyntax("", "(", " "));
                        children.Add(new GenericSyntax("01 00 00 00"));
                        children.Add(new PunctuationSyntax(" ", ")", Environment.NewLine));
                        
                        DirectiveSyntax dir = new DirectiveSyntax(strIndent, "custom", children.ToArray());
                        ret.Add(dir);
                    }
                    else
                    {
                        s_attr = CilAnalysis.MethodToString(constr[0]);
                        output.Write(".custom ");
                        output.Write(s_attr);
                        output.Flush();
                        content = sb.ToString();
                        CommentSyntax node = CommentSyntax.Create(strIndent, content, null, false);
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
                    CommentSyntax node = CommentSyntax.Create(strIndent, content, null, false);
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

        internal static string GetIndentString(int c)
        {
            return "".PadLeft(c, ' ');
        }
        
        internal static void GetAttributeSyntax(object attr, int indent, List<SyntaxNode> ret)
        {
            string content;
            StringBuilder sb;
            string strIndent = "".PadLeft(indent,' ');

                //from metadata
                if (attr is ICustomAttribute)
                {
                    ICustomAttribute ca = (ICustomAttribute)attr;

                    List<SyntaxNode> children = new List<SyntaxNode>();
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(ca.Constructor,false);
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

                    DirectiveSyntax dir = new DirectiveSyntax(strIndent, "custom", children.ToArray());
                    ret.Add(dir);
                    return;
                }

                //from reflection
                Type t = attr.GetType();
                ConstructorInfo[] constr = t.GetConstructors();
                string s_attr;
                sb = new StringBuilder(100);
                StringWriter output = new StringWriter(sb);

                if (constr.Length == 1)
                {
                    int parcount = constr[0].GetParameters().Length;

                    if (parcount == 0 && t.GetFields(BindingFlags.Public & BindingFlags.Instance).Length == 0 &&
                        t.GetProperties(BindingFlags.Public | BindingFlags.Instance).
                        Where((x) => x.DeclaringType != typeof(Attribute) && x.CanWrite == true).Count() == 0
                        )
                    {
                        //Atribute prolog & zero number of arguments (ECMA-335 II.23.3 Custom attributes)
                        List<SyntaxNode> children = new List<SyntaxNode>();
                        MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(constr[0], false);
                        children.Add(mref);
                        children.Add(new PunctuationSyntax(" ", "=", " "));
                        children.Add(new PunctuationSyntax("", "(", " "));
                        children.Add(new GenericSyntax("01 00 00 00"));
                        children.Add(new PunctuationSyntax(" ", ")", Environment.NewLine));
                        
                        DirectiveSyntax dir = new DirectiveSyntax(strIndent, "custom", children.ToArray());
                        ret.Add(dir);
                    }
                    else
                    {
                        s_attr = CilAnalysis.MethodToString(constr[0]);
                        output.Write(".custom ");
                        output.Write(s_attr);
                        output.Flush();
                        content = sb.ToString();
                        CommentSyntax node = CommentSyntax.Create(strIndent, content, null, false);
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
                    CommentSyntax node = CommentSyntax.Create(strIndent, content, null, false);
                    ret.Add(node);
                }
        }

        internal static SyntaxNode[] GetDefaultsSyntax(MethodBase m, int startIndent)
        {
            ParameterInfo[] pars = m.GetParameters();
            List<SyntaxNode> ret = new List<SyntaxNode>(pars.Length);
            
            // Return type custom attributes
            ICustomAttributeProvider provider = null;
            
            if(m is MethodInfo)
            {
                try{ provider = ((MethodInfo)m).ReturnTypeCustomAttributes;}
                catch(NotImplementedException)
                {
                    ret.Add(CommentSyntax.Create(GetIndentString(startIndent+1), "ERROR: MethodInfo.ReturnTypeCustomAttributes is not implemented", null, false));
                }
            }
            
            if(provider!=null)
            {
                object[] attrs = provider.GetCustomAttributes(false);
                
                if(attrs.Length > 0)
                {
                    DirectiveSyntax dir = new DirectiveSyntax(
                        GetIndentString(startIndent+1), "param", new SyntaxNode[] { new GenericSyntax("[0]") }
                        );
                    ret.Add(dir);
                    
                    for(int i=0;i<attrs.Length;i++)
                    {
                        GetAttributeSyntax(attrs[i], startIndent+1, ret);
                        ret.Add(new GenericSyntax(Environment.NewLine));
                    }
                }
            }

            // Parameters
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
                        GetIndentString(startIndent+1), "param", new SyntaxNode[] { new GenericSyntax(content) }
                        );
                    ret.Add(dir);
                }
            }//end for

            return ret.ToArray();
        }

        static MethodBase GetPropertyMethod(PropertyInfo p, string accName) 
        {
            Type t = p.DeclaringType;
            if (t == null) return null;

            //CilTools.Metadata does not implement GetGetMethod/GetSetMethod currently,
            //so we lookup accessors via well-known name patterns
            string methodName = accName + "_" + p.Name;
            MemberInfo[] members = t.GetMember(methodName, allMembers);
            
            for (int i = 0; i < members.Length; i++) 
            {
                if (members[i] is MethodBase) return (MethodBase)members[i];
            }

            return null;
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

            return GetTypeDefSyntaxImpl(t, false, DisassemblerParams.Default, 0);
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
            
            return GetTypeDefSyntaxImpl(t, full, disassemblerParams, 0);
        }

        internal static IEnumerable<SyntaxNode> GetTypeDefSyntaxImpl(Type t, bool full, 
            DisassemblerParams disassemblerParams, int startIndent)
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
                    if (i >= 1) content.Add(new PunctuationSyntax(string.Empty, ",", " "));
                    
                    SyntaxNode[] gpSyntax = SyntaxGenerator.GetGenericParameterSyntax(targs[i]);

                    for (int j = 0; j < gpSyntax.Length; j++)
                    {
                        content.Add(gpSyntax[j]);
                    }
                }

                content.Add(new PunctuationSyntax(String.Empty, ">", String.Empty));
            }

            content.Add(new GenericSyntax(Environment.NewLine));

            //base type
            if (!t.IsInterface && t.BaseType!=null)
            {
                content.Add(new KeywordSyntax(GetIndentString(startIndent), "extends", " ", KeywordKind.Other));

                try
                {
                    content.Add(new MemberRefSyntax(CilAnalysis.GetTypeFullNameSyntax(t.BaseType).ToArray(), t.BaseType));
                }
                catch (TypeLoadException ex) 
                {
                    //handle error when base type is not available
                    content.Add(new IdentifierSyntax(String.Empty, "UnknownType", String.Empty, false, null));

                    Diagnostics.OnError(
                        t, new CilErrorEventArgs(ex, "Failed to read base type for: "+ t.Name)
                        );
                }
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
                content.Add(new KeywordSyntax(GetIndentString(startIndent), "implements", " ", KeywordKind.Other));

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

            DirectiveSyntax header = new DirectiveSyntax(GetIndentString(startIndent), "class", content.ToArray());
            yield return header;
            
            //body
            content.Clear();

            //custom attributes
            try
            {
                SyntaxNode[] arr = SyntaxNode.GetAttributesSyntax(t, startIndent + 1);

                for (int i = 0; i < arr.Length; i++)
                {
                    content.Add(arr[i]);
                }
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    content.Add(CommentSyntax.Create(GetIndentString(startIndent + 1),
                        "NOTE: Custom attributes are not shown.", null, false));
                }
                else throw;
            }

            content.Add(new GenericSyntax(Environment.NewLine));

            //fields
            FieldInfo[] fields = t.GetFields(allMembers);

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

                DirectiveSyntax field = new DirectiveSyntax(GetIndentString(startIndent + 1), "field", inner.ToArray());
                content.Add(field);
            }

            if(fields.Length>0) content.Add(new GenericSyntax(Environment.NewLine));

            //properties
            PropertyInfo[] props;

            try
            {
                props = t.GetProperties(allMembers);
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    props = new PropertyInfo[0];
                    content.Add(CommentSyntax.Create(GetIndentString(startIndent + 1),
                        "NOTE: Properties are not shown." + Environment.NewLine, null, false));
                }
                else throw;
            }

            for (int i = 0; i < props.Length; i++)
            {
                //ECMA-335 II.17 - Defining properties
                List<SyntaxNode> inner = new List<SyntaxNode>(10);
                MethodBase getter = null;
                MethodBase setter = null;

                if (props[i].IsSpecialName)
                {
                    inner.Add(new KeywordSyntax(string.Empty, "specialname", " ", KeywordKind.Other));
                }
                
                bool isStatic = false;

                if (props[i].CanRead) getter = GetPropertyMethod(props[i], "get");
                if (props[i].CanWrite) setter = GetPropertyMethod(props[i], "set");
                
                if (getter != null && getter.IsStatic) isStatic = true;

                if (isStatic)
                {
                    inner.Add(new KeywordSyntax(string.Empty, "static", " ", KeywordKind.Other));
                }
                else 
                {
                    inner.Add(new KeywordSyntax(string.Empty, "instance", " ", KeywordKind.Other));
                }

                inner.Add(new MemberRefSyntax(
                   CilAnalysis.GetTypeNameSyntax(props[i].PropertyType).ToArray(), props[i].PropertyType
                   ));

                inner.Add(new IdentifierSyntax(" ", props[i].Name, string.Empty, true, props[i]));

                //index parameters
                ParameterInfo[] pars = props[i].GetIndexParameters();
                if (pars == null) pars = new ParameterInfo[0];
                
                inner.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

                for (int j = 0; j < pars.Length; j++)
                {
                    if (j >= 1) inner.Add(new PunctuationSyntax(string.Empty, ",", " "));
                    
                    SyntaxNode[] partype = CilAnalysis.GetTypeNameSyntax(pars[j].ParameterType).ToArray();
                    inner.Add(new MemberRefSyntax(partype, pars[j].ParameterType));
                }
                
                inner.Add(new PunctuationSyntax(string.Empty, ")", Environment.NewLine));
                inner.Add(new PunctuationSyntax(GetIndentString(startIndent + 1), "{", Environment.NewLine));

                //property custom attributes
                try
                {
                    SyntaxNode[] arr = GetAttributesSyntax(props[i], startIndent + 2);

                    for (int j = 0; j < arr.Length; j++)
                    {
                        inner.Add(arr[j]);
                    }
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        inner.Add(CommentSyntax.Create(GetIndentString(startIndent + 2),
                            "NOTE: Custom attributes are not shown.", null, false));
                    }
                    else throw;
                }
                
                //property methods
                if (getter != null) 
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(getter, false);

                    DirectiveSyntax dirGet = new DirectiveSyntax(GetIndentString(startIndent + 2), 
                        "get",new SyntaxNode[] { mref });

                    inner.Add(dirGet);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                if (setter != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(setter, false);

                    DirectiveSyntax dirSet = new DirectiveSyntax(GetIndentString(startIndent + 2), 
                        "set", new SyntaxNode[] { mref });

                    inner.Add(dirSet);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                inner.Add(new PunctuationSyntax(GetIndentString(startIndent + 1), "}", Environment.NewLine + Environment.NewLine));
                DirectiveSyntax dirProp = new DirectiveSyntax(GetIndentString(startIndent + 1), "property", inner.ToArray());
                content.Add(dirProp);
            }

            if (full)
            {
                //constructors
                ConstructorInfo[] constructors;

                try
                {
                    constructors = t.GetConstructors(allMembers);
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        content.Add(CommentSyntax.Create(GetIndentString(startIndent + 1),
                            "NOTE: Constructors are not shown.", null, false));
                        constructors = new ConstructorInfo[0];
                    }
                    else throw;
                }

                for (int i = 0; i < constructors.Length; i++)
                {
                    CilGraph gr = CilGraph.Create(constructors[i]);
                    MethodDefSyntax mds = gr.ToSyntaxTreeImpl(disassemblerParams, startIndent + 1);
                    content.Add(mds);
                    content.Add(new GenericSyntax(Environment.NewLine));
                }

                //methods
                MethodInfo[] methods;

                try
                {
                    methods = t.GetMethods(allMembers);
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        content.Add(CommentSyntax.Create(GetIndentString(startIndent + 1),
                            "NOTE: Methods are not shown.", null, false));
                        methods = new MethodInfo[0];
                    }
                    else throw;
                }

                for (int i = 0; i < methods.Length; i++)
                {
                    CilGraph gr = CilGraph.Create(methods[i]);
                    MethodDefSyntax mds = gr.ToSyntaxTreeImpl(disassemblerParams, startIndent + 1);
                    content.Add(mds);
                    content.Add(new GenericSyntax(Environment.NewLine));
                }

                //nested types
                Type[] types = new Type[0];

                if (startIndent > 20)
                {
                    content.Add(CommentSyntax.Create(string.Empty,
                        "ERROR: Indentation is too deep to show nested types!", null, false));
                }
                else
                {
                    try
                    {
                        types = t.GetNestedTypes(allMembers);
                    }
                    catch (Exception ex)
                    {
                        if (ReflectionUtils.IsExpectedException(ex))
                        {
                            content.Add(CommentSyntax.Create(GetIndentString(startIndent + 1),
                                "NOTE: Nested types are not shown.", null, false));
                        }
                        else throw;
                    }
                }

                for (int i = 0; i < types.Length; i++)
                {
                    IEnumerable<SyntaxNode> typeNodes = GetTypeDefSyntaxImpl(types[i], 
                        true, disassemblerParams, startIndent + 1);

                    foreach (SyntaxNode node in typeNodes)
                    {
                        content.Add(node);
                    }

                    content.Add(new GenericSyntax(Environment.NewLine));
                }
            }
            else
            {
                //add comment to indicate that not all members are listed here
                content.Add(CommentSyntax.Create(GetIndentString(startIndent + 1), "...", null, false));
                content.Add(new GenericSyntax(Environment.NewLine));
            }

            BlockSyntax body = new BlockSyntax(GetIndentString(startIndent), SyntaxNode.EmptyArray, content.ToArray());

            for (int i = 0; i < body._children.Count; i++) body._children[i]._parent = body;

            yield return body;
        }
    }
}
