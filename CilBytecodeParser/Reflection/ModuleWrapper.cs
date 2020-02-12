using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    internal class ModuleWrapper
    {
        protected MethodBase srcmethod;
        protected Module innermodule;

        protected ModuleWrapper(MethodBase mb)
        {
            this.srcmethod = mb;
            this.innermodule = mb.Module;
        }

        public ModuleWrapper(Module m)
        {
            this.srcmethod = null;
            this.innermodule = m;
        }

        public static ModuleWrapper Create(MethodBase mb)
        {
            if(Types.IsDynamicMethod(mb)) return new ModuleWrapperDynamic(mb);
            else return new ModuleWrapper(mb);
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

        public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return innermodule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
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
