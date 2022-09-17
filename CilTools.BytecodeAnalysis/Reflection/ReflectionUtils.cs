/* CilTools.BytecodeAnalysis library 
* Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
* License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
    }
}
