/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the directive in CIL assembler. The directive declaration starts from the dotted name.
    /// </summary>
    /// <remarks>
    /// The directive provides meta-information, such as the method's signature or the declarations of local variables.
    /// </remarks>
    public class DirectiveSyntax:SyntaxNode
    {
        string _name;
        KeywordSyntax _namesyntax;
        SyntaxNode[] _content;
        
        /// <summary>
        /// Gets the name of this directive
        /// </summary>
        public string Name { get { return this._name; } }

        /// <summary>
        /// Writes text representation of this directive's content into the specified TextWriter
        /// </summary>
        public void WriteContent(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (this._content.Length == 0) return;

            for (int i = 0; i < this._content.Length; i++) this._content[i].ToText(target);
            target.Flush();
        }

        /// <summary>
        /// Gets the text representation of this directive's content
        /// </summary>
        public string ContentString 
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

        /// <summary>
        /// Gets the collection of nodes that represent this directive's content
        /// </summary>
        public IEnumerable<SyntaxNode> ContentSyntax 
        { 
            get 
            {
                for (int i = 0; i < this._content.Length; i++) yield return this._content[i];
            } 
        }

        internal DirectiveSyntax(string lead, string name, SyntaxNode[] content)
        {
            if (lead == null) lead = "";
            if (content == null) content = new SyntaxNode[0];

            if (content.Length > 0)
            {
                this._namesyntax = new KeywordSyntax(lead, "." + name, " ",KeywordKind.DirectiveName);
            }
            else
            {
                this._namesyntax = new KeywordSyntax(lead, "." + name, Environment.NewLine,KeywordKind.DirectiveName);
            }

            this._name = name;
            this._namesyntax._parent = this;
            this._content = content;

            for (int i = 0; i < this._content.Length; i++) this._content[i]._parent = this;
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            this._namesyntax.ToText(target);
            
            if (this._content.Length > 0)
            {
                target.Write(' ');
                this.WriteContent(target);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            yield return this._namesyntax;

            if (this._content.Length > 0)
            {
                for (int i = 0; i < this._content.Length; i++) yield return this._content[i];
            }
        }

        internal static DirectiveSyntax FromMethodSignature(MethodBase m)
        {
            CustomMethod cm = (CustomMethod)m;
            ParameterInfo[] pars = m.GetParameters();
            
            List<SyntaxNode> inner = new List<SyntaxNode>(100);

            if (m.IsPublic) inner.Add(new KeywordSyntax(" ", "public", String.Empty, KeywordKind.Other));
            else if (m.IsPrivate) inner.Add(new KeywordSyntax(" ", "private", String.Empty, KeywordKind.Other));
            else if (m.IsAssembly) inner.Add(new KeywordSyntax(" ", "assembly", String.Empty, KeywordKind.Other)); //internal
            else if (m.IsFamily) inner.Add(new KeywordSyntax(" ", "family", String.Empty, KeywordKind.Other)); //protected
            else inner.Add(new KeywordSyntax(" ", "famorassem", String.Empty, KeywordKind.Other)); //protected internal

            if (m.IsHideBySig) inner.Add(new KeywordSyntax(" ", "hidebysig", String.Empty, KeywordKind.Other));

            if (m.IsAbstract) inner.Add(new KeywordSyntax(" ", "abstract", String.Empty, KeywordKind.Other));

            if (m.IsVirtual) inner.Add(new KeywordSyntax(" ", "virtual", String.Empty, KeywordKind.Other));

            if (m.IsStatic) inner.Add(new KeywordSyntax(" ", "static", " ", KeywordKind.Other));
            else inner.Add(new KeywordSyntax(" ", "instance", " ", KeywordKind.Other));

            if (m.CallingConvention == CallingConventions.VarArgs)
            {
                inner.Add(new KeywordSyntax(String.Empty, "vararg", " ", KeywordKind.Other));
            }
                        
            if (cm.ReturnType != null)
            {
                inner.Add(new MemberRefSyntax(CilAnalysis.GetTypeNameSyntax(cm.ReturnType).ToArray(), cm.ReturnType));
            }
            
            inner.Add(new IdentifierSyntax(" ", m.Name, String.Empty, true));

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

                SyntaxNode[] partype = CilAnalysis.GetTypeNameSyntax(pars[i].ParameterType).ToArray();

                string parname;
                if (pars[i].Name != null) parname = pars[i].Name;
                else parname = "par" + (i + 1).ToString();

                inner.Add(new MemberRefSyntax(partype, pars[i].ParameterType));
                inner.Add(new IdentifierSyntax(" ", parname, String.Empty,false));
            }

            if (pars.Length > 0) inner.Add(new GenericSyntax(Environment.NewLine));
            inner.Add(new PunctuationSyntax(String.Empty, ")", String.Empty));
            inner.Add(new KeywordSyntax(" ", "cil", String.Empty, KeywordKind.Other));
            inner.Add(new KeywordSyntax(" ", "managed", Environment.NewLine, KeywordKind.Other));
            
            return new DirectiveSyntax("", "method", inner.ToArray());
        }
    }
}
