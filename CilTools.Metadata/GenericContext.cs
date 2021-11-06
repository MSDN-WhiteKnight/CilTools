/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Metadata
{
    internal struct GenericContext
    {
        Type[] typeargs;
        Type[] methodargs;

        public static readonly GenericContext Empty = new GenericContext();

        public GenericContext(Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            this.typeargs = genericTypeArguments;
            this.methodargs = genericMethodArguments;
        }

        public Type[] GenericTypeArguments
        {
            get { return this.typeargs; }
        }

        public Type[] GenericMethodArguments
        {
            get { return this.methodargs; }
        }

        public Type GetDeclaringType()
        {
            if (typeargs != null && typeargs.Length > 0 && methodargs == null)
            {
                Type declaringType = null;

                try
                {
                    declaringType = typeargs[0].DeclaringType;
                }
                catch (NotImplementedException) { }
                catch (NotSupportedException) { }

                return declaringType;
            }
            else
            {
                return null;
            }
        }
    }
}
