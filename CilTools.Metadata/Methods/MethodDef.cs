/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
using CilTools.Metadata.PortableExecutable;
using CilTools.Reflection;
using CilTools.Reflection.PortableExecutable;

namespace CilTools.Metadata.Methods
{
    class MethodDef : MdMethodInfoBase, ICustomMethod, IReflectionInfo
    {
        MethodDefinitionHandle mdefh;
        MethodDefinition mdef;
        MethodBodyBlock mb;
        VTableSlot? vtSlot;

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

                return Utils.CallingConventionFromSig(this.sig);
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
        
        MethodInfo[] GetExplicitlyImplementedMethods()
        {
            TypeDefinitionHandle tdh = this.mdef.GetDeclaringType();

            if (tdh.IsNil) return null;

            //get explicit method implementations table for the declaring type
            TypeDefinition tdef = this.assembly.MetadataReader.GetTypeDefinition(tdh);
            MethodImplementationHandleCollection coll = tdef.GetMethodImplementations();
            List<MethodInfo> decls = new List<MethodInfo>(coll.Count);

            //find implementations for this method
            foreach (MethodImplementationHandle h in coll)
            {
                if (h.IsNil) continue;

                MethodImplementation impl = this.assembly.MetadataReader.GetMethodImplementation(h);
                MethodInfo decl = this.assembly.GetMethodByHandle(impl.MethodDeclaration);
                Debug.Assert(decl != null);
                
                MethodInfo body = this.assembly.GetMethodByHandle(impl.MethodBody);
                Debug.Assert(body != null);
                
                if (body.MetadataToken == this.MetadataToken) decls.Add(decl);
            }

            return decls.ToArray();
        }

        /// <summary>
        /// Gets method's VTable slot address if this method is a native C++ virtual method (used with C++/CLI).
        /// If the method does not have associated VTable slot, returns <see cref="VTableSlot"/> value with
        /// negative indices.
        /// </summary>
        VTableSlot GetVTableSlot()
        {
            //cached value
            if (this.vtSlot.HasValue) return this.vtSlot.Value;

            VTable[] tables = this.assembly.GetVTables();

            for (int i = 0; i < tables.Length; i++)
            {
                for (int j = 0; j < tables[i].SlotsCount; j++)
                {
                    int val = tables[i].GetSlotValueInt32(j);

                    if (val == this.MetadataToken)
                    {
                        this.vtSlot = new VTableSlot(i, j); //cache in instance field
                        return this.vtSlot.Value;
                    }
                }
            }

            //not found
            this.vtSlot = new VTableSlot(-1, -1); //cache in instance field
            return this.vtSlot.Value;
        }

        public object GetReflectionProperty(int id)
        {
            if (id == ReflectionProperties.ExplicitlyImplementedMethods)
            {
                return this.GetExplicitlyImplementedMethods();
            }
            else if (id == ReflectionProperties.VTableEntry)
            {
                VTableSlot slot = this.GetVTableSlot();

                if (slot.TableIndex < 0)
                {
                    return string.Empty;
                }
                else
                {
                    return (slot.TableIndex + 1).ToString() + " : " + (slot.SlotIndex + 1).ToString();
                }
            }
            else if (id == ReflectionProperties.Signature)
            {
                return this.sig;
            }
            else
            {
                return null;
            }
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
