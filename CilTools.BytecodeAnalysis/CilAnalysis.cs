/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using CilTools.Reflection;
using CilTools.Syntax;
using System.IO;

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
            if (t == null) throw new ArgumentNullException("t","Source type cannot be null");

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);

            foreach (SyntaxNode node in CilAnalysis.GetTypeNameSyntax(t)) node.ToText(wr);

            wr.Flush();
            return sb.ToString();
        }

        internal static IEnumerable<SyntaxNode> GetTypeNameSyntax(Type t)
        {
            if(t==null) yield break;

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
                    IEnumerable<SyntaxNode> nodes = GetTypeNameSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "&", String.Empty);
            }
            else if (t.IsArray && t.GetArrayRank() == 1)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = GetTypeNameSyntax(et);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "[]", String.Empty);
            }
            else if (t.IsPointer)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = GetTypeNameSyntax(et);

                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "*", String.Empty);
            }
            else
            {
                if (t.Equals(typeof(void)))
                    yield return new KeywordSyntax(String.Empty, "void", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(bool)))
                    yield return new KeywordSyntax(String.Empty, "bool", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(int)))
                    yield return new KeywordSyntax(String.Empty, "int32", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(uint)))
                    yield return new KeywordSyntax(String.Empty, "uint32", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(long)))
                    yield return new KeywordSyntax(String.Empty, "int64", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(ulong)))
                    yield return new KeywordSyntax(String.Empty, "uint64", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(short)))
                    yield return new KeywordSyntax(String.Empty, "int16", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(ushort)))
                    yield return new KeywordSyntax(String.Empty, "uint16", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(byte)))
                    yield return new KeywordSyntax(String.Empty, "uint8", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(sbyte)))
                    yield return new KeywordSyntax(String.Empty, "int8", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(float)))
                    yield return new KeywordSyntax(String.Empty, "float32", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(double)))
                    yield return new KeywordSyntax(String.Empty, "float64", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(string)))
                    yield return new KeywordSyntax(String.Empty, "string", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(char)))
                    yield return new KeywordSyntax(String.Empty, "char", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(object)))
                    yield return new KeywordSyntax(String.Empty, "object", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(IntPtr)))
                    yield return new KeywordSyntax(String.Empty, "native int", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(UIntPtr)))
                    yield return new KeywordSyntax(String.Empty, "native uint", String.Empty, KeywordKind.Other);
                else if (t.Equals(typeof(System.TypedReference)))
                    yield return new KeywordSyntax(String.Empty, "typedref", String.Empty, KeywordKind.Other);
                else
                {
                    IEnumerable<SyntaxNode> nodes = GetTypeFullNameSyntax(t);

                    foreach (SyntaxNode x in nodes) yield return x;

                    yield break; //GetTypeFullNameSyntax will yield modifiers
                }
            }

            //custom modifiers

            if (t is ITypeInfo)
            {
                foreach (CustomModifier m in ((ITypeInfo)t).Modifiers)
                {
                    foreach (SyntaxNode node in m.ToSyntax()) yield return node;
                }
            }
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

            foreach (SyntaxNode node in CilAnalysis.GetTypeSyntax(t,false)) node.ToText(wr);

            wr.Flush();
            return sb.ToString();
        }

        internal static IEnumerable<SyntaxNode> GetTypeFullNameSyntax(Type t)
        {
            return CilAnalysis.GetTypeSyntax(t, false);
        }

        internal static IEnumerable<SyntaxNode> GetTypeSyntax(Type t,bool isspec)
        {
            //converts reflection Type object to Type or TypeSpec CIL assembler syntax
            //(ECMA-335 II.7.1 Types)

            if (t.IsGenericParameter)
            {
                string prefix;

                if (t.DeclaringMethod == null) prefix = "!";
                else prefix = "!!";

                if (String.IsNullOrEmpty(t.Name))
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
                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeSyntax(et, false);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "&", String.Empty);
            }
            else if (t.IsArray && t.GetArrayRank() == 1)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeSyntax(et, false);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "[]", String.Empty);
            }
            else if (t.IsPointer)
            {
                Type et = t.GetElementType();

                if (et != null)
                {
                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeSyntax(et, false);
                    foreach (SyntaxNode x in nodes) yield return x;
                }

                yield return new PunctuationSyntax(String.Empty, "*", String.Empty);
            }
            else if (t is ITypeInfo && ((ITypeInfo)t).IsFunctionPointer)
            {
                yield return new KeywordSyntax(String.Empty, "method", " ", KeywordKind.Other);
                IEnumerable<SyntaxNode> nodes = ((ITypeInfo)t).TargetSignature.ToSyntax(true);
                foreach (SyntaxNode x in nodes) yield return x;
            }
            else
            {
                if (!isspec) //for TypeSpec, we omit class/valuetype keyword
                {
                    if (t.IsValueType) yield return new KeywordSyntax(String.Empty, "valuetype", " ", KeywordKind.Other);
                    else yield return new KeywordSyntax(String.Empty, "class", " ", KeywordKind.Other);
                }

                Assembly ass = t.Assembly;

                if (ass != null)
                {
                    yield return new PunctuationSyntax(String.Empty, "[", String.Empty);
                    yield return new IdentifierSyntax(String.Empty, ass.GetName().Name, String.Empty, false, ass);
                    yield return new PunctuationSyntax(String.Empty, "]", String.Empty);
                }

                StringBuilder sb = new StringBuilder();

                if (!String.IsNullOrEmpty(t.Namespace))
                {
                    sb.Append(t.Namespace);
                    sb.Append('.');
                }

                if (t.IsNested && t.DeclaringType != null)
                {
                    sb.Append(t.DeclaringType.Name);
                    sb.Append('/');
                }

                sb.Append(t.Name);
                yield return new IdentifierSyntax(String.Empty, sb.ToString(), String.Empty, true,t);

                if (t.IsGenericType)
                {
                    yield return new PunctuationSyntax(String.Empty, "<", String.Empty);

                    Type[] args = t.GetGenericArguments();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i >= 1) yield return new PunctuationSyntax(String.Empty, ",", " ");

                        IEnumerable<SyntaxNode> nodes = GetTypeNameSyntax(args[i]);

                        foreach (SyntaxNode node in nodes) yield return node;
                    }

                    yield return new PunctuationSyntax(String.Empty, ">", String.Empty);
                }
            }

            //custom modifiers

            if (t is ITypeInfo)
            {
                foreach (CustomModifier m in ((ITypeInfo)t).Modifiers)
                {
                    foreach (SyntaxNode node in m.ToSyntax()) yield return node;
                }
            }
        }

        /// <summary>
        /// Escapes special characters in the specified string, using rules similar to what are applied to C# string literals
        /// </summary>
        /// <param name="str">The string to escape</param>
        /// <returns>The escaped string</returns>
        public static string EscapeString(string str)
        {
            if (String.IsNullOrEmpty(str)) return str;

            StringBuilder sb = new StringBuilder(str.Length * 2);

            foreach (char c in str)
            {
                switch (c)
                {
                    case '\0': sb.Append("\\0"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\a': sb.Append("\\a"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '"': sb.Append("\\\""); break;

                    default:
                        if (Char.IsControl(c)) sb.Append("\\u" + ((ushort)c).ToString("X").PadLeft(4, '0'));
                        else sb.Append(c);
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

            foreach (SyntaxNode node in CilAnalysis.GetTypeSpecSyntax(t)) node.ToText(wr);

            wr.Flush();
            return sb.ToString();
        }

        internal static IEnumerable<SyntaxNode> GetTypeSpecSyntax(Type t)
        {
            //this is used when we can omit class/valuetype prefix, such as for method calls
            Debug.Assert(t != null, "GetTypeSpecSyntax: Source type cannot be null");

            //for generic types, the TypeSpec syntax is the same as Type
            if(t.IsGenericType) return CilAnalysis.GetTypeSyntax(t, false);
            else return CilAnalysis.GetTypeSyntax(t, true);
        }

        internal static string MethodToString(MethodBase m)
        {
            //gets the CIL code of the reference to the specified method
            StringBuilder sb = new StringBuilder(200);
            StringWriter wr = new StringWriter(sb);
            SyntaxNode node = CilAnalysis.GetMethodRefSyntax(m, false);
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
            byte[] bytes = BitConverter.GetBytes(token);
            return bytes[0] == 0x01 && bytes[3] == 0x02;
        }

        internal static MemberRefSyntax GetMethodRefSyntax(MethodBase m,bool inlineTok)
        {
            List<SyntaxNode> children = new List<SyntaxNode>(50);
            Type t = m.DeclaringType;
            ParameterInfo[] pars = m.GetParameters();

            MethodInfo mi = m as MethodInfo;
            IEnumerable<SyntaxNode> rt;

            if (inlineTok) 
            {
                //for ldtoken instruction the method reference is preceded by "method" keyword
                children.Add(new KeywordSyntax("", "method", " ", KeywordKind.Other));
            }

            //append return type
            if (mi != null)
            {
                //standard reflection implementation: return type exposed via MethodInfo
                rt = CilAnalysis.GetTypeNameSyntax(mi.ReturnType);
            }
            else if (m is CustomMethod)
            {
                //CilTools reflection implementation: return type exposed via CustomMethod
                Type tReturn = ((CustomMethod)m).ReturnType;

                if (tReturn != null) rt = CilAnalysis.GetTypeNameSyntax(tReturn);
                else rt = new SyntaxNode[] { new KeywordSyntax("", "void", "", KeywordKind.Other) };
            }
            else
            {
                //we append return type here even for constructors
                rt = new SyntaxNode[] { new KeywordSyntax("", "void", "", KeywordKind.Other) };
            }

            if (!m.IsStatic) children.Add(new KeywordSyntax("", "instance", " ", KeywordKind.Other));

            foreach (SyntaxNode node in rt) children.Add(node);

            children.Add(new GenericSyntax(" "));

            //append declaring type
            if (t != null && !IsModuleType(t))
            {
                IEnumerable<SyntaxNode> syntax = CilAnalysis.GetTypeSpecSyntax(t);

                foreach (SyntaxNode node in syntax) children.Add(node);
                
                children.Add(new PunctuationSyntax("","::",""));
            }

            //append name
            children.Add(new IdentifierSyntax("",m.Name,"",true,m));

            if (m.IsGenericMethod)
            {
                children.Add(new PunctuationSyntax("","<",""));

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) children.Add(new PunctuationSyntax("",","," "));

                    IEnumerable<SyntaxNode> syntax = CilAnalysis.GetTypeNameSyntax(args[i]);

                    foreach(SyntaxNode node in syntax) children.Add(node);
                }

                children.Add(new PunctuationSyntax("",">",""));
            }

            children.Add(new PunctuationSyntax("","(",""));

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) children.Add(new PunctuationSyntax("",","," "));

                 IEnumerable<SyntaxNode> syntax = CilAnalysis.GetTypeNameSyntax(pars[i].ParameterType);

                 foreach(SyntaxNode node in syntax) children.Add(node);
            }

            children.Add(new PunctuationSyntax("",")",""));

            return new MemberRefSyntax(children.ToArray(),m);
        }

        /// <summary>
        /// Returns specified method CIL code as string
        /// </summary>
        /// <param name="m">Method for which to retrieve CIL</param>
        /// <remarks>The CIL code returned by this API is intended mainly for reading, not compiling. It is not guaranteed to be a valid input for CIL assembler.</remarks>
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

                    if (ass!=null && mb.DeclaringType !=null &&
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
        public static IEnumerable<MemberInfo> GetReferencedMembers(Type t,MemberCriteria flags)
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
                    var coll = GetReferencedMembers(m,flags);

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
        public static IEnumerable<MemberInfo> GetReferencedMembers(Assembly ass,MemberCriteria flags)
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
                        string error = "";
                        Diagnostics.OnError(item, new CilErrorEventArgs(ex, error));
                    }
                }
            }
            return results;
        }
    }

    /// <summary>
    /// Represents bitwise flags that define what kinds of members are requested 
    /// </summary>
    /// <remarks>External members are members defined in different assembly then the method which references them, not to be confused with `external` keyword in C#. Internal members are members defined in the same assembly as referencing method, similarly, not to be confused with `internal` keyword or `InternalCall` attribute.  If you specify a combination of flags that does not match anything (i.e., if you define neither external nor internal members, or neither methods nor fields) when requesting referenced members, empty collection is returned.</remarks>
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
