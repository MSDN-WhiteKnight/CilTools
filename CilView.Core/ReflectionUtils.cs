/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilView.Core
{
    public static class ReflectionUtils
    {
        public static MethodBase GetRuntimeMethod(MethodBase mi)
        {
            if (!mi.IsStatic)
            {
                throw new NotSupportedException("Instance methods are not supported");
            }

            if (mi.IsConstructor)
            {
                throw new NotSupportedException("Constructors are not supported");
            }

            string methodName = mi.Name;
            ParameterInfo[] pars = mi.GetParameters();
            Type[] args = new Type[pars.Length];

            for (int i = 0; i < pars.Length; i++)
            {
                args[i] = pars[i].ParameterType;
            }

            string typeName = string.Empty;
            string assemblyLocation = string.Empty;

            if (mi.DeclaringType != null)
            {
                typeName = mi.DeclaringType.FullName;

                if(mi.DeclaringType.Assembly!=null) assemblyLocation = mi.DeclaringType.Assembly.Location;
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new Exception("Type name not found");
            }

            if (string.IsNullOrEmpty(assemblyLocation))
            {
                throw new Exception("Assembly location not found");
            }

            if (string.Equals(typeName, "<Module>", StringComparison.Ordinal))
            {
                throw new NotSupportedException("Global functions are not supported");
            }

            Assembly ass = Assembly.LoadFrom(assemblyLocation);
            Type t = ass.GetType(typeName);
            MethodInfo ret = t.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, args, null);
            return ret;
        }        
    }
}
