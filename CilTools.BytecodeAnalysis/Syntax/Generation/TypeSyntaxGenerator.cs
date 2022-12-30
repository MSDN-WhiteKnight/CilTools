/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax.Generation
{
    /// <summary>
    /// Generates sequences of syntax nodes that represent type references in CIL assembler
    /// </summary>
    internal class TypeSyntaxGenerator
    {
        public TypeSyntaxGenerator()
        {
            this.IsTypeSpec = false;
            this.SkipAssemblyName = false;
        }

        public TypeSyntaxGenerator(Assembly ass)
        {
            this.IsTypeSpec = false;
            this.SkipAssemblyName = false;
            this.ContainingAssembly = ass;
        }

        public TypeSyntaxGenerator(bool isSpec, bool skipAssembly)
        {
            this.IsTypeSpec = isSpec;
            this.SkipAssemblyName = skipAssembly;
        }

        public bool IsTypeSpec { get; set; }
        public bool SkipAssemblyName { get; set; }
        public Assembly ContainingAssembly { get; set; }

        /// <summary>
        /// Gets the syntax for a Type or TypeSpec construct (selects automatically)
        /// </summary>
        internal static IEnumerable<SyntaxNode> GetTypeSpecSyntaxAuto(Type t, bool skipAssembly, Assembly containingAssembly)
        {
            //this is used when we can omit class/valuetype prefix, such as for method calls
            Debug.Assert(t != null, "GetTypeSpecSyntaxAuto: Source type cannot be null");

            TypeSyntaxGenerator gen = new TypeSyntaxGenerator();
            gen.SkipAssemblyName = skipAssembly;
            gen.ContainingAssembly = containingAssembly;

            return gen.GetTypeSpecSyntaxAuto(t);
        }

        internal static string GetTypeSpecString(Type t)
        {
            //this is used when we can omit class/valuetype prefix, such as for method calls
            Debug.Assert(t != null, "GetTypeSpecString: Source type cannot be null");

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);
            IEnumerable<SyntaxNode> nodes = GetTypeSpecSyntaxAuto(t, skipAssembly: false, containingAssembly: null);

            foreach (SyntaxNode node in nodes) node.ToText(wr);

            wr.Flush();
            return sb.ToString();
        }

        public IEnumerable<SyntaxNode> GetTypeSyntax(Type t)
        {
            //converts reflection Type object to Type or TypeSpec CIL assembler syntax
            //(ECMA-335 II.7.1 Types)

            if (t.IsGenericParameter)
            {
                string prefix;

                if (t.DeclaringMethod == null) prefix = "!";
                else prefix = "!!";

                if (string.IsNullOrEmpty(t.Name))
                {
                    yield return new GenericSyntax(prefix + t.GenericParameterPosition.ToString());
                }
                else
                {
                    yield return new GenericSyntax(prefix + t.Name);
                }
            }
            else if (t.IsByRef)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "&", string.Empty);
            }
            else if (t.IsArray && t.GetArrayRank() == 1)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "[]", string.Empty);
            }
            else if (t.IsPointer)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "*", string.Empty);
            }
            else if (t is ITypeInfo && ((ITypeInfo)t).IsFunctionPointer)
            {
                yield return new KeywordSyntax(string.Empty, "method", " ", KeywordKind.Other);
                IEnumerable<SyntaxNode> nodes = ((ITypeInfo)t).TargetSignature.ToSyntax(pointer: true, this.ContainingAssembly);
                foreach (SyntaxNode x in nodes) yield return x;
            }
            else
            {
                if (!this.IsTypeSpec) //for TypeSpec, we omit class/valuetype keyword
                {
                    if (t.IsValueType) yield return new KeywordSyntax(string.Empty, "valuetype", " ", KeywordKind.Other);
                    else yield return new KeywordSyntax(string.Empty, "class", " ", KeywordKind.Other);
                }

                if (!this.SkipAssemblyName && !ReflectionUtils.IsTypeInAssembly(t, this.ContainingAssembly))
                {
                    Assembly ass = t.Assembly;

                    if (ass != null)
                    {
                        yield return new PunctuationSyntax(string.Empty, "[", string.Empty);
                        yield return new IdentifierSyntax(string.Empty, ass.GetName().Name, string.Empty, false, ass);
                        yield return new PunctuationSyntax(string.Empty, "]", string.Empty);
                    }
                }

                //type name itself

                if (!string.IsNullOrEmpty(t.Namespace))
                {
                    yield return new IdentifierSyntax(string.Empty, t.Namespace, string.Empty, true, null);
                    yield return new PunctuationSyntax(string.Empty, ".", string.Empty);
                }

                if (t.IsNested && t.DeclaringType != null)
                {
                    yield return new IdentifierSyntax(string.Empty, t.DeclaringType.Name, string.Empty, true, t.DeclaringType);
                    yield return new PunctuationSyntax(string.Empty, "/", string.Empty);
                }

                yield return new IdentifierSyntax(string.Empty, t.Name, string.Empty, true, t);

                if (t.IsGenericType && !this.IsTypeSpec)
                {
                    yield return new PunctuationSyntax(string.Empty, "<", string.Empty);

                    Type[] args = t.GetGenericArguments();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i >= 1) yield return new PunctuationSyntax(string.Empty, ",", " ");

                        IEnumerable<SyntaxNode> nodes = this.GetTypeNameSyntax(args[i]);

                        foreach (SyntaxNode node in nodes) yield return node;
                    }

                    yield return new PunctuationSyntax(string.Empty, ">", string.Empty);
                }
            }

            //custom modifiers

            if (t is ITypeInfo)
            {
                foreach (CustomModifier m in ((ITypeInfo)t).Modifiers)
                {
                    foreach (SyntaxNode node in m.ToSyntax(this.ContainingAssembly)) yield return node;
                }
            }
        }

        public IEnumerable<SyntaxNode> GetTypeNameSyntax(Type t)
        {
            if (t == null) yield break;

            if (t.IsGenericParameter) //See ECMA-335 II.7.1 (Types)
            {
                string prefix;

                if (t.DeclaringMethod == null)
                {
                    //generic type parameter
                    prefix = "!";
                }
                else
                {
                    //generic method parameter
                    prefix = "!!";
                }

                yield return new GenericSyntax(prefix + t.GenericParameterPosition.ToString());
            }
            else if (t.IsByRef)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeNameSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "&", String.Empty);
            }
            else if (t.IsArray && t.GetArrayRank() == 1)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeNameSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "[]", string.Empty);
            }
            else if (t.IsPointer)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeNameSyntax(et);

                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "*", string.Empty);
            }
            else
            {
                if (t.Equals(typeof(void)))
                    yield return new KeywordSyntax(string.Empty, "void", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(bool)))
                    yield return new KeywordSyntax(string.Empty, "bool", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(int)))
                    yield return new KeywordSyntax(string.Empty, "int32", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(uint)))
                    yield return new KeywordSyntax(string.Empty, "uint32", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(long)))
                    yield return new KeywordSyntax(string.Empty, "int64", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(ulong)))
                    yield return new KeywordSyntax(string.Empty, "uint64", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(short)))
                    yield return new KeywordSyntax(string.Empty, "int16", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(ushort)))
                    yield return new KeywordSyntax(string.Empty, "uint16", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(byte)))
                    yield return new KeywordSyntax(string.Empty, "uint8", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(sbyte)))
                    yield return new KeywordSyntax(string.Empty, "int8", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(float)))
                    yield return new KeywordSyntax(string.Empty, "float32", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(double)))
                    yield return new KeywordSyntax(string.Empty, "float64", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(string)))
                    yield return new KeywordSyntax(string.Empty, "string", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(char)))
                    yield return new KeywordSyntax(string.Empty, "char", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(object)))
                    yield return new KeywordSyntax(string.Empty, "object", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(IntPtr)))
                    yield return new KeywordSyntax(string.Empty, "native int", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(UIntPtr)))
                    yield return new KeywordSyntax(string.Empty, "native uint", string.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(TypedReference)))
                    yield return new KeywordSyntax(string.Empty, "typedref", string.Empty, KeywordKind.Other);
                else
                {
                    IEnumerable<SyntaxNode> nodes = this.GetTypeSyntax(t); //full type name syntax

                    foreach (SyntaxNode x in nodes) yield return x;

                    yield break; //GetTypeSyntax will yield modifiers
                }
            }

            //custom modifiers

            if (t is ITypeInfo)
            {
                foreach (CustomModifier m in ((ITypeInfo)t).Modifiers)
                {
                    foreach (SyntaxNode node in m.ToSyntax(this.ContainingAssembly)) yield return node;
                }
            }
        }

        /// <summary>
        /// Gets the syntax for a Type or TypeSpec construct (selects automatically)
        /// </summary>
        public IEnumerable<SyntaxNode> GetTypeSpecSyntaxAuto(Type t)
        {
            //this is used when we can omit class/valuetype prefix, such as for method calls
            Debug.Assert(t != null, "GetTypeSpecSyntaxAuto: Source type cannot be null");

            TypeSyntaxGenerator gen;

            //for generic types, the TypeSpec syntax is the same as Type
            if (t.IsGenericType) gen = new TypeSyntaxGenerator(isSpec: false, this.SkipAssemblyName);
            else gen = new TypeSyntaxGenerator(isSpec: true, this.SkipAssemblyName);

            gen.ContainingAssembly = this.ContainingAssembly;
            return gen.GetTypeSyntax(t);
        }
    }
}
