/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Reflection;
using Microsoft.Diagnostics.Runtime;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Runtime
{
    /// <summary>
    /// Represents information about the constructor in an external CLR instance
    /// </summary>
    public class ClrConstructorInfo : ConstructorInfo, ICustomMethod
    {
        ClrMethod method;
        ClrAssemblyInfo assembly;
        DataTarget target;
        ClrTypeInfo type;

        //backing dynamic method, if this is a method from dynamic module
        ClrDynamicMethod dynamicMethod = null;
        bool dynamicMethodInitialized = false;

        internal ClrConstructorInfo(ClrMethod m, ClrTypeInfo owner)
        {
            this.method = m;
            this.assembly = (ClrAssemblyInfo)owner.Assembly;
            this.type = owner;

            if (assembly != null) this.target = assembly.InnerModule.Runtime.DataTarget;
        }

        /// <summary>
        /// Gets the underlying ClrMD method object
        /// </summary>
        public ClrMethod InnerMethod { get { return this.method; } }

        /// <inheritdoc/>
        public Type ReturnType
        {
            get { return null; }
        }

        /// <inheritdoc/>
        public ITokenResolver TokenResolver
        {
            get { return this.assembly; }
        }

        ClrDynamicMethod FindDynamicMethod()
        {
            if (this.method.Type == null) return null;
            ClrModule module = this.method.Type.Module;
            if (module == null) return null;

            if (this.dynamicMethodInitialized)
            {
                return this.dynamicMethod;
            }

            //try to lookup dynamic method that backs up this 
            //method in dynamic module

            int token = 0;

            unchecked { token = (int)this.method.MetadataToken; }

            ClrDynamicMethod ret = this.assembly.AssemblyReader.GetDynamicAssemblyMethod(
                module.Address,
                token
                );

            this.dynamicMethod = ret;
            this.dynamicMethodInitialized = true;
            return ret;
        }

        /// <inheritdoc/>
        public byte[] GetBytecode()
        {
            byte[] il;
            int bytesread;
            ILInfo ildata = method.IL;

            if (ildata == null)
            {
                //P/Invoke methods does not have IL body
                if (this.method.IsPInvoke) return new byte[0];

                //try to lookup dynamic method that backs up this 
                //method in dynamic module
                ClrDynamicMethod dm = this.FindDynamicMethod();

                if (dm != null)
                {
                    il = dm.GetBytecode();
                    if (il != null) return il;
                }

                throw new CilParserException("Cannot read IL of the method " + method.Name);
            }
            else
            {
                il = new byte[ildata.Length];
                target.ReadProcessMemory(ildata.Address, il, ildata.Length, out bytesread);
                return il;
            }
        }

        /// <inheritdoc/>
        public int MaxStackSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public bool MaxStackSizeSpecified
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public byte[] GetLocalVarSignature()
        {
            return new byte[] { }; //not implemented
        }

        /// <inheritdoc/>
        public ExceptionBlock[] GetExceptionBlocks()
        {
            //P/Invoke methods does not have exception blocks
            if (this.method.IsPInvoke) return new ExceptionBlock[] { };

            //try to lookup dynamic method that backs up this 
            //method in dynamic module
            ClrDynamicMethod dm = this.FindDynamicMethod();

            if (dm != null)
            {
                return dm.GetExceptionBlocks();
            }
            else
            {
                return new ExceptionBlock[] { };
            }
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get
            {
                MethodAttributes ret = (MethodAttributes)0;
                if (method.IsAbstract) ret |= MethodAttributes.Abstract;
                if (method.IsFinal) ret |= MethodAttributes.Final;
                if (method.IsInternal) ret |= MethodAttributes.Assembly;
                if (method.IsPrivate) ret |= MethodAttributes.Private;
                if (method.IsProtected) ret |= MethodAttributes.Family;
                if (method.IsPublic) ret |= MethodAttributes.Public;
                if (method.IsStatic) ret |= MethodAttributes.Static;
                if (method.IsVirtual) ret |= MethodAttributes.Virtual;
                if (method.IsPInvoke) ret |= MethodAttributes.PinvokeImpl;
                if (method.IsSpecialName) ret |= MethodAttributes.SpecialName;
                if (method.IsRTSpecialName) ret |= MethodAttributes.RTSpecialName;
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
            return new ParameterInfo[] { };
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
        public override Type DeclaringType
        {
            get { return this.type; }
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
        public LocalVariable[] GetLocalVariables()
        {
            return new LocalVariable[0];
        }

        /// <inheritdoc/>
        public MethodBase GetDefinition()
        {
            return null;
        }

        /// <inheritdoc/>
        public PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        /// <inheritdoc/>
        public override MemberTypes MemberType
        {
            get { return MemberTypes.Constructor; }
        }

        /// <inheritdoc/>
        public override string Name
        {
            get { return method.Name; }
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
                return (int)method.MetadataToken;
            }
        }

        /// <inheritdoc/>
        public bool InitLocals
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public bool InitLocalsSpecified
        {
            get { return false; }
        }
    }
}
