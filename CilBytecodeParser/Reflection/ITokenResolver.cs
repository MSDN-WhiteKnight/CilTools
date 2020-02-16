/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    /// <summary>
    /// Represents an object that can be used to convert metadata tokens into corresponding high-level reflection objects
    /// </summary>
    public interface ITokenResolver
    {
        /// <summary>
        /// Returns the type identified by the specified metadata token, in the context defined by the specified generic type parameters.
        /// </summary>        
        Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments);

        /// <summary>
        /// Returns the type identified by the specified metadata token.
        /// </summary>        
        Type ResolveType(int metadataToken);

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token, in the context defined by the 
        /// specified generic type parameters.
        /// </summary>        
        MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments);

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token.
        /// </summary>        
        MethodBase ResolveMethod(int metadataToken);

        /// <summary>
        /// Returns the field identified by the specified metadata token, in the context defined by the specified generic type parameters.
        /// </summary>        
        FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments);

        /// <summary>
        /// Returns the field identified by the specified metadata token.
        /// </summary>        
        FieldInfo ResolveField(int metadataToken);

        /// <summary>
        /// Returns the type or member identified by the specified metadata token, in the context defined by the specified 
        /// generic type parameters.
        /// </summary>        
        MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments);

        /// <summary>
        /// Returns the type or member identified by the specified metadata token.
        /// </summary>        
        MemberInfo ResolveMember(int metadataToken);

        /// <summary>
        /// Returns the signature blob identified by a metadata token.
        /// </summary>        
        /// <returns>An array of bytes representing the signature blob.</returns>
        byte[] ResolveSignature(int metadataToken);

        /// <summary>
        /// Returns the string identified by the specified metadata token.
        /// </summary>        
        string ResolveString(int metadataToken);
    }
}
