/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
using CilTools.Reflection;

namespace CilTools.Metadata.Methods
{
    class MethodRef : MdMethodInfoBase, ICustomMethod
    {        
        MemberReferenceHandle mrefh;
        MemberReference mref;        
        MethodBase impl;        

        internal MethodRef(MemberReference m, MemberReferenceHandle mh, MetadataAssembly owner)
        {
            Debug.Assert(m.GetKind() == MemberReferenceKind.Method, "MemberReference passed to MethodRef ctor should be a method");

            this.assembly = owner;
            this.mref = m;
            this.mrefh = mh;
            
            //init declaring type
            this.decltype = Utils.GetRefDeclaringType(this.assembly, this, mref.Parent);
            
            //read signature
            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mref.Signature);
            GenericContext gctx = GenericContext.Create(this.decltype, this);
            SignatureContext ctx = SignatureContext.Create(this.assembly, gctx, null);

            try
            {
                this.sig = Signature.ReadFromArray(sigbytes, ctx);
            }
            catch (NotSupportedException) { }
        }
        
        void LoadImpl()
        {
            //loads actual implementation method referenced by this instance

            if (this.impl != null) return;//already loaded
            if (this.assembly.AssemblyReader == null) return;

            Type et = this.DeclaringType;

            if (et == null) return;

            this.impl = Utils.ResolveMethodRef(this.assembly, et, this, this.sig);
        }                

        /// <inheritdoc/>
        public override ITokenResolver TokenResolver
        {
            get 
            {
                if (this.impl != null) return ((ICustomMethod)this.impl).TokenResolver;
                else return this.assembly; 
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytecode()
        {
            this.LoadImpl();

            if (this.impl != null) return ((ICustomMethod)this.impl).GetBytecode();
            else throw new CilParserException("Failed to load method implementation");
        }

        /// <inheritdoc/>
        public override int MaxStackSize
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).MaxStackSize;
                else return 0;
            }
        }

        /// <inheritdoc/>
        public override bool MaxStackSizeSpecified
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).MaxStackSizeSpecified;
                else return false;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetLocalVarSignature()
        {
            this.LoadImpl();

            if (this.impl != null) return ((ICustomMethod)this.impl).GetLocalVarSignature();
            else return new byte[] { };
        }

        /// <inheritdoc/>
        public override ExceptionBlock[] GetExceptionBlocks()
        {
            this.LoadImpl();

            if (this.impl != null) return ((ICustomMethod)this.impl).GetExceptionBlocks();
            else return new ExceptionBlock[] { };
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get
            {
                try
                {
                    this.LoadImpl();
                }
                catch (TypeLoadException) { }

                MethodAttributes ret = (MethodAttributes)0;

                if (this.impl != null)
                {
                    ret = this.impl.Attributes;
                }
                else if (this.sig != null)
                {
                    //if failed to load impl, we can at least get static/instance attribute from signature
                    if (!this.sig.HasThis) ret |= MethodAttributes.Static;
                }

                return ret;
            }
        }

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            MethodImplAttributes ret = (MethodImplAttributes)0;

            if (this.impl != null)
            {
                ret = this.impl.GetMethodImplementationFlags();
            }

            return ret;
        }

        ParameterInfo[] GetParameters_Sig()
        {
            if (this.sig == null) return new ParameterInfo[0];

            return Utils.GetParametersFromSignature(this.sig, this);
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            if (this.impl != null)
            {
                //reading parameters from impl gives more data (names, default values)
                return this.impl.GetParameters();
            }
            else
            {
                //even if failed to load impl, we can read parameters from signature
                return this.GetParameters_Sig();
            }
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetCustomAttributes(attributeType,inherit);
            else return new object[] { };
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetCustomAttributes(inherit);
            else return new object[] { };
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (this.impl != null) return this.impl.IsDefined(attributeType, inherit);
            else return false;
        }

        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                return assembly.MetadataReader.GetString(mref.Name);
            }
        }

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                return assembly.MetadataReader.GetToken(this.mrefh);
            }
        }

        /// <inheritdoc/>
        public override bool InitLocals
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).InitLocals;
                else return false;
            }
        }

        /// <inheritdoc/>
        public override bool InitLocalsSpecified
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).InitLocalsSpecified;
                return false;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                if (this.sig != null) return this.sig.GenericArgsCount != 0;
                else return false;
            }
        }

        public override Type[] GetGenericArguments()
        {
            if (this.sig != null && this.sig.GenericArgsCount == 0)
            {
                return Utils.EmptyTypeArray;
            }

            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            if (this.impl != null) return this.impl.GetGenericArguments();
            else return Utils.EmptyTypeArray;
        }

        public override Reflection.LocalVariable[] GetLocalVariables()
        {
            byte[] sig = this.GetLocalVarSignature();

            return Reflection.LocalVariable.ReadSignature(sig, this.TokenResolver, this);
        }

        public override PInvokeParams GetPInvokeParams()
        {
            return null;
        }
    }
}
