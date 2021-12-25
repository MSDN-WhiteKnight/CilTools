/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Encapsulates data that identifies the meaning of the signature in some context
    /// </summary>
    /// <remarks>
    /// Some ECMA-335 signature elements, such as class references or generic parameters, require additional 
    /// data to be mapped into the concrete types. Signature context holds such data, for example, a metadata token 
    /// resolver or a generic context.
    /// </remarks>
    public class SignatureContext
    {
        ITokenResolver resolver;
        GenericContext gctx;
        MethodBase gdef;

        static SignatureContext s_empty;
        
        internal SignatureContext(ITokenResolver tokenResolver, GenericContext genericContext, MethodBase genericDefinition)
        {
            Debug.Assert(tokenResolver != null);

            if (genericContext == null)
            {
                genericContext = GenericContext.Empty;
            }

            this.resolver = tokenResolver;
            this.gctx = genericContext;
            this.gdef = genericDefinition;
        }

        internal static SignatureContext Empty
        {
            get
            {
                // Empty signature context should only be used when Signature is sythesized from reflection objects.
                // Parsed signatures should use real context with working resolver.

                if (s_empty == null) s_empty = FromResolver(new DummyTokenResolver());

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

        
        public MethodBase GenericDefinition
        {
            get { return this.gdef; }
        }

        /// <summary>
        /// Creates a new signature context instance
        /// </summary>
        /// <param name="tokenResolver">Metadata tokens resolver</param>
        /// <param name="genericContext">Generic context</param>
        /// <param name="genericDefinition">
        /// Generic method definition, if the signature is for a generic method instantiation, or null otherwise
        /// </param>
        /// <exception cref="ArgumentNullException">Token resolver is null</exception>
        public static SignatureContext Create(ITokenResolver tokenResolver, GenericContext genericContext,
            MethodBase genericDefinition)
        {
            if (tokenResolver == null)
            {
                throw new ArgumentNullException("tokenResolver");
            }

            return new SignatureContext(tokenResolver, genericContext, genericDefinition);
        }

        internal static SignatureContext FromResolver(ITokenResolver resolver)
        {
            return new SignatureContext(resolver, GenericContext.Empty, null);
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
