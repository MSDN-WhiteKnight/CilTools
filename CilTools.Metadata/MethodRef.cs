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

namespace CilTools.Metadata
{
    class MethodRef : MethodInfo, ICustomMethod
    {
        MetadataAssembly assembly;
        MemberReferenceHandle mrefh;
        MemberReference mref;
        Signature sig;
        MethodBase impl;
        Type decltype;

        internal MethodRef(MemberReference m, MemberReferenceHandle mh, MetadataAssembly owner)
        {
            Debug.Assert(m.GetKind() == MemberReferenceKind.Method, "MemberReference passed to MethodRef ctor should be a method");

            this.assembly = owner;
            this.mref = m;
            this.mrefh = mh;
            
            //init declaring type
            this.decltype = GetRefDeclaringType(this.assembly, this, mref.Parent);
            
            //read signature
            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mref.Signature);
            GenericContext gctx = GenericContext.Create(this.decltype, this);
            SignatureContext ctx = new SignatureContext(this.assembly, gctx);

            try
            {
                this.sig = Signature.ReadFromArray(sigbytes, ctx);
            }
            catch (NotSupportedException) { }
        }

        internal static MethodBase CreateReference(MemberReference m, MemberReferenceHandle mh, MetadataAssembly owner)
        {
            string name = owner.MetadataReader.GetString(m.Name);

            if (Utils.IsConstructorName(name)) return new Constructors.ConstructorRef(m, mh, owner);
            else return new MethodRef(m, mh, owner);
        }

        internal static Type GetRefDeclaringType(MetadataAssembly ass, MethodBase methodRef, EntityHandle eh) 
        {
            //get declaring type for method reference based on parent EntityHandle

            if (!eh.IsNil && eh.Kind == HandleKind.TypeReference)
            {
                return new TypeRef(
                    ass.MetadataReader.GetTypeReference((TypeReferenceHandle)eh), (TypeReferenceHandle)eh, ass
                    );
            }
            else if (!eh.IsNil && eh.Kind == HandleKind.TypeSpecification)
            {
                //TypeSpec is either complex type (array etc.) or generic instantiation

                TypeSpecification ts = ass.MetadataReader.GetTypeSpecification(
                    (TypeSpecificationHandle)eh
                    );

                TypeSpec encoded = TypeSpec.ReadFromArray(ass.MetadataReader.GetBlobBytes(ts.Signature),
                    ass, methodRef);

                if (encoded != null) return encoded.Type;
                else return UnknownType.Value;
            }
            else if (!eh.IsNil && eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = ass.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)eh);
                TypeDefinitionHandle tdefh = mdef.GetDeclaringType();

                if (!tdefh.IsNil) return ass.GetTypeDefinition(tdefh);
                else return UnknownType.Value;
            }
            else return UnknownType.Value;
        }

        /// <summary>
        /// Core logic that fetches method definition corresponding to specified method reference 
        /// (shared between MethodRef amd ConstructorRef).
        /// </summary>
        internal static MethodBase ResolveMethodRef(MetadataAssembly ass, Type et, MethodBase methodRef, Signature sig) 
        {
            Type t = ass.AssemblyReader.LoadType(et);

            if (t == null) return null;

            MemberInfo[] members = t.GetMember(methodRef.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );

            //if there's only one method, pick it
            if (members.Length == 1 && members[0] is MethodBase)
            {
                return (MethodBase)members[0];
            }

            //if there are multiple methods with the same name, match by signature
            ParameterInfo[] pars_match;
            bool isstatic_match = false;
            int genargs_match = 0;

            if (sig != null)
            {
                pars_match = Utils.GetParametersFromSignature(sig, methodRef);
                isstatic_match = !sig.HasThis;
                genargs_match = sig.GenericArgsCount;
            }
            else
            {
                pars_match = new ParameterInfo[0];
            }
            
            bool match;

            for (int i = 0; i < members.Length; i++)
            {
                if (!(members[i] is MethodBase)) continue;

                MethodBase m = (MethodBase)members[i];
                ParameterInfo[] pars_i = m.GetParameters();

                if (m.IsStatic != isstatic_match) continue;

                if (pars_i.Length != pars_match.Length) continue;

                //compare generic args count
                Type[] ga = m.GetGenericArguments();
                int genargs = 0;

                if (ga != null) genargs = ga.Length;
                else genargs = 0;

                if (genargs != genargs_match) continue;

                //compare parameter types
                match = true;

                for (int j = 0; j < pars_i.Length; j++)
                {
                    Type pt1 = pars_i[j].ParameterType;
                    Type pt2 = pars_match[j].ParameterType;
                    
                    if (!Utils.TypeEqualsSignature(pt1, pt2))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return m;
                }
            }//end for

            return null;
        }

        void LoadImpl()
        {
            //loads actual implementation method referenced by this instance

            if (this.impl != null) return;//already loaded
            if (this.assembly.AssemblyReader == null) return;

            Type et = this.DeclaringType;

            if (et == null) return;

            this.impl = ResolveMethodRef(this.assembly, et, this, this.sig);
        }

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
        public ITokenResolver TokenResolver
        {
            get 
            {
                if (this.impl != null) return ((ICustomMethod)this.impl).TokenResolver;
                else return this.assembly; 
            }
        }

        /// <inheritdoc/>
        public byte[] GetBytecode()
        {
            this.LoadImpl();

            if (this.impl != null) return ((ICustomMethod)this.impl).GetBytecode();
            else throw new CilParserException("Failed to load method implementation");
        }

        /// <inheritdoc/>
        public int MaxStackSize
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).MaxStackSize;
                else return 0;
            }
        }

        /// <inheritdoc/>
        public bool MaxStackSizeSpecified
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).MaxStackSizeSpecified;
                else return false;
            }
        }

        /// <inheritdoc/>
        public byte[] GetLocalVarSignature()
        {
            this.LoadImpl();

            if (this.impl != null) return ((ICustomMethod)this.impl).GetLocalVarSignature();
            else return new byte[] { };
        }

        /// <inheritdoc/>
        public ExceptionBlock[] GetExceptionBlocks()
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
        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Method;
            }
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
        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
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
        public bool InitLocals
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return ((ICustomMethod)this.impl).InitLocals;
                else return false;
            }
        }

        /// <inheritdoc/>
        public bool InitLocalsSpecified
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
                try
                {
                    this.LoadImpl();
                }
                catch (TypeLoadException) { }

                if (this.impl != null) return this.impl.IsGenericMethod;
                else return false;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        public override Type[] GetGenericArguments()
        {
            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            if (this.impl != null) return this.impl.GetGenericArguments();
            else return new Type[0];
        }

        public Reflection.LocalVariable[] GetLocalVariables()
        {
            byte[] sig = this.GetLocalVarSignature();

            return Reflection.LocalVariable.ReadSignature(sig, this.TokenResolver, this);
        }

        public MethodBase GetDefinition()
        {
            return null;
        }

        public PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }
    }
}
