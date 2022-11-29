/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    /// <summary>
    /// Provides internal static helpers that produce sequences of syntax nodes
    /// </summary>
    internal static class SyntaxGenerator
    {
        internal static SyntaxNode[] GetGenericParameterSyntax(Type t)
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
                ret.Add(new KeywordSyntax(string.Empty, "class", " ", KeywordKind.Other));
            }
            else if((t.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                ret.Add(new KeywordSyntax(string.Empty, "valuetype", " ", KeywordKind.Other));
            }

            if ((t.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                ret.Add(new KeywordSyntax(string.Empty, ".ctor", " ", KeywordKind.Other));
            }

            Type[] constrs = t.GetGenericParameterConstraints();

            if (constrs.Length > 0)
            {
                ret.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

                for (int i = 0; i < constrs.Length; i++)
                {
                    if(i>=1) ret.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeSpecSyntaxAuto(constrs[i], skipAssembly: false);

                    foreach (SyntaxNode node in nodes) ret.Add(node);
                }

                ret.Add(new PunctuationSyntax(string.Empty, ")", " "));
            }

            ret.Add(new IdentifierSyntax(string.Empty, t.Name, string.Empty, false, null));
            return ret.ToArray();
        }

        internal static IEnumerable<SyntaxNode> GetEventsSyntax(Type t, int startIndent)
        {
            //ECMA_335 II.18 - Defining events
            EventInfo[] events = t.GetEvents(ReflectionUtils.AllMembers);
            
            for (int i = 0; i < events.Length; i++)
            {
                List<SyntaxNode> inner = new List<SyntaxNode>(10);

                if (events[i].IsSpecialName)
                {
                    inner.Add(new KeywordSyntax(string.Empty, "specialname", " ", KeywordKind.Other));
                }

                IEnumerable<SyntaxNode> eventTypeSyntax = CilAnalysis.GetTypeSpecSyntaxAuto(
                    events[i].EventHandlerType, skipAssembly: false);

                inner.Add(new MemberRefSyntax(eventTypeSyntax.ToArray(), events[i].EventHandlerType));

                inner.Add(new IdentifierSyntax(" ", events[i].Name, Environment.NewLine, true, events[i]));
                inner.Add(new PunctuationSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "{", Environment.NewLine));

                //custom attributes
                try
                {
                    SyntaxNode[] arr = GetAttributesSyntax(events[i], startIndent + 2, DisassemblerParams.Default);

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
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(adder, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true, ReflectionUtils.GetContainingAssembly(t));

                    DirectiveSyntax dirAdd = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "addon", new SyntaxNode[] { mref });

                    inner.Add(dirAdd);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                MethodInfo remover = events[i].GetRemoveMethod(true);

                if (remover != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(remover, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true, ReflectionUtils.GetContainingAssembly(t));

                    DirectiveSyntax dirRemove = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "removeon", new SyntaxNode[] { mref });

                    inner.Add(dirRemove);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                MethodInfo raiser = events[i].GetRaiseMethod(true);

                if (raiser != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(raiser, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true, ReflectionUtils.GetContainingAssembly(t));

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

        internal static SyntaxNode[] GetAttributesSyntax(ICustomAttributeProvider m, int indent, DisassemblerParams pars)
        {
            object[] attrs = m.GetCustomAttributes(false);
            List<SyntaxNode> ret = new List<SyntaxNode>(attrs.Length);
            Assembly containingAssembly;

            // If we need to assembly-qualify all types, just pretend that we don't know the
            // containing assembly.
            if (pars.AssemblyQualifyAllTypes) containingAssembly = null;
            else containingAssembly = ReflectionUtils.GetProviderAssembly(m);
            
            for (int i = 0; i < attrs.Length; i++)
            {
                GetAttributeSyntax(attrs[i], indent, ret, containingAssembly);
            }

            return ret.ToArray();
        }

        internal static bool IsBuiltInAttribute(Type t)
        {
            if (string.Equals(t.FullName, "System.Runtime.InteropServices.OptionalAttribute",
                StringComparison.Ordinal))
            {
                //OptionalAttribute is translated to [opt]
                return true;
            }
            else if (string.Equals(t.FullName, "System.SerializableAttribute", StringComparison.Ordinal))
            {
                //SerializableAttribute is represented by built-in attribute flag, but runtime reflection still
                //synthesizes it
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static void GetAttributeSyntax(object attr, int indent, List<SyntaxNode> ret, Assembly containingAssembly)
        {
            string content;
            StringBuilder sb;
            string strIndent = "".PadLeft(indent, ' ');

            //from metadata
            if (attr is ICustomAttribute)
            {
                ICustomAttribute ca = (ICustomAttribute)attr;

                if (IsBuiltInAttribute(ca.Constructor.DeclaringType)) return;

                List<SyntaxNode> children = new List<SyntaxNode>();

                MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(ca.Constructor, inlineTok: false, 
                    forceTypeSpec: false, skipAssembly: false, containingAssembly);

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
                children.Add(new PunctuationSyntax("", ")", Environment.NewLine));

                DirectiveSyntax dir = new DirectiveSyntax(strIndent, "custom", children.ToArray());
                ret.Add(dir);
                return;
            }

            //from reflection
            Type t = attr.GetType();

            if (IsBuiltInAttribute(t)) return;

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

                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(constr[0], inlineTok: false, 
                        forceTypeSpec: false, skipAssembly: false, containingAssembly);

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
                    s_attr = CilAnalysis.MethodRefToString(constr[0], containingAssembly);
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
            Assembly containingAssembly = ReflectionUtils.GetContainingAssembly(m);

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
                        GetAttributeSyntax(attrs[i], startIndent + 1, ret, containingAssembly);
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
                    StringBuilder sb = new StringBuilder(100);
                    StringWriter output = new StringWriter(sb);
                    output.Write(' ');
                    output.Write('[');
                    output.Write((i + 1).ToString());
                    output.Write(']');

                    if (pars[i].IsOptional && pars[i].RawDefaultValue != DBNull.Value)
                    {
                        output.Write(" = ");

                        string valstr = ReflectionUtils.GetConstantValueString(pars[i].ParameterType, pars[i].RawDefaultValue);
                        output.WriteLine(valstr);
                    }
                    else
                    {
                        output.WriteLine();
                    }

                    output.Flush();

                    string content = sb.ToString();
                    DirectiveSyntax dir = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "param",
                        new SyntaxNode[] { new GenericSyntax(content) });
                    ret.Add(dir);

                    // Parameter custom attributes
                    for (int j = 0; j < attrs.Length; j++)
                    {
                        GetAttributeSyntax(attrs[j], startIndent + 1, ret, containingAssembly);
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

            if ((t.Attributes & TypeAttributes.Serializable) != 0)
            {
                //Type.IsSerializable considers base type, but we need a raw flag value here
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
            content.Add(new IdentifierSyntax(String.Empty, tname, String.Empty, true, t));

            //generic parameters
            if (t.IsGenericType)
            {
                content.Add(new PunctuationSyntax(String.Empty, "<", String.Empty));
                Type[] targs = t.GetGenericArguments();

                for (int i = 0; i < targs.Length; i++)
                {
                    if (i >= 1) content.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    SyntaxNode[] gpSyntax = GetGenericParameterSyntax(targs[i]);

                    for (int j = 0; j < gpSyntax.Length; j++)
                    {
                        content.Add(gpSyntax[j]);
                    }
                }

                content.Add(new PunctuationSyntax(String.Empty, ">", String.Empty));
            }

            content.Add(new GenericSyntax(Environment.NewLine));

            //base type
            if (!t.IsInterface && t.BaseType != null)
            {
                content.Add(new KeywordSyntax(SyntaxUtils.GetIndentString(startIndent), "extends", " ", KeywordKind.Other));

                try
                {
                    IEnumerable<SyntaxNode> baseTypeNodes = CilAnalysis.GetTypeSpecSyntaxAuto(t.BaseType, skipAssembly: false);
                    MemberRefSyntax mrsBaseType = new MemberRefSyntax(baseTypeNodes.ToArray(), t.BaseType);
                    content.Add(mrsBaseType);
                }
                catch (TypeLoadException ex)
                {
                    //handle error when base type is not available
                    content.Add(new IdentifierSyntax(String.Empty, "UnknownType", String.Empty, false, null));

                    Diagnostics.OnError(
                        t, new CilErrorEventArgs(ex, "Failed to read base type for: " + t.Name)
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
                content.Add(new KeywordSyntax(SyntaxUtils.GetIndentString(startIndent), "implements", " ", KeywordKind.Other));

                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (i >= 1)
                    {
                        content.Add(new PunctuationSyntax(String.Empty, ",", Environment.NewLine));
                    }

                    IEnumerable<SyntaxNode> ifNodes = CilAnalysis.GetTypeSpecSyntaxAuto(interfaces[i], skipAssembly: false);
                    content.Add(new MemberRefSyntax(ifNodes.ToArray(), interfaces[i]));
                }

                content.Add(new GenericSyntax(Environment.NewLine));
            }

            int bodyIndent;
            bool isModuleType = CilAnalysis.IsModuleType(t); //module type holds global fields and functions

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

            //body
            content.Clear();

            //custom attributes
            try
            {
                SyntaxNode[] arr = GetAttributesSyntax(t, startIndent + 1, disassemblerParams);

                for (int i = 0; i < arr.Length; i++)
                {
                    content.Add(arr[i]);
                }
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                        "NOTE: Custom attributes are not shown.", null, false));
                }
                else throw;
            }

            content.Add(new GenericSyntax(Environment.NewLine));
            Assembly containingAssembly;

            // If we need to assembly-qualify all types, just pretend that we don't know the
            // containing assembly.
            if (disassemblerParams.AssemblyQualifyAllTypes) containingAssembly = null;
            else containingAssembly = ReflectionUtils.GetProviderAssembly(t);

            //fields
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
                else if (fields[i].IsFamilyOrAssembly)
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

                if ((fields[i].Attributes & FieldAttributes.RTSpecialName) != 0)
                {
                    inner.Add(new KeywordSyntax(String.Empty, "rtspecialname", " ", KeywordKind.Other));
                }

                SyntaxNode[] ftNodes = CilAnalysis.GetTypeNameSyntax(fields[i].FieldType, containingAssembly).ToArray();
                inner.Add(new MemberRefSyntax(ftNodes, fields[i].FieldType));

                inner.Add(new IdentifierSyntax(" ", fields[i].Name, String.Empty, true, fields[i]));

                object constval = DBNull.Value;

                try { constval = fields[i].GetRawConstantValue(); }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
                catch (InvalidOperationException) { }

                if (constval != DBNull.Value)
                {
                    string valstr = ReflectionUtils.GetConstantValueString(fields[i].FieldType, constval);
                    inner.Add(new PunctuationSyntax(" ", "=", " "));
                    inner.Add(new GenericSyntax(valstr));
                }

                inner.Add(new GenericSyntax(Environment.NewLine));

                //field custom attributes
                try
                {
                    SyntaxNode[] arr = GetAttributesSyntax(fields[i], bodyIndent, disassemblerParams);

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
                        CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(bodyIndent),
                            "Failed to show field custom attributes. " + ReflectionUtils.GetErrorShortString(ex), null, false);

                        inner.Add(cs);
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
                    content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
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
                
                if (!isStatic)
                {
                    inner.Add(new KeywordSyntax(string.Empty, "instance", " ", KeywordKind.Other));
                }

                SyntaxNode[] ptNodes = CilAnalysis.GetTypeNameSyntax(props[i].PropertyType, containingAssembly).ToArray();
                inner.Add(new MemberRefSyntax(ptNodes, props[i].PropertyType));

                inner.Add(new IdentifierSyntax(" ", props[i].Name, string.Empty, true, props[i]));

                //index parameters
                ParameterInfo[] pars = props[i].GetIndexParameters();
                if (pars == null) pars = new ParameterInfo[0];

                inner.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

                for (int j = 0; j < pars.Length; j++)
                {
                    if (j >= 1) inner.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    SyntaxNode[] partype = CilAnalysis.GetTypeNameSyntax(pars[j].ParameterType, containingAssembly).ToArray();
                    inner.Add(new MemberRefSyntax(partype, pars[j].ParameterType));
                }

                inner.Add(new PunctuationSyntax(string.Empty, ")", Environment.NewLine));
                inner.Add(new PunctuationSyntax(SyntaxUtils.GetIndentString(startIndent + 1), "{", Environment.NewLine));

                //property custom attributes
                try
                {
                    SyntaxNode[] arr = GetAttributesSyntax(props[i], startIndent + 2, disassemblerParams);

                    for (int j = 0; j < arr.Length; j++)
                    {
                        inner.Add(arr[j]);
                    }
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        inner.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 2),
                            "NOTE: Custom attributes are not shown.", null, false));
                    }
                    else throw;
                }

                //property methods
                if (getter != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(getter, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true, containingAssembly);

                    DirectiveSyntax dirGet = new DirectiveSyntax(SyntaxUtils.GetIndentString(startIndent + 2),
                        "get", new SyntaxNode[] { mref });

                    inner.Add(dirGet);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                if (setter != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(setter, inlineTok: false, 
                        forceTypeSpec: true, skipAssembly: true, containingAssembly);

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
                foreach (SyntaxNode node in GetEventsSyntax(t, startIndent))
                {
                    content.Add(node);
                }
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    CommentSyntax cs = CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
                        "Failed to show events. " + ReflectionUtils.GetErrorShortString(ex), null, false);

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
                        constructors = t.GetConstructors(ReflectionUtils.AllMembers);
                    }
                    catch (Exception ex)
                    {
                        if (ReflectionUtils.IsExpectedException(ex))
                        {
                            content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
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
                }

                //methods
                MethodInfo[] methods;

                try
                {
                    methods = t.GetMethods(ReflectionUtils.AllMembers);
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(bodyIndent),
                            "NOTE: Methods are not shown.", null, false));
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
                            content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(startIndent + 1),
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
                content.Add(CommentSyntax.Create(SyntaxUtils.GetIndentString(bodyIndent), "...", null, false));
                content.Add(new GenericSyntax(Environment.NewLine));
            }

            if (!isModuleType)
            {
                // Normal type is printed as block
                BlockSyntax body = new BlockSyntax(SyntaxUtils.GetIndentString(startIndent),
                    SyntaxNode.EmptyArray, content.ToArray());

                for (int i = 0; i < body._children.Count; i++) body._children[i]._parent = body;

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
