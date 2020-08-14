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
    class MethodInstance : CustomMethod
    {
        MetadataAssembly assembly;
        MethodSpecificationHandle mspech;
        MethodSpecification mspec;
        CustomMethod definition; //generic method definition which this one is instantiating
        MethodBodyBlock mb;
        Signature sig; //signature of this instantiation (contains generic args, not regular params) 

        internal MethodInstance(MethodSpecification m, MethodSpecificationHandle mh, MetadataAssembly owner)
        {
            this.assembly = owner;
            this.mspec = m;
            this.mspech = mh;

            EntityHandle eh = mspec.Method;

            if (eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = owner.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)eh);
                this.definition = new MetadataMethod(mdef, (MethodDefinitionHandle)eh, owner);

                int rva = mdef.RelativeVirtualAddress;

                if (rva == 0)
                {
                    this.mb = null;
                }
                else
                {
                    this.mb = assembly.PEReader.GetMethodBody(rva);
                }
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = owner.MetadataReader.GetMemberReference((MemberReferenceHandle)eh);
                this.definition = new ExternalMethod(mref, (MemberReferenceHandle)eh, owner);
            }

            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mspec.Signature);
            this.sig = new Signature(sigbytes, this.assembly,this.definition);
        }

        /// <summary>
        /// Gets the method's returned type
        /// </summary>
        public override Type ReturnType
        {
            get
            {
                return this.definition.ReturnType;
            }
        }

        /// <inheritdoc/>
        public override ITokenResolver TokenResolver
        {
            get
            {
                if (this.definition != null) return this.definition.TokenResolver;
                else return this.assembly;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytecode()
        {
            if (this.mb != null) return mb.GetILBytes(); //load CIL as byte array
            else return this.definition.GetBytecode();
        }

        /// <inheritdoc/>
        public override int MaxStackSize
        {
            get
            {
                if (mb != null) return mb.MaxStack;
                else return this.definition.MaxStackSize;
            }
        }

        /// <inheritdoc/>
        public override bool MaxStackSizeSpecified
        {
            get
            {
                if (mb != null) return true;
                else return this.definition.MaxStackSizeSpecified;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetLocalVarSignature()
        {
            if (this.mb != null && !this.mb.LocalSignature.IsNil)
            {
                StandaloneSignature sig = assembly.MetadataReader.GetStandaloneSignature(mb.LocalSignature);
                return assembly.MetadataReader.GetBlobBytes(sig.Signature);
            }
            else return this.definition.GetLocalVarSignature();
        }

        /// <inheritdoc/>
        public override ExceptionBlock[] GetExceptionBlocks()
        {
            if (this.mb == null)
            {
                return this.definition.GetExceptionBlocks();
            }

            ExceptionBlock[] ret = new ExceptionBlock[mb.ExceptionRegions.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ExceptionHandlingClauseOptions opt = (ExceptionHandlingClauseOptions)0;
                Type t = null;

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
                    opt, mb.ExceptionRegions[i].TryOffset, mb.ExceptionRegions[i].TryLength, t,
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
                return this.definition.Attributes;
            }
        }

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return this.definition.GetMethodImplementationFlags();
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            return this.definition.GetParameters();
        }

        public override Type[] GetGenericArguments()
        {
            if (this.sig == null) return this.definition.GetGenericArguments();
            
            Type[] args = new Type[this.sig.ParamsCount];
            Type[] dargs = this.definition.GetGenericArguments();
            if (dargs == null) dargs = args;

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = this.sig.GetParamType(i).Type;

                if(args[i].IsGenericParameter && args[i].DeclaringMethod != null)
                {
                    if (i >= dargs.Length) continue;

                    //if type is generic method argument, substitute with type from definition
                    //so we will have argument name
                    args[i] = dargs[i];
                }
            }

            return args;
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
                return this.definition.DeclaringType;
            }
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return this.definition.GetCustomAttributes(attributeType, inherit);
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return this.definition.GetCustomAttributes(inherit);
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return this.definition.IsDefined(attributeType, inherit);
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
                return this.definition.Name;
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
                return assembly.MetadataReader.GetToken(this.mspech);
            }
        }

        /// <inheritdoc/>
        public override bool InitLocals
        {
            get
            {
                if (mb != null) return mb.LocalVariablesInitialized;
                else return this.definition.InitLocals;
            }
        }

        /// <inheritdoc/>
        public override bool InitLocalsSpecified
        {
            get
            {
                if (mb != null) return true;
                else return this.definition.InitLocalsSpecified;
            }
        }

        public override bool IsGenericMethod { get { return true; } }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return false;
            }
        }
    }
}
