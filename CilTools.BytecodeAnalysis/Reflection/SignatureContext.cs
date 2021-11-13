/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Reflection
{
    public class SignatureContext
    {
        ITokenResolver resolver;
        GenericContext gctx;

        public SignatureContext(ITokenResolver tokenResolver, GenericContext genericContext)
        {
            if (tokenResolver == null)
            {
                throw new ArgumentNullException("tokenResolver");
            }

            if (genericContext == null)
            {
                genericContext = GenericContext.Empty;
            }

            this.resolver = tokenResolver;
            this.gctx = genericContext;
        }

        public ITokenResolver TokenResolver
        {
            get { return this.resolver; }
        }

        public GenericContext GenericContext
        {
            get { return this.gctx; }
        }
    }
}
