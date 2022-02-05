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
    public static class MethodRunner
    {
        public static bool CanExecute(MethodBase m, out string errorMessage)
        {
            if (m.MemberType == MemberTypes.Constructor)
            {
                errorMessage = "Constructors are not supported";
                return false;
            }

            if (!m.IsStatic)
            {
                errorMessage = "Instance methods are not supported";
                return false;
            }

            if (m.IsGenericMethod)
            {
                errorMessage = "Generic methods are not supported";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static MethodBase GetRuntimeMethod(MethodBase mi)
        {
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
                throw new NotSupportedException("Assembly location not found");
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

        static MethodExecutionResults ExecuteMethodImpl(object args)
        {
            MethodExecutionArgs mea = (MethodExecutionArgs)args;
            MethodExecutionResults res = new MethodExecutionResults();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                res.ReturnValue = mea.m.Invoke(null, mea.pars);
            }
            catch (Exception ex)
            {
                res.ExceptionObject = ex;
            }

            sw.Stop();
            res.Method = mea.m;
            res.ParameterValues = mea.pars;
            res.Duration = sw.Elapsed;
            return res;
        }

        public static MethodExecutionResults ExecuteMethod(MethodBase m, IEnumerable<MethodParameter> parameters, 
            TimeSpan timeout)
        {
            MethodExecutionResults res;
            List<object> invokationPars = new List<object>();

            foreach (MethodParameter par in parameters)
            {
                if(par.IsNull) { invokationPars.Add(null); continue; }

                object parValue;

                if (par.ParamType.Equals(typeof(IntPtr)))
                {
                    long x = long.Parse(par.Value);
                    parValue = new IntPtr(x);
                }
                else if (par.ParamType.Equals(typeof(UIntPtr)))
                {
                    ulong x = ulong.Parse(par.Value);
                    parValue = new UIntPtr(x);
                }
                else if (par.ParamType.IsByRef)
                {
                    Type tTarget = par.ParamType.GetElementType();
                    parValue = Convert.ChangeType(par.Value, tTarget);
                }
                else
                {
                    parValue = Convert.ChangeType(par.Value, par.ParamType);
                }

                invokationPars.Add(parValue);
            }

            MethodExecutionArgs mea = new MethodExecutionArgs();
            mea.m = m;
            mea.pars = invokationPars.ToArray();

            Task<MethodExecutionResults> tExecution = new Task<MethodExecutionResults>(ExecuteMethodImpl, mea);
            tExecution.Start();
            bool completed = tExecution.Wait(timeout);

            if (completed)
            {
                res = tExecution.Result;
                res.IsTimedOut = false;
            }
            else
            {
                res = new MethodExecutionResults();
                res.Method = m;
                res.IsTimedOut = true;
                res.Duration = timeout;
            }

            if (res.ReturnValue != null) res.ReturnValueType = res.ReturnValue.GetType();
            else res.ReturnValueType = ((MethodInfo)m).ReturnType;

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

        class MethodExecutionArgs
        {
            public MethodBase m;
            public object[] pars;
        }
    }
}
