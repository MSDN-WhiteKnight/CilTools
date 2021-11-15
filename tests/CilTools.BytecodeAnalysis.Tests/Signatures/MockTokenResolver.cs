/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using CilTools.Reflection;

namespace CilTools.BytecodeAnalysis.Tests.Signatures
{
    public class MockTokenResolver : ITokenResolver
    {
        public static readonly MockTokenResolver Value = new MockTokenResolver();

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
