/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;


namespace CilTools.Metadata
{
    class ExternalMethod : CustomMethod
    {
        MetadataAssembly assembly;
        MemberReferenceHandle mrefh;
        MemberReference mref;
        Signature sig;

        internal ExternalMethod(MemberReference m, MemberReferenceHandle mh, MetadataAssembly owner)
        {
            Debug.Assert(m.GetKind() == MemberReferenceKind.Method, "MemberReference passed to ExternalMethod ctor should be a method");

            this.assembly = owner;
            this.mref = m;
            this.mrefh = mh;

            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mref.Signature);
            this.sig = new Signature(sigbytes, this.assembly);
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
            return new byte[0];
        }

        /// <inheritdoc/>
        public override int MaxStackSize
        {
            get
            {
                return 0;
            }
        }

        /// <inheritdoc/>
        public override bool MaxStackSizeSpecified
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetLocalVarSignature()
        {
            return new byte[] { }; //not implemented
        }

        /// <inheritdoc/>
        public override ExceptionBlock[] GetExceptionBlocks()
        {
            return new ExceptionBlock[] { }; //not implemented
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get
            {
                MethodAttributes ret = (MethodAttributes)0;
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
                EntityHandle eh = mref.Parent;

                if (!eh.IsNil && eh.Kind == HandleKind.TypeReference)
                {
                    return new ExternalType(assembly.MetadataReader.GetTypeReference((TypeReferenceHandle)eh), (TypeReferenceHandle)eh, this.assembly);
                }
                else return UnknownType.Value; 
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
        public override bool InitLocals
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool InitLocalsSpecified
        {
            get { return false; }
        }
    }
}

