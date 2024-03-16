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

        public IEnumerable<SyntaxNode> GetDefinedTypeSyntax(Type t)
        {
            // Converts reflection Type object to CIL assembler syntax for TypeReference or TypeDefinition
            // (ECMA-335 II.7.3 References to user-defined types)

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
                    IEnumerable<SyntaxNode> nodes = this.GetDefinedTypeSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "&", string.Empty);
            }
            else if (t.IsArray && t.GetArrayRank() == 1)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    //arrays are always in signature
                    IEnumerable<SyntaxNode> nodes = this.GetSignatureTypeSyntax(et);

                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "[]", string.Empty);
            }
            else if (t.IsPointer)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    //pointers are always in signature
                    IEnumerable<SyntaxNode> nodes = this.GetSignatureTypeSyntax(et);
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
                    if (t.IsValueType) yield return new KeywordSyntax("valuetype", " ");
                    else yield return new KeywordSyntax("class", " ");
                }

                if (!this.SkipAssemblyName && !ReflectionFacts.IsTypeInAssembly(t, this.ContainingAssembly))
                {
                    Assembly ass = t.Assembly;

                    if (ass != null)
                    {
                        yield return new PunctuationSyntax(string.Empty, "[", string.Empty);

                        yield return new IdentifierSyntax(string.Empty, ass.GetName().Name, string.Empty, 
                            IdentifierKind.Other, ass);

                        yield return new PunctuationSyntax(string.Empty, "]", string.Empty);
                    }
                }

                //type name itself

                if (!string.IsNullOrEmpty(t.Namespace))
                {
                    yield return new IdentifierSyntax(string.Empty, t.Namespace, string.Empty, IdentifierKind.Member, null);
                    yield return new PunctuationSyntax(string.Empty, ".", string.Empty);
                }

                if (t.IsNested && t.DeclaringType != null)
                {
                    yield return new IdentifierSyntax(string.Empty, t.DeclaringType.Name, string.Empty, 
                        IdentifierKind.Member, t.DeclaringType);

                    yield return new PunctuationSyntax(string.Empty, "/", string.Empty);
                }

                yield return new IdentifierSyntax(string.Empty, t.Name, string.Empty, IdentifierKind.Member, t);

                if (t.IsGenericType && !this.IsTypeSpec)
                {
                    yield return new PunctuationSyntax(string.Empty, "<", string.Empty);

                    Type[] args = t.GetGenericArguments();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i >= 1) yield return new PunctuationSyntax(string.Empty, ",", " ");

                        IEnumerable<SyntaxNode> nodes = this.GetSignatureTypeSyntax(args[i]);

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

        public IEnumerable<SyntaxNode> GetSignatureTypeSyntax(Type t)
        {
            // Converts reflection Type object to CIL Assembler syntax for a type in signature
            // See ECMA-335 II.7.1 (Types)

            if (t == null) yield break;

            if (t.IsGenericParameter) 
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
                    IEnumerable<SyntaxNode> nodes = this.GetSignatureTypeSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "&", String.Empty);
            }
            else if (t.IsArray && t.GetArrayRank() == 1)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetSignatureTypeSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "[]", string.Empty);
            }
            else if (t.IsPointer)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = this.GetSignatureTypeSyntax(et);

                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(string.Empty, "*", string.Empty);
            }
            else
            {
                string specialTypeName = ReflectionUtils.TryGetSpecialTypeName(t);
                Debug.Assert(specialTypeName != string.Empty);
                
                if (specialTypeName != null)
                {
                    //type keyword for special types
                    yield return new KeywordSyntax(specialTypeName, string.Empty);
                }
                else
                {
                    IEnumerable<SyntaxNode> nodes = this.GetDefinedTypeSyntax(t); //full type name syntax

                    foreach (SyntaxNode x in nodes) yield return x;

                    yield break; //GetDefinedTypeSyntax will yield modifiers
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
            return gen.GetDefinedTypeSyntax(t);
        }
    }
}
