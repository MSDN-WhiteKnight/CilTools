/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Encapsulates data that identifies the meaning of the signature in some context
    /// </summary>
    /// <remarks>
    /// Some ECMA-335 signature elements, such as class references or generic parameters, require additional 
    /// data to be mapped into the concrete types. Signature context holds such data: a metadata token resolver 
    /// and a generic context.
    /// </remarks>
    public class SignatureContext
    {
        ITokenResolver resolver;
        GenericContext gctx;

        /// <summary>
        /// Creates a new signature context instance
        /// </summary>
        /// <param name="tokenResolver">Metadata tokens resolver</param>
        /// <param name="genericContext">Generic context</param>
        /// <exception cref="ArgumentNullException">Token resolver is null</exception>
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

        /// <summary>
        /// Gets the token resolver object
        /// </summary>
        public ITokenResolver TokenResolver
        {
            get { return this.resolver; }
        }

        /// <summary>
        /// Gets a generic context object
        /// </summary>
        public GenericContext GenericContext
        {
            get { return this.gctx; }
        }
    }
}
