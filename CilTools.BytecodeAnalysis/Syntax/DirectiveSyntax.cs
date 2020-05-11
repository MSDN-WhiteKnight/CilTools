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
    public class DirectiveSyntax:SyntaxElement
    {        
        string _name;
        SyntaxElement[] _content;
        
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

        public IEnumerable<SyntaxElement> InnerSyntax 
        { 
            get 
            {
                for (int i = 0; i < this._content.Length; i++) yield return this._content[i];
            } 
        }

        public int InnerElementsCount { get { return this._content.Length; } }

        internal DirectiveSyntax(string lead, string name, SyntaxElement[] content)
        {
            if (lead == null) lead = "";
            if (content == null) content = new SyntaxElement[0];            

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
            target.Write(' ');
            this.WriteContent(target);
        }

        internal static DirectiveSyntax FromMethodSignature(MethodBase m)
        {
            CustomMethod cm = (CustomMethod)m;
            ParameterInfo[] pars = m.GetParameters();

            StringBuilder sb = new StringBuilder(100);
            StringWriter output = new StringWriter(sb);

            if (m.IsPublic) output.Write("public ");
            else if (m.IsPrivate) output.Write("private ");
            else if (m.IsAssembly) output.Write("assembly "); //internal
            else if (m.IsFamily) output.Write("family "); //protected
            else output.Write("famorassem "); //protected internal

            if (m.IsHideBySig) output.Write("hidebysig ");

            if (m.IsAbstract) output.Write("abstract ");

            if (m.IsVirtual) output.Write("virtual ");

            if (m.IsStatic) output.Write("static ");
            else output.Write("instance ");

            if (m.CallingConvention == CallingConventions.VarArgs)
            {
                output.Write("vararg ");
            }

            string rt = "";
            if (cm.ReturnType != null) rt = CilAnalysis.GetTypeName(cm.ReturnType) + " ";
            output.Write(rt);

            output.Write(m.Name);

            if (m.IsGenericMethod)
            {
                output.Write('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) output.Write(", ");

                    if (args[i].IsGenericParameter) output.Write(args[i].Name);
                    else output.Write(CilAnalysis.GetTypeName(args[i]));
                }

                output.Write('>');
            }

            output.Write('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) output.WriteLine(", ");
                else output.WriteLine();

                output.Write("    ");
                if (pars[i].IsOptional) output.Write("[opt] ");

                output.Write(CilAnalysis.GetTypeName(pars[i].ParameterType));

                string parname;
                if (pars[i].Name != null) parname = pars[i].Name;
                else parname = "par" + (i + 1).ToString();

                output.Write(' ');
                output.Write(parname);

            }

            if (pars.Length > 0) output.WriteLine();
            output.Write(')');
            output.Write(" cil managed");
            output.Flush();

            return new DirectiveSyntax("", "method", new SyntaxElement[]{new GenericSyntax(sb.ToString())});
        }
    }
}
