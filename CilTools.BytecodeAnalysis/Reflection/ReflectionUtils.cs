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

        public static bool IsExpectedException(Exception ex)
        {
            //check expected exception types that can pop up due to reflection APIs being not 
            //implemented on custom subclasses 

            return ex is NotImplementedException || ex is NotSupportedException ||
                ex is InvalidOperationException;
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
    }
}
