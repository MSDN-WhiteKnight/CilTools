/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Reflection;
using System.Reflection.Metadata;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Metadata.Constructors
{
    abstract class MdConstructorBase : ConstructorInfo, ICustomMethod
    {
        protected MetadataAssembly assembly;
        protected Signature sig;
        protected Type decltype;

        protected void InitSignature(BlobHandle hSig)
        {
            GenericContext gctx = GenericContext.Create(this.decltype, this);
            SignatureContext ctx = SignatureContext.Create(this.assembly, gctx, null);
            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(hSig);
            this.sig = Signature.ReadFromArray(sigbytes, ctx);
        }

        public abstract ITokenResolver TokenResolver { get; }

        public abstract int MaxStackSize { get; }

        public abstract bool MaxStackSizeSpecified { get; }

        public abstract bool InitLocals { get; }

        public abstract bool InitLocalsSpecified { get; }

        public abstract byte[] GetBytecode();

        public abstract byte[] GetLocalVarSignature();

        public abstract ExceptionBlock[] GetExceptionBlocks();

        /// <inheritdoc/>
        public override Type DeclaringType
        {
            get
            {
                return this.decltype;
            }
        }

        public Type ReturnType
        {
            get { return null; }
        }

        /// <inheritdoc/>
        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Constructor;
            }
        }
        
        /// <inheritdoc/>
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters,
            System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        /// <inheritdoc/>
        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        /// <inheritdoc/>
        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }
                
        public MethodBase GetDefinition()
        {
            return null;
        }

        public PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        public Reflection.LocalVariable[] GetLocalVariables()
        {
            byte[] sig = this.GetLocalVarSignature();

            return Reflection.LocalVariable.ReadSignature(sig, this.TokenResolver, this);
        }
    }
}
