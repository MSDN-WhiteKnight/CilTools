/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using CilTools.Internal;
using CilTools.Metadata.Methods;
using CilTools.Reflection;

namespace CilTools.Metadata.Constructors
{
    class ConstructorDef : MdConstructorBase, ICustomMethod
    {
        MethodDefinitionHandle mdefh;
        MethodDefinition mdef;
        MethodBodyBlock mb;

        internal ConstructorDef(MethodDefinition m, MethodDefinitionHandle mh, MetadataAssembly owner)
        {
            this.assembly = owner;
            this.mdef = m;
            this.mdefh = mh;

            int rva = mdef.RelativeVirtualAddress;

            if (rva == 0)
            {
                this.mb = null;
            }
            else
            {
                try
                {
                    this.mb = assembly.PEReader.GetMethodBody(rva);
                }
                catch (BadImageFormatException)
                {
                    //Prevent BadImageFormatException (Invalid method header) 
                    //when accessing native methods in C++/CLI aasemblies

                    bool isNative = mdef.Attributes.HasFlag(MethodAttributes.PinvokeImpl) ||
                        mdef.ImplAttributes.HasFlag(MethodImplAttributes.Native);

                    if (isNative) this.mb = null;
                    else throw;
                }
            }

            //init declaring type
            TypeDefinitionHandle ht = mdef.GetDeclaringType();

            if (!ht.IsNil) this.decltype = this.assembly.GetTypeDefinition(ht);
            else this.decltype = null;

            //read signature
            base.InitSignature(mdef.Signature);
        }

        void ThrowIfDisposed()
        {
            if (this.assembly.MetadataReader == null)
            {
                throw new ObjectDisposedException("MetadataReader");
            }
        }

        /// <inheritdoc/>
        public override ITokenResolver TokenResolver
        {
            get { return this.assembly; }
        }

        /// <inheritdoc/>
        public override byte[] GetBytecode()
        {
            if (this.mb == null) return null;

            this.ThrowIfDisposed();

            return mb.GetILBytes(); //load CIL as byte array
        }

        /// <inheritdoc/>
        public override int MaxStackSize
        {
            get
            {
                if (mb != null) return mb.MaxStack;
                else return -1;
            }
        }

        /// <inheritdoc/>
        public override bool MaxStackSizeSpecified
        {
            get
            {
                return mb != null;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetLocalVarSignature()
        {
            if (this.mb == null) return new byte[0];
            if (mb.LocalSignature.IsNil) return new byte[0];

            StandaloneSignature sig = assembly.MetadataReader.GetStandaloneSignature(mb.LocalSignature);
            return assembly.MetadataReader.GetBlobBytes(sig.Signature);
        }

        /// <inheritdoc/>
        public override ExceptionBlock[] GetExceptionBlocks()
        {
            if (this.mb == null) return null;

            return MethodDef.GetMethodExceptionBlocks(this.mb, this.assembly);
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get
            {
                this.ThrowIfDisposed();

                MethodAttributes ret = mdef.Attributes;
                return ret;
            }
        }

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            this.ThrowIfDisposed();

            return this.mdef.ImplAttributes;
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            if (this.sig == null) return new ParameterInfo[0];

            return MethodDef.GetMethodParameters(this.assembly.MetadataReader, this, this.mdef, this.sig);
        }
        
        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.mdef.GetCustomAttributes();
            return Utils.ReadCustomAttributes(coll, this, this.assembly);
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
        
        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                return assembly.MetadataReader.GetString(mdef.Name);
            }
        }

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                return assembly.MetadataReader.GetToken(this.mdefh);
            }
        }

        /// <inheritdoc/>
        public override bool InitLocals
        {
            get
            {
                if (mb != null) return mb.LocalVariablesInitialized;
                else return false;
            }
        }

        /// <inheritdoc/>
        public override bool InitLocalsSpecified
        {
            get { return mb != null; }
        }

        public override bool IsGenericMethod
        {
            get
            {
                if (this.sig == null) return false;

                return this.sig.GenericArgsCount > 0;
            }
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                if (this.sig == null) return false;

                return this.sig.GenericArgsCount > 0;
            }
        }

        public override Type[] GetGenericArguments()
        {
            GenericParameterHandleCollection hcoll = mdef.GetGenericParameters();
            Type[] ret = new Type[hcoll.Count];

            for (int i = 0; i < ret.Length; i++)
            {
                GenericParameter gp = this.assembly.MetadataReader.GetGenericParameter(hcoll[i]);
                StringHandle sh = gp.Name;

                if (!sh.IsNil)
                    ret[i] = new GenericParamType(this, gp.Index, assembly.MetadataReader.GetString(sh));
                else
                    ret[i] = new GenericParamType(this, gp.Index);
            }

            return ret;
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                if (this.sig == null) return base.CallingConvention;

                CallingConventions ret = (CallingConventions)0;

                if (this.sig.CallingConvention == BytecodeAnalysis.CallingConvention.Vararg)
                {
                    ret = CallingConventions.VarArgs;
                }
                else if (this.sig.CallingConvention == BytecodeAnalysis.CallingConvention.Default)
                {
                    ret = CallingConventions.Standard;
                }

                if (this.sig.HasThis) ret |= CallingConventions.HasThis;
                if (this.sig.ExplicitThis) ret |= CallingConventions.ExplicitThis;

                return ret;
            }
        }
    }
}
