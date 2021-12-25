﻿/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
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

        static SignatureContext s_empty;
        
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

        internal static SignatureContext Empty
        {
            get
            {
                // Empty signature context should only be used when Signature is sythesized from reflection objects.
                // Parsed signatures should use real context with working resolver.

                if (s_empty == null) s_empty = new SignatureContext(new DummyTokenResolver(), GenericContext.Empty);

                return s_empty;
            }
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

        class DummyTokenResolver : ITokenResolver
        {
            public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                return null;
            }

            public FieldInfo ResolveField(int metadataToken)
            {
                return null;
            }

            public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                return null;
            }

            public MemberInfo ResolveMember(int metadataToken)
            {
                return null;
            }

            public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                return null;
            }

            public MethodBase ResolveMethod(int metadataToken)
            {
                return null;
            }

            public byte[] ResolveSignature(int metadataToken)
            {
                return null;
            }

            public string ResolveString(int metadataToken)
            {
                return null;
            }

            public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                return null;
            }

            public Type ResolveType(int metadataToken)
            {
                return null;
            }
        }
    }
}
