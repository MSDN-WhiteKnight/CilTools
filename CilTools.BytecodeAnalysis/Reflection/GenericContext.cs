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
    /// Represent a set of information that defines a meaning of generic signature in some context
    /// </summary>
    /// <remarks>
    /// A generic context is used with when reading signatures that could potentially contain generic parameters. 
    /// A generic parameter in signature only encodes its number; getting any additional information, such as a 
    /// parameter name, requires access to specific generic parameters or arguments being referenced in current 
    /// context.
    /// </remarks>
    public class GenericContext
    {
        Type declaringType;
        MethodBase declaringMethod;
        Type[] genericTypeArguments;
        Type[] genericMethodArguments;

        internal static readonly GenericContext Empty = new GenericContext(null,null,null,null);
        static readonly Type[] s_emptyArgs = new Type[0];

        GenericContext(Type t, MethodBase m, Type[] typeargs, Type[] methodargs)
        {
            this.declaringType = t;
            this.declaringMethod = m;
            this.genericTypeArguments = typeargs;
            this.genericMethodArguments = methodargs;
        }

        /// <summary>
        /// Creates a new generic context using the specified generic arguments
        /// </summary>
        /// <param name="typeargs">An array of generic type arguments (can be null)</param>
        /// <param name="methodargs">An array of generic method arguments (can be null)</param>
        /// <remarks>
        /// Pass null values if the generic context is unknown or signature does not use generic parameters
        /// </remarks>
        public static GenericContext FromArgs(Type[] typeargs, Type[] methodargs)
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

            if (methodargs != null && methodargs.Length > 0)
            {
                try
                {
                    if (methodargs[0].IsGenericParameter)
                    {
                        declmethod = methodargs[0].DeclaringMethod;
                    }
                }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }

                if (declmethod == null) declmethod = UnknownMethod.Value;
            }

            return new GenericContext(decltype, declmethod, typeargs, methodargs);
        }

        /// <summary>
        /// Gets generic type or method arguments from the specified instance. Returns empty array on failure.
        /// </summary>
        internal static Type[] TryGetGenericArguments(MemberInfo member) 
        {
            Type[] ret=s_emptyArgs;

            try
            {
                if (member is Type)
                {
                    Type t = (Type)member;

                    if (t.IsGenericType)
                    {
                        ret = t.GetGenericArguments();
                    }
                }
                else if (member is MethodBase) 
                {
                    MethodBase m = (MethodBase)member;

                    if (m.IsGenericMethod) 
                    {
                        ret = m.GetGenericArguments();
                    }
                }
            }
            catch (NotImplementedException) { }
            catch (NotSupportedException) { }

            return ret;
        }

        /// <summary>
        /// Creates a new generic context using the specified declaring members
        /// </summary>
        /// <param name="decltype">Declaring type (could be null)</param>
        /// <param name="declmethod">Declaring method (could be null)</param>
        /// <remarks>
        /// Pass null values if the generic context is unknown or signature does not use generic parameters
        /// </remarks>
        public static GenericContext Create(Type decltype,MethodBase declmethod)
        {
            Type[] typeargs = null;
            Type[] methodargs = null;

            if (decltype != null)
            {
                typeargs = TryGetGenericArguments(decltype);
            }

            if (declmethod != null)
            {
                methodargs = TryGetGenericArguments(declmethod);
            }

            return new GenericContext(decltype, declmethod, typeargs, methodargs);
        }

        /// <summary>
        /// Gets a generic type argument at the specified index
        /// </summary>
        public Type GetTypeArgument(int i)
        {
            if (this.genericTypeArguments == null) return null;
            else return this.genericTypeArguments[i];
        }

        /// <summary>
        /// Gets a generic method argument at the specified index
        /// </summary>        
        public Type GetMethodArgument(int i)
        {
            if (this.genericMethodArguments == null) return null;
            else return this.genericMethodArguments[i];
        }

        /// <summary>
        /// Gets the number of generic type arguments
        /// </summary>
        public int TypeArgumentsCount
        {
            get
            {
                if (this.genericTypeArguments == null) return 0;
                else return this.genericTypeArguments.Length;
            }
        }

        /// <summary>
        /// Gets the number of generic method arguments
        /// </summary>
        public int MethodArgumentsCount
        {
            get
            {
                if (this.genericMethodArguments == null) return 0;
                else return this.genericMethodArguments.Length;
            }
        }

        /// <summary>
        /// Gets a declaring type in this generic context
        /// </summary>
        public Type DeclaringType
        {
            get { return this.declaringType; }
        }

        /// <summary>
        /// Gets a declaring method in this generic context
        /// </summary>
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
