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
        Type decltype;

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

            //init declaring type
            TypeDefinitionHandle ht = mdef.GetDeclaringType();

            if (!ht.IsNil) this.decltype=new MetadataType(assembly.MetadataReader.GetTypeDefinition(ht), ht, this.assembly);
            else this.decltype = null;
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
            if (this.mb == null) return null;

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
            return this.mdef.ImplAttributes;
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            if (this.sig == null) return new ParameterInfo[0];

            ParameterInfo[] pars = new ParameterInfo[this.sig.ParamsCount];
            ParameterHandleCollection hcoll = this.mdef.GetParameters();

            foreach (ParameterHandle h in hcoll)
            {
                Parameter par = this.assembly.MetadataReader.GetParameter(h);
                int index = par.SequenceNumber-1;
                if (index >= pars.Length) continue;
                if (index < 0) continue;

                pars[index] = new ParameterSpec(
                    this.sig.GetParamType(index), par, this,this.assembly.MetadataReader
                    );
            }

            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i] == null) pars[i] = new ParameterSpec(this.sig.GetParamType(i), i, this);
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
                return this.decltype;
            }
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
            object[] ret = new object[coll.Count];
            int i = 0;

            foreach (CustomAttributeHandle h in coll)
            {
                CustomAttribute ca = this.assembly.MetadataReader.GetCustomAttribute(h);
                EntityHandle eh = ca.Constructor;
                MethodBase constr=null;

                if (eh.Kind == HandleKind.MethodDefinition)
                {
                    MethodDefinition mdef = assembly.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)eh);
                    constr = new MetadataMethod(mdef, (MethodDefinitionHandle)eh, this.assembly);
                }
                else if (eh.Kind == HandleKind.MemberReference)
                {
                    MemberReference mref = assembly.MetadataReader.GetMemberReference((MemberReferenceHandle)eh);

                    if (mref.GetKind() == MemberReferenceKind.Method)
                        constr = new ExternalMethod(mref, (MemberReferenceHandle)eh, this.assembly);
                }

                ret[i] = new MetadataCustomAttribute(this, constr, assembly.MetadataReader.GetBlobBytes(ca.Value));
                i++;
            }

            return ret;
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
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
            
            for(int i = 0; i < ret.Length; i++)
            {
                GenericParameter gp = this.assembly.MetadataReader.GetGenericParameter(hcoll[i]);
                StringHandle sh = gp.Name;

                if(!sh.IsNil) 
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
                else if(this.sig.CallingConvention == BytecodeAnalysis.CallingConvention.Default)
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

