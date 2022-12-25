/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Syntax.Generation;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Provides static methods that assist in parsing and analysing CIL bytecode
    /// </summary>
    public static class CilAnalysis
    {
        /// <summary>
        /// Gets the name of .NET type in CIL notation
        /// </summary>
        /// <param name="t">Type for which name is requested</param>
        /// <exception cref="System.ArgumentNullException">t is null</exception>
        /// <remarks>Returns short type name, such as <c>int32</c>, if it exists. Otherwise returns full name.</remarks>
        /// <returns>Short of full type name</returns>
        public static string GetTypeName(Type t)
        {
            if (t == null) throw new ArgumentNullException("t", "Source type cannot be null");

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);
            TypeSyntaxGenerator gen = new TypeSyntaxGenerator();

            foreach (SyntaxNode node in gen.GetTypeNameSyntax(t)) node.ToText(wr);

            wr.Flush();
            return sb.ToString();
        }

        internal static IEnumerable<SyntaxNode> GetTypeNameSyntax(Type t, Assembly containingAssembly)
        {
            TypeSyntaxGenerator gen = new TypeSyntaxGenerator(containingAssembly);
            return gen.GetTypeNameSyntax(t);
        }
        
        /// <summary>
        /// Gets the full name of .NET type in CIL notation
        /// </summary>
        /// <param name="t">Type for which name is requested</param>
        /// <exception cref="System.ArgumentNullException">t is null</exception>
        /// <remarks>Returns fully qualified name, such as <c>class [mscorlib]System.String</c></remarks>
        /// <returns>Full type name</returns>
        public static string GetTypeFullName(Type t)
        {
            if (t == null) throw new ArgumentNullException("t", "Source type cannot be null");

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);
            TypeSyntaxGenerator gen = new TypeSyntaxGenerator();
            IEnumerable<SyntaxNode> nodes = gen.GetTypeSyntax(t);

            foreach (SyntaxNode node in nodes)
            {
                node.ToText(wr);
            }

            wr.Flush();
            return sb.ToString();
        }

        internal static IEnumerable<SyntaxNode> GetTypeSyntax(Type t, bool isspec, bool skipAssembly, Assembly containingAssembly)
        {
            TypeSyntaxGenerator gen = new TypeSyntaxGenerator(isspec, skipAssembly);
            gen.ContainingAssembly = containingAssembly;
            return gen.GetTypeSyntax(t);
        }
        
        static string CharToOctal(char c)
        {
            if ((uint)c <= byte.MaxValue)
            {
                return "\\" + Convert.ToString((uint)c, 8).PadLeft(3, '0');
            }
            else
            {
                byte[] bytes = BitConverter.GetBytes(c);
                return "\\" + Convert.ToString(bytes[0], 8).PadLeft(3, '0') +
                    "\\" + Convert.ToString(bytes[1], 8).PadLeft(3, '0');
            }
        }

        /// <summary>
        /// Escapes special characters in the specified string, preparing it to be used as CIL assembler string literal
        /// </summary>
        /// <param name="str">The string to escape</param>
        /// <returns>The escaped string</returns>
        /// <remarks>
        /// See ECMA-335 II.5.2 for string literal escaping rules. 
        /// In CIL Tools 2.3 and earlier versions, this method used string escaping rules for C# string literals.
        /// </remarks>
        public static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            StringBuilder sb = new StringBuilder(str.Length * 2);

            foreach (char c in str)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append(CharToOctal(c)); break;

                    default:
                        if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ')
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append(CharToOctal(c));
                        }
                        break;
                }
            }

            return sb.ToString();
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

        internal static string MethodRefToString(MethodBase m, Assembly containingAssembly)
        {
            //gets the CIL code of the reference to the specified method
            StringBuilder sb = new StringBuilder(200);
            StringWriter wr = new StringWriter(sb);

            SyntaxNode node = GetMethodRefSyntax(m, inlineTok: false, forceTypeSpec: false, skipAssembly: false,
                containingAssembly);

            node.ToText(wr);
            wr.Flush();
            return sb.ToString();
        }

        internal static bool IsModuleType(Type t)
        {
            int token = 0;

            try { token = t.MetadataToken; }
            catch (InvalidOperationException) { return false; }

            //First row in TypeDef table represents dummy type for module-level decls
            //(ECMA-335 II.22.37  TypeDef : 0x02 )
            return token == 0x02000001;
        }

        internal static MemberRefSyntax GetMethodRefSyntax(MethodBase m, bool inlineTok, bool forceTypeSpec,
            bool skipAssembly, Assembly containingAssembly)
        {
            List<SyntaxNode> children = new List<SyntaxNode>(50);
            Type t = m.DeclaringType;
            int sentinelPos = -1;

            //we only query parameter types so no need to resolve external references
            ParameterInfo[] pars = ReflectionUtils.GetMethodParams(m, RefResolutionMode.NoResolve);

            MethodInfo mi = m as MethodInfo;
            IEnumerable<SyntaxNode> rt;
            TypeSyntaxGenerator gen = new TypeSyntaxGenerator();
            gen.ContainingAssembly = containingAssembly;

            if (inlineTok)
            {
                //for ldtoken instruction the method reference is preceded by "method" keyword
                children.Add(new KeywordSyntax(string.Empty, "method", " ", KeywordKind.Other));
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
                else rt = new SyntaxNode[] { new KeywordSyntax(string.Empty, "void", string.Empty, KeywordKind.Other) };
            }
            else if (m is CustomMethod)
            {
                //CilTools reflection implementation: return type exposed via CustomMethod
                Type tReturn = ((CustomMethod)m).ReturnType;

                if (tReturn != null) rt = gen.GetTypeNameSyntax(tReturn);
                else rt = new SyntaxNode[] { new KeywordSyntax(string.Empty, "void", string.Empty, KeywordKind.Other) };
            }
            else
            {
                //we append return type here even for constructors
                rt = new SyntaxNode[] { new KeywordSyntax(string.Empty, "void", string.Empty, KeywordKind.Other) };
            }

            if (!ReflectionUtils.IsMethodStatic(m))
            {
                children.Add(new KeywordSyntax(string.Empty, "instance", " ", KeywordKind.Other));
            }

            if (m.CallingConvention == CallingConventions.VarArgs)
            {
                children.Add(new KeywordSyntax(string.Empty, "vararg", " ", KeywordKind.Other));

                Signature sig = ReflectionProperties.Get(m, ReflectionProperties.Signature) as Signature;
                sentinelPos = sig.SentinelPosition;
            }

            foreach (SyntaxNode node in rt) children.Add(node);

            children.Add(new GenericSyntax(" "));

            //append declaring type
            if (t != null && !IsModuleType(t))
            {
                IEnumerable<SyntaxNode> syntax;

                // When method reference is used for property/event accessors, TypeSpec syntax is forced, so we don't 
                // call into Type.IsValueType needlessly 
                // (prevents issues like https://github.com/MSDN-WhiteKnight/CilTools/issues/140).
                // See ECMA-335 II.17 - Defining properties.
                TypeSyntaxGenerator dtGen = new TypeSyntaxGenerator();
                dtGen.SkipAssemblyName = skipAssembly;
                dtGen.ContainingAssembly = containingAssembly;

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

                children.Add(new PunctuationSyntax(string.Empty, "::", string.Empty));
            }

            //append name
            children.Add(new IdentifierSyntax(string.Empty, m.Name, string.Empty, true, m));

            if (m.IsGenericMethod)
            {
                children.Add(new PunctuationSyntax(string.Empty, "<", string.Empty));

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) children.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    IEnumerable<SyntaxNode> syntax = gen.GetTypeNameSyntax(args[i]);

                    foreach (SyntaxNode node in syntax) children.Add(node);
                }

                children.Add(new PunctuationSyntax(string.Empty, ">", string.Empty));
            }

            children.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

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

            children.Add(new PunctuationSyntax(string.Empty, ")", string.Empty));

            return new MemberRefSyntax(children.ToArray(), m);
        }

        /// <summary>
        /// Returns specified method CIL code as string
        /// </summary>
        /// <param name="m">Method for which to retrieve CIL</param>
        /// <returns>CIL code string</returns>
        public static string MethodToText(MethodBase m)
        {
            if (m == null) return "(null)";

            return CilGraph.Create(m).ToText();
        }

        /// <summary>
        /// Gets all methods that are referenced by the specified method
        /// </summary>
        /// <param name="mb">Method for which to retrieve referenced methods</param>
        /// <exception cref="System.ArgumentNullException">Source method is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in source method's body. For example, if the source method calls `Foo` method or creates delegate pointing to `Foo`, `Foo` is referenced by the source method.</remarks>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(MethodBase mb)
        {
            var coll = GetReferencedMembers(mb, MemberCriteria.External | MemberCriteria.Internal |
                MemberCriteria.Methods);

            foreach (var item in coll)
            {
                if (item is MethodBase) yield return (MethodBase)item;
            }
        }

        /// <summary>
        /// Gets all members (fields or methods) referenced by specified method
        /// </summary>
        /// <param name="mb">Method for which to retrieve referenced members</param>
        /// <exception cref="System.ArgumentNullException">Source method is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in source method's body. For example, if the source method calls `Foo` method or creates delegate pointing to `Foo`, `Foo` is referenced by the source method.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(MethodBase mb)
        {
            return GetReferencedMembers(mb, MemberCriteria.External | MemberCriteria.Internal |
                MemberCriteria.Methods | MemberCriteria.Fields);
        }

        /// <summary>
        /// Gets members (fields or methods) referenced by specified method that match specified criteria
        /// </summary>
        /// <param name="mb">Method for which to retrieve referenced members</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retrieved</param>
        /// <exception cref="System.ArgumentNullException">Source method is null</exception>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Failed to retrieve method body for the method</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in source method's body. For example, if the source method calls `Foo` method or creates delegate pointing to `Foo`, `Foo` is referenced by the source method.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(MethodBase mb, MemberCriteria flags)
        {
            if (mb == null) throw new ArgumentNullException("mb", "Source method cannot be null");

            List<MemberInfo> results = new List<MemberInfo>();

            //detect combinations of flags that can't produce any results
            if ((flags & MemberCriteria.Methods) == 0 && (flags & MemberCriteria.Fields) == 0) return results;
            if ((flags & MemberCriteria.Internal) == 0 && (flags & MemberCriteria.External) == 0) return results;

            Assembly ass;
            var ins = CilReader.GetInstructions(mb);

            foreach (var instr in ins)
            {
                if (instr.ReferencedMember == null) continue;

                if ((flags & MemberCriteria.Methods) != 0 && instr.ReferencedMember is MethodBase)
                {
                    var item = instr.ReferencedMember as MethodBase;

                    //if declaring type is null, consider method external (usually the case for methods implemented in native code)
                    if (item.DeclaringType == null) ass = null;
                    else ass = item.DeclaringType.Assembly;

                    if (ass != null && mb.DeclaringType != null &&
                        ass.FullName.ToLower().Trim() == mb.DeclaringType.Assembly.FullName.ToLower().Trim())
                    {
                        //internal
                        if ((flags & MemberCriteria.Internal) != 0 && !results.Contains(item)) results.Add(item);
                    }
                    else
                    {
                        //external
                        if ((flags & MemberCriteria.External) != 0 && !results.Contains(item)) results.Add(item);
                    }
                }
                else if ((flags & MemberCriteria.Fields) != 0 && instr.ReferencedMember is FieldInfo)
                {
                    var field = instr.ReferencedMember as FieldInfo;
                    ass = field.DeclaringType.Assembly;

                    if (ass.FullName.ToLower().Trim() == mb.DeclaringType.Assembly.FullName.ToLower().Trim())
                    {
                        //internal
                        if ((flags & MemberCriteria.Internal) != 0 && !results.Contains(field)) results.Add(field);
                    }
                    else
                    {
                        //external
                        if ((flags & MemberCriteria.External) != 0 && !results.Contains(field)) results.Add(field);
                    }
                }

            }

            return results;
        }

        /// <summary>
        /// Get all methods that are referenced by the code of the specified type
        /// </summary>
        /// <param name="t">Type for which to retrieve referenced methods</param>
        /// <exception cref="System.ArgumentNullException">Source type is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the type's methods.</remarks>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(Type t)
        {
            var coll = GetReferencedMembers(t, MemberCriteria.External | MemberCriteria.Internal |
                MemberCriteria.Methods);

            foreach (var item in coll)
            {
                if (item is MethodBase) yield return (MethodBase)item;
            }
        }

        /// <summary>
        /// Gets all members referenced by the code of specified type
        /// </summary>
        /// <param name="t">Type for which to retrieve referenced members</param>
        /// <exception cref="System.ArgumentNullException">Source type is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the type's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(Type t)
        {
            return GetReferencedMembers(t, MemberCriteria.External | MemberCriteria.Internal |
                MemberCriteria.Methods | MemberCriteria.Fields);
        }

        /// <summary>
        /// Gets members referenced by the code of specified type that match specified criteria
        /// </summary>
        /// <param name="t">Type for which to retrieve referenced members</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retrieved</param>
        /// <exception cref="System.ArgumentNullException">Source type is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the type's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(Type t, MemberCriteria flags)
        {
            if (t == null) throw new ArgumentNullException("t", "Source type cannot be null");

            //process regular methods
            var methods = t.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );
            List<MemberInfo> results = new List<MemberInfo>();

            foreach (var m in methods)
            {
                try
                {
                    var coll = GetReferencedMembers(m, flags);

                    foreach (var item in coll)
                    {
                        if (!results.Contains(item)) results.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to get a list of referenced members.";
                    Diagnostics.OnError(m, new CilErrorEventArgs(ex, error));
                }
            }

            //process constructors
            var constr = t.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );

            foreach (var m in constr)
            {
                try
                {
                    var coll = GetReferencedMembers(m, flags);

                    foreach (var item in coll)
                    {
                        if (!results.Contains(item)) results.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to get a list of referenced members.";
                    Diagnostics.OnError(m, new CilErrorEventArgs(ex, error));
                }
            }

            return results;
        }

        /// <summary>
        /// Get all methods that are referenced by the code in the specified assembly
        /// </summary>
        /// <param name="ass">Assembly for which to retrieve referenced methods</param>
        /// <exception cref="System.ArgumentNullException">Source assembly is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the assembly's methods.</remarks>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(Assembly ass)
        {
            var coll = GetReferencedMembers(ass, MemberCriteria.External | MemberCriteria.Internal |
                MemberCriteria.Methods);

            foreach (var item in coll)
            {
                if (item is MethodBase) yield return (MethodBase)item;
            }
        }

        /// <summary>
        /// Gets all members referenced by the code of specified assembly
        /// </summary>
        /// <param name="ass">Assembly for which to retrieve referenced members</param>
        /// <exception cref="System.ArgumentNullException">Source assembly is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the assembly's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(Assembly ass)
        {
            return GetReferencedMembers(ass, MemberCriteria.External | MemberCriteria.Internal |
                MemberCriteria.Methods | MemberCriteria.Fields);
        }

        /// <summary>
        /// Gets members referenced by the code of specified assembly that match specified criteria
        /// </summary>
        /// <param name="ass">Assembly for which to retrieve referenced members</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retrieved</param>
        /// <exception cref="System.ArgumentNullException">Source assembly is null</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the assembly's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(Assembly ass, MemberCriteria flags)
        {
            if (ass == null) throw new ArgumentNullException("ass", "Source assembly cannot be null");

            Type[] types = ass.GetTypes();
            List<MemberInfo> results = new List<MemberInfo>();

            foreach (Type t in types)
            {
                var coll = GetReferencedMembers(t, flags);

                foreach (var item in coll)
                {
                    try
                    {
                        if (!results.Contains(item)) results.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.OnError(item, new CilErrorEventArgs(ex, string.Empty));
                    }
                }
            }
            return results;
        }
    }

    /// <summary>
    /// Represents bitwise flags that define what kinds of members are requested 
    /// </summary>
    /// <remarks>External members are members defined in different assembly then the method which references them, not to be 
    /// confused with `external` keyword in C#. Internal members are members defined in the same assembly as referencing method, 
    /// similarly, not to be confused with `internal` keyword or `InternalCall` attribute.  If you specify a combination of flags 
    /// that does not match anything (i.e., if you define neither external nor internal members, or neither methods nor fields) 
    /// when requesting referenced members, empty collection is returned.</remarks>
    [Flags]
    public enum MemberCriteria
    {
        /// <summary>
        /// Return external (not from the same assembly as containing method) members
        /// </summary>
        External = 0x01,

        /// <summary>
        /// Return internal (from the same assembly as containing method) members 
        /// </summary>
        Internal = 0x02,

        /// <summary>
        /// Return methods (including constructors)
        /// </summary>
        Methods = 0x04,

        /// <summary>
        /// Return fields
        /// </summary>
        Fields = 0x08
    }
}
