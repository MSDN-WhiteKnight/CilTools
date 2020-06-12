/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    public abstract class SyntaxNode
    {
        protected string _lead=String.Empty;
        protected string _trail = String.Empty;

        internal static readonly SyntaxNode[] EmptySyntax = new SyntaxNode[] { new GenericSyntax(String.Empty) };

        internal static readonly SyntaxNode[] EmptyArray = new SyntaxNode[] { };

        public abstract void ToText(TextWriter target);

        public abstract IEnumerable<SyntaxNode> EnumerateChildNodes();

        public string LeadingWhitespace { get { return this._lead; } }

        public string TrailingWhitespace { get { return this._trail; } }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(60);
            StringWriter wr = new StringWriter(sb);
            this.ToText(wr);
            wr.Flush();
            return sb.ToString();
        }

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

            for (int i = 0; i < attrs.Length; i++)
            {
                Type t = attrs[i].GetType();
                ConstructorInfo[] constr = t.GetConstructors();
                string s_attr;
                StringBuilder sb = new StringBuilder(100);
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
                    s_attr = CilAnalysis.GetTypeNameInternal(t);
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
