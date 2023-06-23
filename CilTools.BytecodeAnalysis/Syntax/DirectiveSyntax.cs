/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax.Generation;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the directive in CIL assembler. The directive declaration starts from the dotted name.
    /// </summary>
    /// <remarks>
    /// The directive provides meta-information, such as the method's signature or the declarations of local variables.
    /// </remarks>
    public class DirectiveSyntax : SyntaxNode
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
            if (content == null) content = SyntaxNode.EmptyArray;

            if (content.Length > 0)
            {
                this._namesyntax = new KeywordSyntax(lead, "." + name, " ", KeywordKind.DirectiveName);
            }
            else
            {
                this._namesyntax = new KeywordSyntax(lead, "." + name, Environment.NewLine, KeywordKind.DirectiveName);
            }

            this._name = name;
            this._namesyntax.SetParent(this);
            this._content = content;

            for (int i = 0; i < this._content.Length; i++) this._content[i].SetParent(this);
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            this._namesyntax.ToText(target);

            if (this._content.Length > 0)
            {
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

        internal static DirectiveSyntax FromMethodSignature(MethodBase m, string lead)
        {
            ICustomMethod cm = (ICustomMethod)m;
            ParameterInfo[] pars = m.GetParameters();

            List<SyntaxNode> inner = new List<SyntaxNode>(100);

            // ECMA-335 II.15.4.2 - Predefined attributes on methods
            if (m.IsPublic)
            {
                inner.Add(new KeywordSyntax("public", string.Empty));
            }
            else if (m.IsPrivate)
            {
                inner.Add(new KeywordSyntax("private", string.Empty));
            }
            else if (m.IsAssembly)
            {
                inner.Add(new KeywordSyntax("assembly", string.Empty)); //internal
            }
            else if (m.IsFamily)
            {
                inner.Add(new KeywordSyntax("family", string.Empty)); //protected
            }
            else
            {
                inner.Add(new KeywordSyntax("famorassem", string.Empty)); //protected internal
            }

            if (m.IsHideBySig) inner.Add(new KeywordSyntax(" ", "hidebysig", string.Empty, KeywordKind.Other));

            if (m.IsAbstract) inner.Add(new KeywordSyntax(" ", "abstract", string.Empty, KeywordKind.Other));

            if ((m.Attributes & MethodAttributes.NewSlot) == MethodAttributes.NewSlot)
            {
                inner.Add(new KeywordSyntax(" ", "newslot", string.Empty, KeywordKind.Other));
            }

            if (m.IsVirtual) inner.Add(new KeywordSyntax(" ", "virtual", string.Empty, KeywordKind.Other));

            if (m.IsFinal) inner.Add(new KeywordSyntax(" ", "final", string.Empty, KeywordKind.Other));

            if (m.IsSpecialName) inner.Add(new KeywordSyntax(" ", "specialname", string.Empty, KeywordKind.Other));

            if ((m.Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName)
            {
                inner.Add(new KeywordSyntax(" ", "rtspecialname", string.Empty, KeywordKind.Other));
            }

            if (m.IsStatic) inner.Add(new KeywordSyntax(" ", "static", " ", KeywordKind.Other));
            else inner.Add(new KeywordSyntax(" ", "instance", " ", KeywordKind.Other));

            if ((m.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                inner.Add(new KeywordSyntax("pinvokeimpl", string.Empty));
                inner.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

                PInvokeParams ppars = cm.GetPInvokeParams();

                if (ppars != null)
                {
                    inner.Add(LiteralSyntax.CreateFromValue(string.Empty, ppars.ModuleName, " "));

                    if (!string.IsNullOrEmpty(ppars.FunctionName) &&
                        !string.Equals(ppars.FunctionName, m.Name, StringComparison.InvariantCulture))
                    {
                        inner.Add(new KeywordSyntax("as", " "));
                        inner.Add(LiteralSyntax.CreateFromValue(string.Empty, ppars.FunctionName, " "));
                    }

                    if (ppars.SetLastError)
                    {
                        inner.Add(new KeywordSyntax("lasterr", " "));
                    }

                    if (ppars.ExactSpelling)
                    {
                        inner.Add(new KeywordSyntax("nomangle", " "));
                    }

                    switch (ppars.CallingConvention)
                    {
                        case System.Runtime.InteropServices.CallingConvention.Cdecl:
                            inner.Add(new KeywordSyntax("cdecl", " "));
                            break;
                        case System.Runtime.InteropServices.CallingConvention.Winapi:
                            inner.Add(new KeywordSyntax("platformapi", " "));
                            break;
                        case System.Runtime.InteropServices.CallingConvention.StdCall:
                            inner.Add(new KeywordSyntax("stdcall", " "));
                            break;
                        case System.Runtime.InteropServices.CallingConvention.FastCall:
                            inner.Add(new KeywordSyntax("fastcall", " "));
                            break;
                        case System.Runtime.InteropServices.CallingConvention.ThisCall:
                            inner.Add(new KeywordSyntax("thiscall", " "));
                            break;
                    }

                    switch (ppars.CharSet)
                    {
                        case System.Runtime.InteropServices.CharSet.Ansi:
                            inner.Add(new KeywordSyntax("ansi", " "));
                            break;
                        case System.Runtime.InteropServices.CharSet.Unicode:
                            inner.Add(new KeywordSyntax("unicode", " "));
                            break;
                    }

                    if (ppars.BestFitMapping.HasValue)
                    {
                        inner.Add(new KeywordSyntax("bestfit", string.Empty));
                        inner.Add(new PunctuationSyntax(string.Empty, ":", string.Empty));

                        if (ppars.BestFitMapping.Value) inner.Add(new GenericSyntax("on"));
                        else inner.Add(new GenericSyntax("off"));
                    }
                }

                inner.Add(new PunctuationSyntax(string.Empty, ")", " "));
            }

            if (m.CallingConvention == CallingConventions.VarArgs)
            {
                inner.Add(new KeywordSyntax("vararg", " "));
            }

            Assembly containingAssembly = ReflectionUtils.GetContainingAssembly(m);
            TypeSyntaxGenerator tgen = new TypeSyntaxGenerator(containingAssembly);

            if (cm.ReturnType != null)
            {
                IEnumerable<SyntaxNode> rtNodes = tgen.GetTypeNameSyntax(cm.ReturnType);
                inner.Add(new MemberRefSyntax(rtNodes.ToArray(), cm.ReturnType));
            }
            else
            {
                //we append return type here even for constructors
                inner.Add(new KeywordSyntax("void", string.Empty));
            }

            inner.Add(new IdentifierSyntax(" ", m.Name, string.Empty, true, m));

            if (m.IsGenericMethod)
            {
                inner.Add(new PunctuationSyntax(string.Empty, "<", string.Empty));

                Type[] args;

                if (!m.IsGenericMethodDefinition)
                {
                    MethodBase mdef = cm.GetDefinition();

                    if (mdef != null) args = mdef.GetGenericArguments();
                    else args = m.GetGenericArguments();
                }
                else
                {
                    args = m.GetGenericArguments();
                }

                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) inner.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    SyntaxGenerator gen = new SyntaxGenerator(containingAssembly);
                    SyntaxNode[] gpSyntax = gen.GetGenericParameterSyntax(args[i]);

                    for (int j = 0; j < gpSyntax.Length; j++)
                    {
                        inner.Add(gpSyntax[j]);
                    }
                }

                inner.Add(new PunctuationSyntax(string.Empty, ">", string.Empty));
            }

            inner.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) inner.Add(new PunctuationSyntax(string.Empty, ",", " " + Environment.NewLine));
                else inner.Add(new GenericSyntax(Environment.NewLine));

                inner.Add(new GenericSyntax(lead + "    "));

                if (pars[i].IsOptional) inner.Add(new GenericSyntax("[opt] "));

                IEnumerable<SyntaxNode> parNodes = tgen.GetTypeNameSyntax(pars[i].ParameterType);

                string parname;
                if (pars[i].Name != null) parname = pars[i].Name;
                else parname = "par" + (i + 1).ToString();

                inner.Add(new MemberRefSyntax(parNodes.ToArray(), pars[i].ParameterType));
                inner.Add(new IdentifierSyntax(" ", parname, string.Empty, false, pars[i]));
            }

            if (pars.Length > 0) inner.Add(new GenericSyntax(Environment.NewLine + lead));
            inner.Add(new PunctuationSyntax(string.Empty, ")", string.Empty));

            //ECMA-335 II.15.4.3 - Implementation attributes of methods
            MethodImplAttributes mia = (MethodImplAttributes)0;

            try { mia = m.GetMethodImplementationFlags(); }
            catch (NotImplementedException) { }

            //Code implementation attributes
            if ((mia & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime)
            {
                inner.Add(new KeywordSyntax(" ", "runtime", string.Empty, KeywordKind.Other));
            }
            else if ((mia & MethodImplAttributes.Native) == MethodImplAttributes.Native)
            {
                inner.Add(new KeywordSyntax(" ", "native", string.Empty, KeywordKind.Other));
            }
            else
            {
                inner.Add(new KeywordSyntax(" ", "cil", string.Empty, KeywordKind.Other));
            }

            //managed or unmanaged
            if ((mia & MethodImplAttributes.Unmanaged) == MethodImplAttributes.Unmanaged)
            {
                inner.Add(new KeywordSyntax(" ", "unmanaged", string.Empty, KeywordKind.Other));
            }
            else
            {
                inner.Add(new KeywordSyntax(" ", "managed", string.Empty, KeywordKind.Other));
            }

            //implementation flags
            if ((mia & MethodImplAttributes.ForwardRef) == MethodImplAttributes.ForwardRef)
            {
                inner.Add(new KeywordSyntax(" ", "forwardref", string.Empty, KeywordKind.Other));
            }

            if ((mia & MethodImplAttributes.InternalCall) == MethodImplAttributes.InternalCall)
            {
                inner.Add(new KeywordSyntax(" ", "internalcall", string.Empty, KeywordKind.Other));
            }

            if ((mia & MethodImplAttributes.NoInlining) == MethodImplAttributes.NoInlining)
            {
                inner.Add(new KeywordSyntax(" ", "noinlining", string.Empty, KeywordKind.Other));
            }

            if ((mia & MethodImplAttributes.NoOptimization) == MethodImplAttributes.NoOptimization)
            {
                inner.Add(new KeywordSyntax(" ", "nooptimization", string.Empty, KeywordKind.Other));
            }

            if ((mia & MethodImplAttributes.Synchronized) == MethodImplAttributes.Synchronized)
            {
                inner.Add(new KeywordSyntax(" ", "synchronized", string.Empty, KeywordKind.Other));
            }

            inner.Add(new GenericSyntax(Environment.NewLine));

            return new DirectiveSyntax(lead, "method", inner.ToArray());
        }
    }
}
