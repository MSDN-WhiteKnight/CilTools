/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    class MetadataMethod : CustomMethod
    {        
        MetadataAssembly assembly;
        MethodDefinitionHandle mdefh;
        MethodDefinition mdef;
        MethodBodyBlock mb;
        Signature sig;

        internal MetadataMethod(MethodDefinition m, MethodDefinitionHandle mh, MetadataAssembly owner)
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
                this.mb = assembly.PEReader.GetMethodBody(rva);
            }

            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mdef.Signature);
            this.sig = new Signature(sigbytes, this.assembly,this);
        }

        /// <summary>
        /// Gets the method's returned type
        /// </summary>
        public override Type ReturnType
        {
            get
            {
                if (String.Equals(this.Name, ".ctor", StringComparison.InvariantCulture)) return null;

                if (this.sig == null) return UnknownType.Value;
                else return this.sig.ReturnType.Type;
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
            if (this.mb == null) return null;
            
            StandaloneSignature sig = assembly.MetadataReader.GetStandaloneSignature(mb.LocalSignature);
            return assembly.MetadataReader.GetBlobBytes(sig.Signature);            
        }

        /// <inheritdoc/>
        public override ExceptionBlock[] GetExceptionBlocks()
        {
            if (this.mb == null) return null;

            ExceptionBlock[] ret = new ExceptionBlock[mb.ExceptionRegions.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ExceptionHandlingClauseOptions opt=(ExceptionHandlingClauseOptions)0;
                Type t=null;

                switch (mb.ExceptionRegions[i].Kind)
                {
                    case ExceptionRegionKind.Catch: 
                        opt = ExceptionHandlingClauseOptions.Clause;
                        EntityHandle eh = mb.ExceptionRegions[i].CatchType;
                        
                        if (eh.Kind == HandleKind.TypeDefinition)
                        {
                            t = new MetadataType(
                                assembly.MetadataReader.GetTypeDefinition((TypeDefinitionHandle)eh),
                                (TypeDefinitionHandle)eh,
                                this.assembly);
                        }
                        else if (eh.Kind == HandleKind.TypeReference)
                        {
                            t = new ExternalType(
                                assembly.MetadataReader.GetTypeReference((TypeReferenceHandle)eh),
                                (TypeReferenceHandle)eh,
                                this.assembly);
                        }

                        break;
                    case ExceptionRegionKind.Finally: opt = ExceptionHandlingClauseOptions.Finally; break;
                    case ExceptionRegionKind.Filter: opt = ExceptionHandlingClauseOptions.Filter; break;
                    case ExceptionRegionKind.Fault: opt = ExceptionHandlingClauseOptions.Fault; break;
                }

                ret[i] = new ExceptionBlock(
                    opt, mb.ExceptionRegions[i].TryOffset, mb.ExceptionRegions[i].TryLength,t,
                    mb.ExceptionRegions[i].HandlerOffset, mb.ExceptionRegions[i].HandlerLength,
                    mb.ExceptionRegions[i].FilterOffset);
            }

            return ret;
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get
            {
                MethodAttributes ret = mdef.Attributes;
                return ret;
            }
        }

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            if (this.sig == null) return new ParameterInfo[0];

            ParameterInfo[] pars = new ParameterInfo[this.sig.ParamsCount];

            for (int i = 0; i < pars.Length; i++)
            {
                pars[i] = new Parameter(this.sig.GetParamType(i), i, this);
            }

            return pars;
        }

        /// <inheritdoc/>
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters,
            System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
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
                TypeDefinitionHandle ht = mdef.GetDeclaringType();

                if (!ht.IsNil) return new MetadataType(assembly.MetadataReader.GetTypeDefinition(ht), ht, this.assembly);
                else return null;
            }
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new object[] { };
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[] { };
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        /// <inheritdoc/>
        public override MemberTypes MemberType
        {
            get { return MemberTypes.Method; }
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
        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
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
    }
}

