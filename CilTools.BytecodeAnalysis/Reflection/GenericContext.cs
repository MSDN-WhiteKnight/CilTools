/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represent a set of generic type and method parameters that defines a meaning of generic signature in some context
    /// </summary>
    public class GenericContext
    {
        Type declaringType;
        MethodBase declaringMethod;
        Type[] genericTypeArguments;
        Type[] genericMethodArguments;

        internal static readonly GenericContext Empty = new GenericContext(null,null,null,null);

        GenericContext(Type t, MethodBase m, Type[] typeargs, Type[] methodargs)
        {
            this.declaringType = t;
            this.declaringMethod = m;
            this.genericTypeArguments = typeargs;
            this.genericMethodArguments = methodargs;
        }

        public static GenericContext Create(Type[] typeargs, Type[] methodargs)
        {
            Type decltype = null;

            if (typeargs != null && typeargs.Length > 0 && typeargs[0].IsGenericParameter)
            {
                try
                {
                    decltype = typeargs[0].DeclaringType;
                }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
            }

            MethodBase declmethod = null;

            if (methodargs != null && methodargs.Length > 0 && methodargs[0].IsGenericParameter)
            {
                try
                {
                    declmethod = methodargs[0].DeclaringMethod;
                }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
            }

            return new GenericContext(decltype, declmethod, typeargs, methodargs);
        }

        public static GenericContext Create(Type decltype,MethodBase declmethod)
        {
            Type[] typeargs = null;
            Type[] methodargs = null;

            if (decltype != null && decltype.IsGenericType)
            {
                try
                {
                    typeargs = decltype.GetGenericArguments();
                }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
            }

            if (declmethod != null && declmethod.IsGenericMethod)
            {
                try
                {
                    methodargs = declmethod.GetGenericArguments();
                }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }
            }

            return new GenericContext(decltype, declmethod, typeargs, methodargs);
        }

        public Type[] GenericTypeArguments
        {
            get { return this.genericTypeArguments; }
        }

        public Type[] GenericMethodArguments
        {
            get { return this.genericMethodArguments; }
        }

        public Type DeclaringType
        {
            get { return this.declaringType; }
        }

        public MethodBase DeclaringMethod
        {
            get { return this.declaringMethod; }
        }

        internal MemberInfo GetDeclaringMember()
        {
            if (this.declaringMethod != null) return this.declaringMethod;
            else return this.declaringType;
        }

        internal static GenericContext FromMember(MemberInfo m)
        {
            if (m == null) return GenericContext.Empty;

            MethodBase declmethod = null;
            Type decltype = null;

            if (m is MethodBase)
            {
                declmethod = (MethodBase)m;
                decltype = m.DeclaringType;
                return GenericContext.Create(decltype, declmethod);
            }
            else if (m is Type)
            {
                decltype = m as Type;
                return GenericContext.Create(decltype, declmethod);
            }
            else
            {
                decltype = m.DeclaringType;
                return GenericContext.Create(decltype, declmethod);
            }
        }
    }
}
