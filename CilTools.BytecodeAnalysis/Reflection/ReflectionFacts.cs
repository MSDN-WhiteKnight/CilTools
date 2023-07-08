/* CIL Tools 
* Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
* License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    /// <summary>
    /// Provides static methods that return information about reflection objects as boolean values
    /// </summary>
    internal static class ReflectionFacts
    {
        static bool StrEquals(string left, string right)
        {
            //metadata strings are compared using non-linguistic comparison
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the value indicating whether the specified method is an entry point of its containing assembly. For 
        /// methods in non-executable assemblies that have no entry point, always returns <c>false</c>
        /// </summary>
        internal static bool IsEntryPoint(MethodBase m)
        {
            try
            {
                if (m.DeclaringType == null) return false;
                if (m.DeclaringType.Assembly == null) return false;

                MethodBase entryPoint = m.DeclaringType.Assembly.EntryPoint;

                if (entryPoint == null) return false;

                return m.MetadataToken == entryPoint.MetadataToken;
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex)) return false;
                else throw;
            }
        }

        /// <summary>
        /// Gets the value indicating whether the specified method has no method body by design
        /// </summary>
        /// <remarks>
        /// Method has no method body by design if it is abstract, PInvoke-implemented or runtime-implemented
        /// </remarks>
        internal static bool IsMethodWithoutBody(MethodBase m)
        {
            MethodImplAttributes implattr = 0;

            try { implattr = m.GetMethodImplementationFlags(); }
            catch (NotImplementedException) { }

            if (m.IsAbstract ||
                (m.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl ||
                (implattr & MethodImplAttributes.InternalCall) == MethodImplAttributes.InternalCall ||
                (implattr & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime
                )
            {
                //If method is abstract, PInvoke or provided by runtime,
                //it does not have CIL method body by design                
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Gets the value indicating whether the specified method is static. Avoids resolving external method 
        /// references when it's possible.
        /// </summary>
        internal static bool IsMethodStatic(MethodBase method)
        {
            object val = ReflectionProperties.Get(method, ReflectionProperties.IsStatic);

            if (val is bool) return (bool)val;
            else return method.IsStatic;
        }

        internal static bool IsEnumType(Type t)
        {
            Type baseType = null;

            try
            {
                baseType = t.BaseType;
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    Diagnostics.OnError(t, new CilErrorEventArgs(ex, "Failed to get base type"));
                }
                else throw;
            }

            if (baseType == null) return false;

            return string.Equals(baseType.FullName, "System.Enum", StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Gets the value indicating whether the specified type is defined in the specified assembly
        /// </summary>
        internal static bool IsTypeInAssembly(Type t, Assembly ass)
        {
            if (t == null || ass == null) return false;

            Assembly typeAssembly = t.Assembly;

            if (typeAssembly == null) return false;

            string name1 = typeAssembly.GetName().Name;
            string name2 = ass.GetName().Name;
            return string.Equals(name1, name2, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the value indicating whether the specified type is a special type that holds global members
        /// </summary>
        /// <remarks>
        /// The special <c>Module</c> type holds global (module-level) fields and functions
        /// </remarks>
        internal static bool IsModuleType(Type t)
        {
            int token = 0;

            try { token = t.MetadataToken; }
            catch (InvalidOperationException) { return false; }

            //First row in TypeDef table represents dummy type for module-level decls
            //(ECMA-335 II.22.37  TypeDef : 0x02 )
            return token == 0x02000001;
        }

        /// <summary>
        /// Gets the value indicating whether the specified type represents a custom attribute type that has some special 
        /// representaion in CIL (that is, represented as some special syntax and not as regular <c>.custom</c> directive). 
        /// </summary>
        internal static bool IsBuiltInAttribute(Type t)
        {
            if (StrEquals(t.FullName, "System.Runtime.InteropServices.OptionalAttribute"))
            {
                return true; //translated to [opt]
            }
            else if (StrEquals(t.FullName, "System.SerializableAttribute"))
            {
                //translated to 'serializable' built-in type attribute
                return true;
            }
            else if (StrEquals(t.FullName, "System.Runtime.InteropServices.ComImportAttribute"))
            {
                //translated to 'import' built-in type attribute
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the value indicating whether the specified type declares at least one public instance field. Inherited fields 
        /// does not count.
        /// </summary>
        internal static bool HasPublicInstanceFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0;
        }

        /// <summary>
        /// Gets the value indicating whether the specified type declares at least one public instance property with a 
        /// <c>set</c> method
        /// </summary>
        internal static bool HasPublicWritableInstanceProperties(Type t)
        {
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (props.Length == 0) return false;

            return props.Where((x) => x.CanWrite).Count() > 0;
        }
    }
}
