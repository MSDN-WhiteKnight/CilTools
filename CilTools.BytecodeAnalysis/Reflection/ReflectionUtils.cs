/* CilTools.BytecodeAnalysis library 
* Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
* License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    internal static class ReflectionUtils
    {
        public static BindingFlags InstanceMembers 
        {
            get { return BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic; }
        }

        public static BindingFlags AllMembers
        {
            get { return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic; }
        }

        public static bool IsExpectedException(Exception ex)
        {
            //check expected exception types that can pop up due to reflection APIs being not 
            //implemented on custom subclasses 

            return ex is NotImplementedException || ex is NotSupportedException ||
                ex is InvalidOperationException;
        }

        public static string GetErrorShortString(Exception ex)
        {
            return ex.GetType().ToString() + ": " + ex.Message;
        }

        public static bool IsEntryPoint(MethodBase m)
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
                if (IsExpectedException(ex)) return false;
                else throw;
            }
        }

        public static bool IsMethodWithoutBody(MethodBase m)
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

        public static MethodInfo GetExplicitlyImplementedMethod(MethodBase m)
        {
            if (m is ConstructorInfo) return null;
            if (m.DeclaringType == null) return null;
            if (m.IsStatic) return null;

            // Try to use special reflection property to avoid GetInterfaceMap that resolves
            // external references
            object val = ReflectionProperties.Get(m, ReflectionProperties.ExplicitlyImplementedMethods);

            if (val != null && val is MethodInfo[])
            {
                MethodInfo[] eim = (MethodInfo[])val;

                if (eim.Length == 0) return null;
                else return eim[0];
            }

            // If it is not possible, determine explicitly implemented method based on the interface map
            Type t = m.DeclaringType;
            Type[] ifTypes = t.GetInterfaces();

            for (int i = 0; i < ifTypes.Length; i++)
            {
                InterfaceMapping map = t.GetInterfaceMap(ifTypes[i]);

                for (int j = 0; j < map.TargetMethods.Length; j++)
                {
                    if (map.TargetMethods[j].MetadataToken != m.MetadataToken) continue;

                    //method implements interface method

                    if (string.Equals(m.Name, map.InterfaceMethods[j].Name))
                    {
                        continue; //implements implicitly
                    }
                    else
                    {
                        return map.InterfaceMethods[j];
                    }
                }
            }

            return null;
        }

        public static ParameterInfo[] GetMethodParams(MethodBase method, RefResolutionMode refResolutionMode)
        {
            if (method is IParamsProvider)
            {
                return ((IParamsProvider)method).GetParameters(refResolutionMode);
            }
            else
            {
                return method.GetParameters();
            }
        }

        /// <summary>
        /// Gets the value indicating whether the specified method is static. Avoids resolving external method 
        /// references when it's possible.
        /// </summary>
        public static bool IsMethodStatic(MethodBase method)
        {
            object val = ReflectionProperties.Get(method, ReflectionProperties.IsStatic);

            if (val is bool) return (bool)val;
            else return method.IsStatic;
        }

        public static bool IsEnumType(Type t)
        {
            Type baseType = null;

            try
            {
                baseType = t.BaseType;
            }
            catch (Exception ex)
            {
                if (IsExpectedException(ex))
                {
                    Diagnostics.OnError(t, new CilErrorEventArgs(ex, "Failed to get base type"));
                }
                else throw;
            }

            if (baseType == null) return false;

            return string.Equals(baseType.FullName, "System.Enum", StringComparison.InvariantCulture);
        }

        public static bool IsTypeInAssembly(Type t, Assembly ass)
        {
            if (t == null || ass == null) return false;

            Assembly typeAssembly = t.Assembly;

            if (typeAssembly == null) return false;

            string name1 = typeAssembly.GetName().Name;
            string name2 = ass.GetName().Name;
            return string.Equals(name1, name2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static Assembly GetContainingAssembly(MemberInfo mi)
        {
            if (mi == null) return null;

            if (mi is Type)
            {
                Type t = (Type)mi;
                return t.Assembly;
            }
            else
            {
                Type t = mi.DeclaringType;

                if (t == null) return null;
                else return t.Assembly;
            }
        }

        public static Assembly GetProviderAssembly(ICustomAttributeProvider provider)
        {
            if (provider is MemberInfo)
            {
                return GetContainingAssembly((MemberInfo)provider);
            }
            else if (provider is Assembly)
            {
                return (Assembly)provider;
            }
            else if (provider is Module)
            {
                return ((Module)provider).Assembly;
            }
            else return null;
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

        internal static bool HasPublicInstanceFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0;
        }

        internal static bool HasPublicWritableInstanceProperties(Type t)
        {
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (props.Length == 0) return false;
            
            return props.Where((x) => x.CanWrite).Count() > 0;
        }
    }
}
