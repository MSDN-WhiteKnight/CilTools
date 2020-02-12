using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    public class ModuleWrapper
    {
        MethodBase srcmethod;        

        protected ModuleWrapper(MethodBase mb)
        {
            this.srcmethod = mb;
        }

        public static ModuleWrapper Create(MethodBase mb)
        {
            if (mb is DynamicMethod) return new ModuleWrapperDynamic(mb);
            else return new ModuleWrapper(mb);
        }

        public virtual Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return srcmethod.Module.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return srcmethod.Module.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return srcmethod.Module.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return srcmethod.Module.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public virtual byte[] ResolveSignature(int metadataToken)
        {
            return srcmethod.Module.ResolveSignature(metadataToken);
        }

        public virtual string ResolveString(int metadataToken)
        {
            return srcmethod.Module.ResolveString(metadataToken);
        }
    }
}
