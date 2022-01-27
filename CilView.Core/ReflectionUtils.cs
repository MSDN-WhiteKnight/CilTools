/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilView.Core.Reflection;

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

        public static MethodExecutionResults ExecuteMethod(MethodBase m, IEnumerable<MethodParameter> parameters, 
            TimeSpan timeout)
        {
            MethodExecutionResults res = new MethodExecutionResults();
            List<object> invokationPars = new List<object>();

            foreach (MethodParameter par in parameters)
            {
                if(par.IsNull) { invokationPars.Add(null); continue; }

                object parValue = Convert.ChangeType(par.Value, par.ParamType);
                invokationPars.Add(parValue);
            }

            Stopwatch sw = new Stopwatch();
            object retValue = null;
            sw.Start();

            try
            {
                retValue = m.Invoke(null, invokationPars.ToArray());
            }
            catch (Exception ex)
            {
                res.ExceptionObject = ex;
            }

            sw.Stop();
            res.ReturnValue = retValue;
            res.Duration = sw.Elapsed;

            return res;
        }

        public static MethodParameter[] GetMethodParameters(MethodBase m)
        {
            ParameterInfo[] pars = m.GetParameters();
            MethodParameter[] ret = new MethodParameter[pars.Length];

            for (int i = 0; i < pars.Length; i++)
            {
                MethodParameter mp = new MethodParameter(pars[i].Name, pars[i].ParameterType, string.Empty);
                ret[i] = mp;
            }

            return ret;
        }
    }
}
