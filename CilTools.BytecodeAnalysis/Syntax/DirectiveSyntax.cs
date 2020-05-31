/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    public class DirectiveSyntax:SyntaxNode
    {        
        string _name;
        SyntaxNode[] _content;
        
        public string Name { get { return this._name; } }

        public void WriteContent(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (this._content.Length == 0) return;

            for (int i = 0; i < this._content.Length; i++) this._content[i].ToText(target);
            target.Flush();
        }

        public string Content 
        { 
            get 
            {
                if (this._content.Length == 0) return String.Empty;

                StringBuilder sb = new StringBuilder(200);
                StringWriter wr = new StringWriter(sb);
                this.WriteContent(wr);
                return sb.ToString();
            } 
        }

        public IEnumerable<SyntaxNode> InnerSyntax 
        { 
            get 
            {
                for (int i = 0; i < this._content.Length; i++) yield return this._content[i];
            } 
        }

        public int InnerElementsCount { get { return this._content.Length; } }

        internal DirectiveSyntax(string lead, string name, SyntaxNode[] content)
        {
            if (lead == null) lead = "";
            if (content == null) content = new SyntaxNode[0];            

            this._lead = lead;
            this._name = name;
            this._content = content;            
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write('.');
            target.Write(this._name);

            if (this._content.Length > 0)
            {
                target.Write(' ');
                this.WriteContent(target);
            }
            else
            {
                target.WriteLine();
            }
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            if (this._content.Length > 0)
            {
                yield return new KeywordSyntax(this._lead, "." + this._name, " ");

                for (int i = 0; i < this._content.Length; i++) yield return this._content[i];
            }
            else
            {
                yield return new KeywordSyntax(this._lead, "." + this._name, Environment.NewLine);
            }
        }

        internal static DirectiveSyntax FromMethodSignature(MethodBase m)
        {
            CustomMethod cm = (CustomMethod)m;
            ParameterInfo[] pars = m.GetParameters();
            
            List<SyntaxNode> inner = new List<SyntaxNode>(100);

            if (m.IsPublic) inner.Add(new KeywordSyntax(" ", "public", String.Empty));
            else if (m.IsPrivate) inner.Add(new KeywordSyntax(" ", "private", String.Empty));
            else if (m.IsAssembly) inner.Add(new KeywordSyntax(" ", "assembly", String.Empty)); //internal
            else if (m.IsFamily) inner.Add(new KeywordSyntax(" ", "family", String.Empty)); //protected
            else inner.Add(new KeywordSyntax(" ", "famorassem", String.Empty)); //protected internal

            if (m.IsHideBySig) inner.Add(new KeywordSyntax(" ", "hidebysig", String.Empty));

            if (m.IsAbstract) inner.Add(new KeywordSyntax(" ", "abstract", String.Empty));

            if (m.IsVirtual) inner.Add(new KeywordSyntax(" ", "virtual", String.Empty));

            if (m.IsStatic) inner.Add(new KeywordSyntax(" ", "static", String.Empty));
            else inner.Add(new KeywordSyntax(" ", "instance", String.Empty));

            if (m.CallingConvention == CallingConventions.VarArgs)
            {
                inner.Add(new KeywordSyntax(" ", "vararg", String.Empty));
            }

            string rt = "";
            if (cm.ReturnType != null) rt = " "+CilAnalysis.GetTypeName(cm.ReturnType);
            inner.Add(new TypeRefSyntax(rt));

            inner.Add(new IdentifierSyntax(" ", m.Name, String.Empty));

            if (m.IsGenericMethod)
            {
                inner.Add(new PunctuationSyntax(String.Empty,"<",String.Empty));

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) inner.Add(new PunctuationSyntax(String.Empty, ","," "));

                    if (args[i].IsGenericParameter) inner.Add(new GenericSyntax(args[i].Name));
                    else inner.Add(new GenericSyntax(CilAnalysis.GetTypeName(args[i])));
                }

                inner.Add(new PunctuationSyntax(String.Empty, ">", String.Empty));
            }

            inner.Add(new PunctuationSyntax(String.Empty, "(", String.Empty));

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) inner.Add(new PunctuationSyntax(String.Empty, ",", " "+ Environment.NewLine));
                else inner.Add(new GenericSyntax(Environment.NewLine));

                inner.Add(new GenericSyntax("    "));

                if (pars[i].IsOptional) inner.Add(new GenericSyntax("[opt] "));

                string partype = CilAnalysis.GetTypeName(pars[i].ParameterType);

                string parname;
                if (pars[i].Name != null) parname = pars[i].Name;
                else parname = "par" + (i + 1).ToString();

                inner.Add(new TypeRefSyntax(partype));
                inner.Add(new IdentifierSyntax(" ", parname, String.Empty));
            }

            if (pars.Length > 0) inner.Add(new GenericSyntax(Environment.NewLine));
            inner.Add(new PunctuationSyntax(String.Empty, ")", String.Empty));
            inner.Add(new KeywordSyntax(" ", "cil", String.Empty));
            inner.Add(new KeywordSyntax(" ", "managed", Environment.NewLine));
            
            return new DirectiveSyntax("", "method", inner.ToArray());
        }
    }
}
