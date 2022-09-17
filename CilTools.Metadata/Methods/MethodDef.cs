/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
using CilTools.Reflection;

namespace CilTools.Metadata.Methods
{
    class MethodDef : MdMethodInfoBase, ICustomMethod
    {
        MethodDefinitionHandle mdefh;
        MethodDefinition mdef;
        MethodBodyBlock mb;

        internal MethodDef(MethodDefinition m, MethodDefinitionHandle mh, MetadataAssembly owner)
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
            GenericContext gctx = GenericContext.Create(this.decltype, this);
            SignatureContext ctx = SignatureContext.Create(this.assembly, gctx, null);
            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mdef.Signature);
            this.sig = Signature.ReadFromArray(sigbytes, ctx);
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

            return Utils.GetMethodExceptionBlocks(this.mb, this.assembly);
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

            return Utils.GetMethodParameters(this.assembly, this, this.mdef, this.sig);
        }
        
        /// <inheritdoc/>
        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
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
            return Utils.GetGenericParameters(this.assembly, this, hcoll);
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

        public override PInvokeParams GetPInvokeParams()
        {
            if (!this.mdef.Attributes.HasFlag(MethodAttributes.PinvokeImpl)) return null;

            MethodImport imp = this.mdef.GetImport();

            if (imp.Module.IsNil) return null;
            if (imp.Name.IsNil) return null;

            ModuleReference mrf = this.assembly.MetadataReader.GetModuleReference(imp.Module);
            string modulename = this.assembly.MetadataReader.GetString(mrf.Name);
            string funcname = this.assembly.MetadataReader.GetString(imp.Name);
            CharSet cs = (CharSet)0;

            switch (imp.Attributes & MethodImportAttributes.CharSetMask)
            {
                case MethodImportAttributes.CharSetAnsi:cs = CharSet.Ansi;break;
                case MethodImportAttributes.CharSetAuto: cs = CharSet.Auto; break;
                case MethodImportAttributes.CharSetUnicode: cs = CharSet.Unicode; break;                
            }

            System.Runtime.InteropServices.CallingConvention conv=
                (System.Runtime.InteropServices.CallingConvention)0;
            
            switch (imp.Attributes & MethodImportAttributes.CallingConventionMask)
            {
                case MethodImportAttributes.CallingConventionStdCall: 
                    conv = System.Runtime.InteropServices.CallingConvention.StdCall; 
                    break;
                case MethodImportAttributes.CallingConventionWinApi:
                    conv = System.Runtime.InteropServices.CallingConvention.Winapi;
                    break;
                case MethodImportAttributes.CallingConventionCDecl:
                    conv = System.Runtime.InteropServices.CallingConvention.Cdecl;
                    break;
                case MethodImportAttributes.CallingConventionFastCall:
                    conv = System.Runtime.InteropServices.CallingConvention.FastCall;
                    break;
                case MethodImportAttributes.CallingConventionThisCall:
                    conv = System.Runtime.InteropServices.CallingConvention.ThisCall;
                    break;
            }

            bool? bestfit = null;

            if (imp.Attributes.HasFlag(MethodImportAttributes.BestFitMappingEnable))
                bestfit = true;
            else if(imp.Attributes.HasFlag(MethodImportAttributes.BestFitMappingDisable))
                bestfit = false;

            PInvokeParams ret = new PInvokeParams(
                modulename,funcname,cs,
                imp.Attributes.HasFlag(MethodImportAttributes.ExactSpelling),
                imp.Attributes.HasFlag(MethodImportAttributes.SetLastError),
                bestfit,conv);

            return ret;
        }

        public override Reflection.LocalVariable[] GetLocalVariables()
        {
            byte[] sig = this.GetLocalVarSignature();

            return Reflection.LocalVariable.ReadSignature(sig, this.TokenResolver, this);
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                ParameterHandleCollection phc = this.mdef.GetParameters();

                foreach (ParameterHandle ph in phc)
                {
                    Parameter p = this.assembly.MetadataReader.GetParameter(ph);

                    if (p.Attributes.HasFlag(ParameterAttributes.Retval) || p.SequenceNumber == 0)
                    {
                        CustomAttributeHandleCollection coll = p.GetCustomAttributes();
                        object[] attrs = Utils.ReadCustomAttributes(coll, this.ReturnType, this.assembly);
                        return new AttributesCollection(attrs);
                    }
                }

                return AttributesCollection.Empty;
            }
        }
    }
}
