/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax.Generation
{
    /// <summary>
    /// Generates sequences of syntax nodes in the context of the specified assembly
    /// </summary>
    internal class SyntaxGenerator
    {
        Assembly containingAssembly;

        internal SyntaxGenerator()
        {
            this.containingAssembly = null;
        }

        internal SyntaxGenerator(Assembly ass)
        {
            this.containingAssembly = ass;
        }

        static PunctuationSyntax NewPunctuation(string content)
        {
            return new PunctuationSyntax(string.Empty, content, string.Empty);
        }

        internal SyntaxNode[] GetGenericParameterSyntax(Type t)
        {
            if (!t.IsGenericParameter)
            {
                return new SyntaxNode[]{
                    new IdentifierSyntax(string.Empty, CilAnalysis.GetTypeName(t), string.Empty, false, null) 
                };
            }

            //ECMA-335 II.10.1.7 Generic parameters
            List<SyntaxNode> ret = new List<SyntaxNode>();

            if ((t.GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
            {
                ret.Add(new PunctuationSyntax(string.Empty, "+", " "));
            }
            else if ((t.GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
            {
                ret.Add(new PunctuationSyntax(string.Empty, "-", " "));
            }

            if ((t.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                ret.Add(new KeywordSyntax("class", " "));
            }
            else if((t.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                ret.Add(new KeywordSyntax("valuetype", " "));
            }

            if ((t.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                ret.Add(new KeywordSyntax(".ctor", " "));
            }

            Type[] constrs = t.GetGenericParameterConstraints();

            if (constrs.Length > 0)
            {
                ret.Add(NewPunctuation("("));

                for (int i = 0; i < constrs.Length; i++)
                {
                    if(i>=1) ret.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    IEnumerable<SyntaxNode> nodes = TypeSyntaxGenerator.GetTypeSpecSyntaxAuto(
                        constrs[i], skipAssembly: false, this.containingAssembly);

                    foreach (SyntaxNode node in nodes) ret.Add(node);
                }

                ret.Add(new PunctuationSyntax(string.Empty, ")", " "));
            }

            ret.Add(new IdentifierSyntax(string.Empty, t.Name, string.Empty, false, null));
            return ret.ToArray();
        }

        internal IEnumerable<SyntaxNode> GetEventsSyntax(Type t, int startIndent)
        {
            //ECMA_335 II.18 - Defining events
            EventInfo[] events = t.GetEvents(ReflectionUtils.AllMembers);
            
            for (int i = 0; i < events.Length; i++)
            {
                List<SyntaxNode> inner = new List<SyntaxNode>(10);

                if (events[i].IsSpecialName)
                {
                    inner.Add(new KeywordSyntax("specialname", " "));
                }

                IEnumerable<SyntaxNode> eventTypeSyntax = TypeSyntaxGenerator.GetTypeSpecSyntaxAuto(
                    events[i].EventHandlerType, skipAssembly: false, this.containingAssembly);

                inner.Add(new MemberRefSyntax(eventTypeSyntax.ToArray(), events[i].EventHandlerType));

                inner.Add(new IdentifierSyntax(" ", events[i].Name, Environment.NewLine, true, events[i]));
                inner.Add(new PunctuationSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "{", Environment.NewLine));

                //custom attributes
                try
                {
                    SyntaxNode[] arr = this.GetAttributesSyntax(events[i], startIndent + 2);

                    for (int j = 0; j < arr.Length; j++)
                    {
                        inner.Add(arr[j]);
                    }
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 2),
                            "Failed to show custom attributes. " + ReflectionUtils.GetErrorShortString(ex), 
                            null, false);

                        inner.Add(cs);
                        CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to show event custom attributes.");
                        Diagnostics.OnError(events[i], ea);
                    }
                    else throw;
                }

                //accessors
                MethodInfo adder = events[i].GetAddMethod(true);

                if (adder != null)
                {
                    MemberRefSyntax mref = this.GetMethodRefSyntax(adder, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true);

                    DirectiveSyntax dirAdd = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "addon", new SyntaxNode[] { mref });

                    inner.Add(dirAdd);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                MethodInfo remover = events[i].GetRemoveMethod(true);

                if (remover != null)
                {
                    MemberRefSyntax mref = this.GetMethodRefSyntax(remover, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true);

                    DirectiveSyntax dirRemove = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "removeon", new SyntaxNode[] { mref });

                    inner.Add(dirRemove);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                MethodInfo raiser = events[i].GetRaiseMethod(true);

                if (raiser != null)
                {
                    MemberRefSyntax mref = this.GetMethodRefSyntax(raiser, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true);

                    DirectiveSyntax dirFire = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "fire", new SyntaxNode[] { mref });

                    inner.Add(dirFire);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                PunctuationSyntax ps = new PunctuationSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "}",
                    Environment.NewLine + Environment.NewLine);
                inner.Add(ps);

                DirectiveSyntax dirEvent = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 1), 
                    "event", inner.ToArray());
                yield return dirEvent;
            }//end for
        }

        internal SyntaxNode[] GetAttributesSyntax(ICustomAttributeProvider m, int indent)
        {
            object[] attrs = m.GetCustomAttributes(false);
            List<SyntaxNode> ret = new List<SyntaxNode>(attrs.Length);
            
            for (int i = 0; i < attrs.Length; i++)
            {
                this.GetAttributeSyntax(attrs[i], indent, ret);
            }

            return ret.ToArray();
        }
        
        internal void GetAttributeSyntax(object attr, int indent, List<SyntaxNode> ret)
        {
            string content;
            StringBuilder sb;
            string strIndent = "".PadLeft(indent, ' ');

            //from metadata
            if (attr is ICustomAttribute)
            {
                ICustomAttribute ca = (ICustomAttribute)attr;

                if (ReflectionUtils.IsBuiltInAttribute(ca.Constructor.DeclaringType)) return;

                List<SyntaxNode> children = new List<SyntaxNode>();

                MemberRefSyntax mref = this.GetMethodRefSyntax(ca.Constructor, inlineTok: false, 
                    forceTypeSpec: false, skipAssembly: false);

                children.Add(mref);
                children.Add(new PunctuationSyntax(" ", "=", " "));
                children.Add(new PunctuationSyntax("", "(", " "));
                sb = new StringBuilder(ca.Data.Length * 3);

                for (int j = 0; j < ca.Data.Length; j++)
                {
                    sb.Append(ca.Data[j].ToString("X2", CultureInfo.InvariantCulture));
                    sb.Append(' ');
                }

                children.Add(new GenericSyntax(sb.ToString()));
                children.Add(new PunctuationSyntax(string.Empty, ")", Environment.NewLine));

                DirectiveSyntax dir = new DirectiveSyntax(strIndent, "custom", children.ToArray());
                ret.Add(dir);
                return;
            }

            //from reflection
            Type t = attr.GetType();

            if (ReflectionUtils.IsBuiltInAttribute(t)) return;

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

                    MemberRefSyntax mref = this.GetMethodRefSyntax(constr[0], inlineTok: false, 
                        forceTypeSpec: false, skipAssembly: false);

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
                    s_attr = this.MethodRefToString(constr[0]);
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
                s_attr = TypeSyntaxGenerator.GetTypeSpecString(t);
                output.Write(s_attr);
                output.Flush();
                content = sb.ToString();
                CommentSyntax node = CommentSyntax.Create(strIndent, content, null, false);
                ret.Add(node);
            }
        }

        static SyntaxNode[] GetConstantValueSyntax(Type t, object constant)
        {
            if (constant == null)
            {
                return new SyntaxNode[] { new KeywordSyntax("nullref", string.Empty) };
            }

            List<SyntaxNode> output = new List<SyntaxNode>();

            if (constant.GetType() == typeof(string))
            {
                output.Add(LiteralSyntax.CreateFromValue(string.Empty, constant.ToString(), string.Empty));
            }
            else if (constant.GetType() == typeof(char))
            {
                output.Add(new KeywordSyntax("char", string.Empty));
                output.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));
                ushort val = Convert.ToUInt16(constant);
                string raw = "0x" + val.ToString("X4", CultureInfo.InvariantCulture);
                output.Add(new GenericSyntax(raw));
                output.Add(new PunctuationSyntax(string.Empty, ")", string.Empty));
            }
            else if (constant.GetType() == typeof(bool))
            {
                output.Add(new KeywordSyntax("bool", string.Empty));
                output.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

                if ((bool)constant) output.Add(new KeywordSyntax("true", string.Empty));
                else output.Add(new KeywordSyntax("false", string.Empty));

                output.Add(new PunctuationSyntax(string.Empty, ")", string.Empty));
            }
            else //most of the types...
            {
                Type constType;

                if (ReflectionUtils.IsEnumType(t))
                {
                    constType = constant.GetType(); //use enum underlying numeric type
                }
                else
                {
                    constType = t;
                }

                TypeSyntaxGenerator gen = new TypeSyntaxGenerator();
                IEnumerable<SyntaxNode> syntax = gen.GetTypeNameSyntax(constType);

                foreach (SyntaxNode node in syntax) output.Add(node);

                output.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));
                output.Add(LiteralSyntax.CreateFromValue(string.Empty, constant, string.Empty));
                output.Add(new PunctuationSyntax(string.Empty, ")", string.Empty));
            }

            return output.ToArray();
        }

        internal SyntaxNode[] GetDefaultsSyntax(MethodBase m, int startIndent)
        {
            ParameterInfo[] pars = m.GetParameters();
            List<SyntaxNode> ret = new List<SyntaxNode>(pars.Length);

            // Return type custom attributes
            ICustomAttributeProvider provider = null;

            if (m is MethodInfo)
            {
                try { provider = ((MethodInfo)m).ReturnTypeCustomAttributes; }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        CommentSyntax commentSyntax = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                            "NOTE: Return type and parameters custom attributes are not shown.", null, false);
                        ret.Add(commentSyntax);
                    }
                    else throw;
                }
            }

            if (provider != null)
            {
                object[] attrs = provider.GetCustomAttributes(false);

                if (attrs.Length > 0)
                {
                    DirectiveSyntax dir = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "param",
                        new SyntaxNode[] { new GenericSyntax("[0]" + Environment.NewLine) });
                    ret.Add(dir);

                    for (int i = 0; i < attrs.Length; i++)
                    {
                        this.GetAttributeSyntax(attrs[i], startIndent + 1, ret);
                    }
                }
            }

            // Parameters
            for (int i = 0; i < pars.Length; i++)
            {
                object[] attrs = new object[0];

                try
                {
                    attrs = pars[i].GetCustomAttributes(false);
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        Diagnostics.OnError(m,
                            new CilErrorEventArgs(ex, "Failed to get parameter custom attributes"));
                    }
                    else throw;
                }

                if ((pars[i].IsOptional && pars[i].RawDefaultValue != DBNull.Value) || attrs.Length > 0)
                {
                    List<SyntaxNode> dirNodes = new List<SyntaxNode>();
                    dirNodes.Add(new PunctuationSyntax(" ", "[", string.Empty));
                    dirNodes.Add(LiteralSyntax.CreateFromValue(string.Empty, i + 1, string.Empty));
                    dirNodes.Add(new PunctuationSyntax(string.Empty, "]", string.Empty));
                    
                    if (pars[i].IsOptional && pars[i].RawDefaultValue != DBNull.Value)
                    {
                        dirNodes.Add(new PunctuationSyntax(" ", "=", " "));
                        SyntaxNode[] valSyntax = GetConstantValueSyntax(pars[i].ParameterType, pars[i].RawDefaultValue);

                        foreach (SyntaxNode node in valSyntax) dirNodes.Add(node);
                    }

                    dirNodes.Add(new GenericSyntax(Environment.NewLine));
                    
                    DirectiveSyntax dir = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "param",
                        dirNodes.ToArray());
                    ret.Add(dir);

                    // Parameter custom attributes
                    for (int j = 0; j < attrs.Length; j++)
                    {
                        this.GetAttributeSyntax(attrs[j], startIndent + 1, ret);
                    }
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
            MemberInfo[] members = t.GetMember(methodName, ReflectionUtils.AllMembers);

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] is MethodBase) return (MethodBase)members[i];
            }

            return null;
        }

        internal MemberRefSyntax GetMethodRefSyntax(MethodBase m, bool inlineTok, bool forceTypeSpec,
            bool skipAssembly)
        {
            List<SyntaxNode> children = new List<SyntaxNode>(50);
            Type t = m.DeclaringType;
            int sentinelPos = -1;

            //we only query parameter types so no need to resolve external references
            ParameterInfo[] pars = ReflectionUtils.GetMethodParams(m, RefResolutionMode.NoResolve);

            MethodInfo mi = m as MethodInfo;
            IEnumerable<SyntaxNode> rt;
            TypeSyntaxGenerator gen = new TypeSyntaxGenerator();
            gen.ContainingAssembly = this.containingAssembly;

            if (inlineTok)
            {
                //for ldtoken instruction the method reference is preceded by "method" keyword
                children.Add(new KeywordSyntax("method", " "));
            }

            //append return type
            if (mi != null)
            {
                //standard reflection implementation: return type exposed via MethodInfo
                rt = gen.GetTypeNameSyntax(mi.ReturnType);
            }
            else if (m is ICustomMethod)
            {
                //CilTools reflection implementation: return type exposed via ICustomMethod
                Type tReturn = ((ICustomMethod)m).ReturnType;

                if (tReturn != null) rt = gen.GetTypeNameSyntax(tReturn);
                else rt = new SyntaxNode[] { new KeywordSyntax("void", string.Empty) };
            }
            else if (m is CustomMethod)
            {
                //CilTools reflection implementation: return type exposed via CustomMethod
                Type tReturn = ((CustomMethod)m).ReturnType;

                if (tReturn != null) rt = gen.GetTypeNameSyntax(tReturn);
                else rt = new SyntaxNode[] { new KeywordSyntax("void", string.Empty) };
            }
            else
            {
                //we append return type here even for constructors
                rt = new SyntaxNode[] { new KeywordSyntax("void", string.Empty) };
            }

            if (!ReflectionUtils.IsMethodStatic(m))
            {
                children.Add(new KeywordSyntax("instance", " "));
            }

            if (m.CallingConvention == CallingConventions.VarArgs)
            {
                children.Add(new KeywordSyntax("vararg", " "));

                Signature sig = ReflectionProperties.Get(m, ReflectionProperties.Signature) as Signature;
                sentinelPos = sig.SentinelPosition;
            }

            foreach (SyntaxNode node in rt) children.Add(node);

            children.Add(new GenericSyntax(" "));

            //append declaring type
            if (t != null && !ReflectionUtils.IsModuleType(t))
            {
                IEnumerable<SyntaxNode> syntax;

                // When method reference is used for property/event accessors, TypeSpec syntax is forced, so we don't 
                // call into Type.IsValueType needlessly 
                // (prevents issues like https://github.com/MSDN-WhiteKnight/CilTools/issues/140).
                // See ECMA-335 II.17 - Defining properties.
                TypeSyntaxGenerator dtGen = new TypeSyntaxGenerator();
                dtGen.SkipAssemblyName = skipAssembly;
                dtGen.ContainingAssembly = this.containingAssembly;

                if (forceTypeSpec)
                {
                    dtGen.IsTypeSpec = true;
                    syntax = dtGen.GetTypeSyntax(t);
                }
                else
                {
                    syntax = dtGen.GetTypeSpecSyntaxAuto(t);
                }

                foreach (SyntaxNode node in syntax) children.Add(node);

                children.Add(NewPunctuation("::"));
            }

            //append name
            children.Add(new IdentifierSyntax(string.Empty, m.Name, string.Empty, true, m));

            if (m.IsGenericMethod)
            {
                children.Add(NewPunctuation("<"));

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) children.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    IEnumerable<SyntaxNode> syntax = gen.GetTypeNameSyntax(args[i]);

                    foreach (SyntaxNode node in syntax) children.Add(node);
                }

                children.Add(NewPunctuation(">"));
            }

            children.Add(NewPunctuation("("));

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) children.Add(new PunctuationSyntax(string.Empty, ",", " "));

                if (i == sentinelPos)
                {
                    // Varargs sentinel position (ECMA-335 II.23.2.2 - MethodRefSig)
                    children.Add(new PunctuationSyntax(string.Empty, "...", " "));
                    children.Add(new PunctuationSyntax(string.Empty, ",", " "));
                }

                IEnumerable<SyntaxNode> syntax = gen.GetTypeNameSyntax(pars[i].ParameterType);

                foreach (SyntaxNode node in syntax) children.Add(node);
            }

            children.Add(NewPunctuation(")"));

            return new MemberRefSyntax(children.ToArray(), m);
        }

        string MethodRefToString(MethodBase m)
        {
            //gets the CIL code of the reference to the specified method
            StringBuilder sb = new StringBuilder(200);
            StringWriter wr = new StringWriter(sb);

            SyntaxNode node = this.GetMethodRefSyntax(m, inlineTok: false, forceTypeSpec: false, skipAssembly: false);

            node.ToText(wr);
            wr.Flush();
            return sb.ToString();
        }

        internal static IEnumerable<SyntaxNode> GetTypeDefSyntax(Type t, bool full,
            DisassemblerParams disassemblerParams, int startIndent)
        {
            Assembly containingAssembly;

            // If we need to assembly-qualify all types, just pretend that we don't know the
            // containing assembly.
            if (disassemblerParams.AssemblyQualifyAllTypes) containingAssembly = null;
            else containingAssembly = ReflectionUtils.GetContainingAssembly(t);

            SyntaxGenerator gen = new SyntaxGenerator(containingAssembly);
            return gen.GetTypeDefSyntaxImpl(t, full, disassemblerParams, startIndent);
        }

        IEnumerable<SyntaxNode> GetTypeDefSyntaxImpl(Type t, bool full, 
            DisassemblerParams disassemblerParams, int startIndent)
        {
            List<SyntaxNode> content = new List<SyntaxNode>(10);
            
            //type standard attributes
            if (t.IsInterface)
            {
                content.Add(new KeywordSyntax("interface", " "));
            }

            if (t.IsNested)
            {
                content.Add(new KeywordSyntax("nested", " "));

                if (t.IsNestedPublic)
                {
                    content.Add(new KeywordSyntax("public", " "));
                }
                else if (t.IsNestedAssembly)
                {
                    content.Add(new KeywordSyntax("assembly", " "));
                }
                else if (t.IsNestedFamily)
                {
                    content.Add(new KeywordSyntax("family", " "));
                }
                else if (t.IsNestedFamORAssem)
                {
                    content.Add(new KeywordSyntax("famorassem", " "));
                }
                else if (t.IsNestedFamANDAssem)
                {
                    content.Add(new KeywordSyntax("famandassem", " "));
                }
                else
                {
                    content.Add(new KeywordSyntax("private", " "));
                }
            }
            else
            {
                if (t.IsPublic)
                {
                    content.Add(new KeywordSyntax("public", " "));
                }
                else
                {
                    content.Add(new KeywordSyntax("private", " "));
                }
            }

            if (t.IsAbstract)
            {
                content.Add(new KeywordSyntax("abstract", " "));
            }

            if (t.IsAutoLayout)
            {
                content.Add(new KeywordSyntax("auto", " "));
            }
            else if (t.IsLayoutSequential)
            {
                content.Add(new KeywordSyntax("sequential", " "));
            }
            else if (t.IsExplicitLayout)
            {
                content.Add(new KeywordSyntax("explicit", " "));
            }

            if (t.IsAnsiClass)
            {
                content.Add(new KeywordSyntax("ansi", " "));
            }
            else if (t.IsUnicodeClass)
            {
                content.Add(new KeywordSyntax("unicode", " "));
            }
            else if (t.IsAutoClass)
            {
                content.Add(new KeywordSyntax("autochar", " "));
            }

            if (t.IsSealed)
            {
                content.Add(new KeywordSyntax("sealed", " "));
            }

            if ((t.Attributes & TypeAttributes.Serializable) != 0)
            {
                //Type.IsSerializable considers base type, but we need a raw flag value here
                content.Add(new KeywordSyntax("serializable", " "));
            }

            if (t.IsSpecialName)
            {
                content.Add(new KeywordSyntax("specialname", " "));
            }

            if ((t.Attributes & TypeAttributes.RTSpecialName) != 0)
            {
                content.Add(new KeywordSyntax("rtspecialname", " "));
            }

            if ((t.Attributes & TypeAttributes.BeforeFieldInit) != 0)
            {
                content.Add(new KeywordSyntax("beforefieldinit", " "));
            }

            //type name
            string tname = "";
            if (!t.IsNested && !string.IsNullOrEmpty(t.Namespace)) tname += t.Namespace + ".";
            tname += t.Name;
            content.Add(new IdentifierSyntax(string.Empty, tname, string.Empty, true, t));

            //generic parameters
            if (t.IsGenericType)
            {
                content.Add(NewPunctuation("<"));
                Type[] targs = t.GetGenericArguments();

                for (int i = 0; i < targs.Length; i++)
                {
                    if (i >= 1) content.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    SyntaxNode[] gpSyntax = this.GetGenericParameterSyntax(targs[i]);

                    for (int j = 0; j < gpSyntax.Length; j++)
                    {
                        content.Add(gpSyntax[j]);
                    }
                }

                content.Add(NewPunctuation(">"));
            }

            content.Add(new GenericSyntax(Environment.NewLine));

            //base type
            if (!t.IsInterface && t.BaseType != null)
            {
                content.Add(new KeywordSyntax(SyntaxUtils.GetIndentString(startIndent), "extends", " ", KeywordKind.Other));

                try
                {
                    IEnumerable<SyntaxNode> baseTypeNodes = TypeSyntaxGenerator.GetTypeSpecSyntaxAuto(
                        t.BaseType, skipAssembly: false, this.containingAssembly);

                    MemberRefSyntax mrsBaseType = new MemberRefSyntax(baseTypeNodes.ToArray(), t.BaseType);
                    content.Add(mrsBaseType);
                }
                catch (TypeLoadException ex)
                {
                    //handle error when base type is not available
                    content.Add(new IdentifierSyntax(string.Empty, "UnknownType", string.Empty, 
                        ismember: false, target: null));

                    Diagnostics.OnError(t, new CilErrorEventArgs(ex, "Failed to read base type for: " + t.Name));
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
                Diagnostics.OnError(t, new CilErrorEventArgs(ex, "Failed to get interfaces."));
            }

            if (interfaces != null && interfaces.Length > 0)
            {
                content.Add(new KeywordSyntax(SyntaxUtils.GetIndentString(startIndent), "implements", " ", KeywordKind.Other));

                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (i >= 1)
                    {
                        content.Add(new PunctuationSyntax(string.Empty, ",", Environment.NewLine));
                    }

                    IEnumerable<SyntaxNode> ifNodes = TypeSyntaxGenerator.GetTypeSpecSyntaxAuto(
                        interfaces[i], skipAssembly: false, this.containingAssembly);

                    content.Add(new MemberRefSyntax(ifNodes.ToArray(), interfaces[i]));
                }

                content.Add(new GenericSyntax(Environment.NewLine));
            }

            int bodyIndent;
            bool isModuleType = ReflectionUtils.IsModuleType(t); //module type holds global fields and functions

            if (!isModuleType)
            {
                DirectiveSyntax header = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent), "class", content.ToArray());
                yield return header;
                bodyIndent = startIndent + 1;
            }
            else
            {
                bodyIndent = startIndent;
            }

            //body (ECMA 335 II.10.2 - Body of a type definition)
            content.Clear();

            //struct layout 
            StructLayoutAttribute sla = null;

            try
            {
                sla = t.StructLayoutAttribute;
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get struct layout.");
                    Diagnostics.OnError(t, ea);
                }
                else throw;
            }

            if (sla != null && sla.Value != LayoutKind.Auto && (sla.Size != 0 || sla.Pack != 0))
            {
                //It is unclear whether layout attributes are supported for auto layout or not,
                //but runtime reflection sometimes returns bogus values that aren't actually 
                //present in metadata, so we skip it

                LiteralSyntax ls = LiteralSyntax.CreateFromValue(string.Empty, sla.Pack, Environment.NewLine);
                DirectiveSyntax ds = new DirectiveSyntax(SyntaxUtils.GetIndentString(bodyIndent),
                    "pack", new SyntaxNode[] { ls });
                content.Add(ds);

                ls = LiteralSyntax.CreateFromValue(string.Empty, sla.Size, Environment.NewLine);
                ds = new DirectiveSyntax(SyntaxUtils.GetIndentString(bodyIndent),
                    "size", new SyntaxNode[] { ls });
                content.Add(ds);
            }

            //custom attributes
            try
            {
                SyntaxNode[] arr = this.GetAttributesSyntax(t, startIndent + 1);

                for (int i = 0; i < arr.Length; i++)
                {
                    content.Add(arr[i]);
                }
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                        "Failed to get custom attributes. " + ReflectionUtils.GetErrorShortString(ex), trail: null, isRaw: false);

                    content.Add(cs);
                    CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get type custom attributes.");
                    Diagnostics.OnError(t, ea);
                }
                else throw;
            }

            content.Add(new GenericSyntax(Environment.NewLine));
            
            //fields
            TypeSyntaxGenerator tgen = new TypeSyntaxGenerator(this.containingAssembly);
            FieldInfo[] fields = t.GetFields(ReflectionUtils.AllMembers);

            if (isModuleType && fields.Length > 0)
            {
                CommentSyntax cs = CommentSyntax.Create(string.Empty, " *** Global fields ***", null, false);
                content.Add(cs);
            }

            for (int i = 0; i < fields.Length; i++)
            {
                List<SyntaxNode> inner = new List<SyntaxNode>(10);

                if (fields[i].IsPublic)
                {
                    inner.Add(new KeywordSyntax("public", " "));
                }
                else if (fields[i].IsAssembly)
                {
                    inner.Add(new KeywordSyntax("assembly", " ")); //internal
                }
                else if (fields[i].IsFamily)
                {
                    inner.Add(new KeywordSyntax("family", " ")); //protected
                }
                else if (fields[i].IsFamilyOrAssembly)
                {
                    inner.Add(new KeywordSyntax("famorassem", " ")); //protected internal
                }
                else if (fields[i].IsFamilyAndAssembly)
                {
                    inner.Add(new KeywordSyntax("famandassem", " "));
                }
                else
                {
                    inner.Add(new KeywordSyntax("private", " "));
                }

                if (fields[i].IsStatic)
                {
                    inner.Add(new KeywordSyntax("static", " "));
                }

                if (fields[i].IsInitOnly)
                {
                    inner.Add(new KeywordSyntax("initonly", " "));
                }

                if (fields[i].IsLiteral)
                {
                    inner.Add(new KeywordSyntax("literal", " "));
                }

                if (fields[i].IsNotSerialized)
                {
                    inner.Add(new KeywordSyntax("notserialized", " "));
                }

                if (fields[i].IsSpecialName)
                {
                    inner.Add(new KeywordSyntax("specialname", " "));
                }

                if ((fields[i].Attributes & FieldAttributes.RTSpecialName) != 0)
                {
                    inner.Add(new KeywordSyntax("rtspecialname", " "));
                }

                SyntaxNode[] ftNodes = tgen.GetTypeNameSyntax(fields[i].FieldType).ToArray();
                inner.Add(new MemberRefSyntax(ftNodes, fields[i].FieldType));

                inner.Add(new IdentifierSyntax(" ", fields[i].Name, string.Empty, true, fields[i]));

                object constval = DBNull.Value;

                try { constval = fields[i].GetRawConstantValue(); }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
                catch (InvalidOperationException) { }

                if (constval != DBNull.Value)
                {
                    inner.Add(new PunctuationSyntax(" ", "=", " "));
                    SyntaxNode[] valSyntax = GetConstantValueSyntax(fields[i].FieldType, constval);

                    foreach (SyntaxNode node in valSyntax) inner.Add(node);
                }

                inner.Add(new GenericSyntax(Environment.NewLine));

                //field custom attributes
                try
                {
                    SyntaxNode[] arr = this.GetAttributesSyntax(fields[i], bodyIndent);

                    for (int j = 0; j < arr.Length; j++)
                    {
                        inner.Add(arr[j]);
                    }

                    if(arr.Length > 0) inner.Add(new GenericSyntax(Environment.NewLine));
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get field custom attributes.");
                        Diagnostics.OnError(t, ea);
                    }
                    else throw;
                }

                DirectiveSyntax field = new DirectiveSyntax(SyntaxUtils.GetIndentString(bodyIndent),
                    "field", inner.ToArray());
                content.Add(field);
            }

            if (fields.Length > 0) content.Add(new GenericSyntax(Environment.NewLine));

            //properties
            PropertyInfo[] props;

            try
            {
                props = t.GetProperties(ReflectionUtils.AllMembers);
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    props = new PropertyInfo[0];

                    CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                        "Failed to get properties. " + ReflectionUtils.GetErrorShortString(ex), trail: null, isRaw: false);

                    content.Add(cs);
                    CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get properties.");
                    Diagnostics.OnError(t, ea);
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
                    inner.Add(new KeywordSyntax("specialname", " "));
                }

                bool isStatic = false;

                if (props[i].CanRead) getter = GetPropertyMethod(props[i], "get");
                if (props[i].CanWrite) setter = GetPropertyMethod(props[i], "set");

                if (getter != null && getter.IsStatic) isStatic = true;
                
                if (!isStatic)
                {
                    inner.Add(new KeywordSyntax("instance", " "));
                }

                SyntaxNode[] ptNodes = tgen.GetTypeNameSyntax(props[i].PropertyType).ToArray();
                inner.Add(new MemberRefSyntax(ptNodes, props[i].PropertyType));

                inner.Add(new IdentifierSyntax(" ", props[i].Name, string.Empty, true, props[i]));

                //index parameters
                ParameterInfo[] pars = props[i].GetIndexParameters();
                if (pars == null) pars = new ParameterInfo[0];

                inner.Add(NewPunctuation("("));

                for (int j = 0; j < pars.Length; j++)
                {
                    if (j >= 1) inner.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    SyntaxNode[] partype = tgen.GetTypeNameSyntax(pars[j].ParameterType).ToArray();
                    inner.Add(new MemberRefSyntax(partype, pars[j].ParameterType));
                }

                inner.Add(new PunctuationSyntax(string.Empty, ")", Environment.NewLine));
                inner.Add(new PunctuationSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "{", Environment.NewLine));

                //property custom attributes
                try
                {
                    SyntaxNode[] arr = this.GetAttributesSyntax(props[i], startIndent + 2);

                    for (int j = 0; j < arr.Length; j++)
                    {
                        inner.Add(arr[j]);
                    }
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get property custom attributes.");
                        Diagnostics.OnError(t, ea);
                    }
                    else throw;
                }

                //property methods
                if (getter != null)
                {
                    MemberRefSyntax mref = this.GetMethodRefSyntax(getter, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true);

                    DirectiveSyntax dirGet = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "get", new SyntaxNode[] { mref });

                    inner.Add(dirGet);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                if (setter != null)
                {
                    MemberRefSyntax mref = this.GetMethodRefSyntax(setter, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true);

                    DirectiveSyntax dirSet = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "set", new SyntaxNode[] { mref });

                    inner.Add(dirSet);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                // Property method references skip assembly name, because ilasm dies when it encounters syntax like
                //     .get int32 [MyAssembly]MyType::get_X()
                // Probably accessors from different assemblies are not supported; ECMA spec is not clear whether
                // they should be.

                inner.Add(new PunctuationSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "}",
                    Environment.NewLine + Environment.NewLine));
                DirectiveSyntax dirProp = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 1),
                    "property", inner.ToArray());
                content.Add(dirProp);
            }

            //events
            try
            {
                foreach (SyntaxNode node in this.GetEventsSyntax(t, startIndent))
                {
                    content.Add(node);
                }
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                        "Failed to get events. " + ReflectionUtils.GetErrorShortString(ex), trail: null, isRaw: false);

                    content.Add(cs);
                    CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get events.");
                    Diagnostics.OnError(t, ea);
                }
                else throw;
            }

            if (full)
            {
                //constructors
                if (!isModuleType)
                {
                    ConstructorInfo[] constructors;

                    try
                    {
                        constructors = t.GetConstructors(ReflectionUtils.AllMembers | BindingFlags.DeclaredOnly);
                    }
                    catch (Exception ex)
                    {
                        if (ReflectionUtils.IsExpectedException(ex))
                        {
                            string errorStr = "Failed to get constructors. " + ReflectionUtils.GetErrorShortString(ex);

                            CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                                errorStr, trail: null, isRaw: false);

                            content.Add(cs);
                            CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get constructors.");
                            Diagnostics.OnError(t, ea);
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
                }

                //methods
                MethodInfo[] methods;

                try
                {
                    methods = t.GetMethods(ReflectionUtils.AllMembers | BindingFlags.DeclaredOnly);
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        string errorStr = "Failed to get methods. " + ReflectionUtils.GetErrorShortString(ex);

                        CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(bodyIndent),
                            errorStr, trail: null, isRaw: false);

                        content.Add(cs);
                        CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get methods.");
                        Diagnostics.OnError(t, ea);
                        methods = new MethodInfo[0];
                    }
                    else throw;
                }

                if (isModuleType && methods.Length > 0)
                {
                    CommentSyntax cs = CommentSyntax.Create(string.Empty, " *** Global functions ***", null, false);
                    content.Add(cs);
                }

                for (int i = 0; i < methods.Length; i++)
                {
                    CilGraph gr = CilGraph.Create(methods[i]);
                    MethodDefSyntax mds = gr.ToSyntaxTreeImpl(disassemblerParams, bodyIndent);
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
                        types = t.GetNestedTypes(ReflectionUtils.AllMembers);
                    }
                    catch (Exception ex)
                    {
                        if (ReflectionUtils.IsExpectedException(ex))
                        {
                            string errorStr = "Failed to get nested types. " + ReflectionUtils.GetErrorShortString(ex);

                            CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                                errorStr, trail: null, isRaw: false);

                            content.Add(cs);
                            CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to get nested types.");
                            Diagnostics.OnError(t, ea);
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
                content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(bodyIndent), "...", null, false));
                content.Add(new GenericSyntax(Environment.NewLine));
            }

            if (!isModuleType)
            {
                // Normal type is printed as block
                BlockSyntax body = new BlockSyntax(SyntaxUtils.GetIndentString(startIndent),
                    SyntaxNode.EmptyArray, content.ToArray());
                
                yield return body;
            }
            else
            {
                // Global fields and functions are just printed at root level
                for (int i = 0; i < content.Count; i++)
                {
                    yield return content[i];
                }
            }
        }
    }
}
