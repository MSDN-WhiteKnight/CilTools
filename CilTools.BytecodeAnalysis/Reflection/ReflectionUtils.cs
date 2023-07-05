/* CilTools.BytecodeAnalysis library 
* Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
* License: BSD 2.0 */
using System;
using System.Collections.Generic;
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

        public static BindingFlags AllDeclared
        {
            get 
            { 
                return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | 
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly; 
            }
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
        
        /// <summary>
        /// Gets a special type name for types that are represented by keywords in IL. Returns <c>null</c> if the type 
        /// is not a special type.
        /// </summary>
        internal static string TryGetSpecialTypeName(Type t)
        {
            if (t is TypeSpec)
            {
                //for types from signatures, try to detect from element type
                TypeSpec ts = (TypeSpec)t;

                switch (ts.ElementType)
                {
                    case ElementType.Boolean: return "bool";
                    case ElementType.Void:    return "void";
                    case ElementType.I1:      return "int8";
                    case ElementType.U1:      return "uint8";
                    case ElementType.I2:      return "int16";
                    case ElementType.U2:      return "uint16";
                    case ElementType.I4:      return "int32";
                    case ElementType.U4:      return "uint32";
                    case ElementType.I8:      return "int64";
                    case ElementType.U8:      return "uint64";
                    case ElementType.I:       return "native int";
                    case ElementType.U:       return "native uint";
                    case ElementType.R4:      return "float32";
                    case ElementType.R8:      return "float64";
                    case ElementType.String:  return "string";
                    case ElementType.Char:    return "char";
                    case ElementType.Object:  return "object";
                    case ElementType.TypedByRef: return "typedref";
                }
            }

            //compare with runtime types from current CoreLib
            if (t.Equals(typeof(void))) return "void";
            else if (t.Equals(typeof(bool))) return "bool";
            else if (t.Equals(typeof(int))) return "int32";
            else if (t.Equals(typeof(uint))) return "uint32";
            else if (t.Equals(typeof(long))) return "int64";
            else if (t.Equals(typeof(ulong))) return "uint64";
            else if (t.Equals(typeof(short))) return "int16";
            else if (t.Equals(typeof(ushort))) return "uint16";
            else if (t.Equals(typeof(byte))) return "uint8";
            else if (t.Equals(typeof(sbyte))) return "int8";
            else if (t.Equals(typeof(float))) return "float32";
            else if (t.Equals(typeof(double))) return "float64";
            else if (t.Equals(typeof(string))) return "string";
            else if (t.Equals(typeof(char))) return "char";
            else if (t.Equals(typeof(object))) return "object";
            else if (t.Equals(typeof(IntPtr))) return "native int";
            else if (t.Equals(typeof(UIntPtr))) return "native uint";
            else if (t.Equals(typeof(TypedReference))) return "typedref";
            else return null;
        }
    }
}
