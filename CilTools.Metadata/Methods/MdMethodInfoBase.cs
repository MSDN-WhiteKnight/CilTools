/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Globalization;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Metadata.Methods
{
    abstract class MdMethodInfoBase : MethodInfo, ICustomMethod
    {
        protected MetadataAssembly assembly;
        protected Signature sig;
        protected Type decltype;

        public abstract ITokenResolver TokenResolver { get; }
        public abstract int MaxStackSize { get; }
        public abstract bool MaxStackSizeSpecified { get; }
        public abstract bool InitLocals { get; }
        public abstract bool InitLocalsSpecified { get; }

        public abstract byte[] GetBytecode();
        public abstract byte[] GetLocalVarSignature();
        public abstract ExceptionBlock[] GetExceptionBlocks();
        public abstract Reflection.LocalVariable[] GetLocalVariables();
        public abstract PInvokeParams GetPInvokeParams();

        /// <summary>
        /// Gets the method's returned type
        /// </summary>
        public override Type ReturnType
        {
            get
            {
                if (this.sig == null) return UnknownType.Value;
                else return this.sig.ReturnType.Type;
            }
        }

        /// <inheritdoc/>
        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override Type DeclaringType
        {
            get
            {
                return this.decltype;
            }
        }

        /// <inheritdoc/>
        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Method;
            }
        }

        /// <inheritdoc/>
        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        /// <inheritdoc/>
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        public MethodBase GetDefinition()
        {
            return null;
        }

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }
    }
}
