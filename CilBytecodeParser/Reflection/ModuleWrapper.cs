/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    /// <summary>
    /// Resolves metadata tokens in the context of the specified module
    /// </summary>
    internal class ModuleWrapper:ITokenResolver
    {
        protected MethodBase srcmethod;
        protected Module innermodule;

        /// <summary>
        /// Creates ModuleWrapper object using the module containing the specified method
        /// </summary>        
        public ModuleWrapper(MethodBase mb)
        {
            this.srcmethod = mb;
            this.innermodule = mb.Module;
        }

        /// <summary>
        /// Creates ModuleWrapper object using the explicitly specified module
        /// </summary>        
        public ModuleWrapper(Module m)
        {
            this.srcmethod = null;
            this.innermodule = m;
        }                

        public virtual Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return innermodule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual Type ResolveType(int metadataToken)
        {
            return ResolveType(metadataToken, null, null);
        }

        public virtual MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return innermodule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual MethodBase ResolveMethod(int metadataToken)
        {
            return ResolveMethod(metadataToken, null, null);
        }

        public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return innermodule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual FieldInfo ResolveField(int metadataToken)
        {
            return ResolveField(metadataToken, null, null);
        }

        public virtual MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return innermodule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual MemberInfo ResolveMember(int metadataToken)
        {
            return ResolveMember(metadataToken, null, null);
        }

        public virtual byte[] ResolveSignature(int metadataToken)
        {
            return innermodule.ResolveSignature(metadataToken);
        }

        public virtual string ResolveString(int metadataToken)
        {
            return innermodule.ResolveString(metadataToken);
        }

        public virtual Module InnerModule
        {
            get { return innermodule; }
        }

        public virtual MethodBase InnerMethod
        {
            get { return srcmethod; }
        }
    }
}
