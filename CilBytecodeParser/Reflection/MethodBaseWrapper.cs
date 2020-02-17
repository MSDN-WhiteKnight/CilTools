/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Globalization;

namespace CilBytecodeParser.Reflection
{
    internal class MethodBaseWrapper : CustomMethod
    {        
        MethodBase srcmethod;
        ITokenResolver resolver;
        int _stacksize;
        bool _hasstacksize;
        byte[] _code;
        byte[] _localssig;

        public MethodBaseWrapper(MethodBase mb) : base()
        {
            this.srcmethod = mb;
            this.resolver = CustomMethod.CreateResolver(mb);

            if (Types.IsDynamicMethod(srcmethod))
            {
                FieldInfo fieldGenerator = srcmethod.GetType().GetField("m_ilGenerator",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object valueGenerator = fieldGenerator.GetValue(srcmethod);
                FieldInfo field;

                if (valueGenerator != null)
                {

                    field = Types.ILGeneratorType.GetField("m_ILStream",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    byte[] ilbytes = (byte[])field.GetValue(valueGenerator);

                    field = Types.ILGeneratorType.GetField("m_length",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    int len = (int)field.GetValue(valueGenerator);

                    field = valueGenerator.GetType().GetField("m_methodBuilder",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    MethodBase val = (MethodBase)field.GetValue(valueGenerator);

                    byte[] il = new byte[len];
                    Array.Copy(ilbytes, il, len);
                    this._code = il;
                }
                else
                {
                    this._code = new byte[0];
                }

                FieldInfo fieldResolver = srcmethod.GetType().GetField("m_resolver",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object valueResolver = fieldResolver.GetValue(srcmethod);

                if (valueResolver != null)
                {
                    field = valueResolver.GetType().GetField("m_localSignature",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    byte[] sigbytes = (byte[])field.GetValue(valueResolver);
                    this._localssig = sigbytes;

                    field = valueResolver.GetType().GetField("m_stackSize",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    this._stacksize = (int)field.GetValue(valueResolver);
                    this._hasstacksize = true;
                }
                else
                {
                    this._localssig = new byte[0];
                    this._hasstacksize = false;
                }
            }
            else
            {
                MethodBody body = srcmethod.GetMethodBody();

                if (body != null)
                {
                    this._code = body.GetILAsByteArray();
                    int token = body.LocalSignatureMetadataToken;

                    if (token == 0) this._localssig = new byte[0];
                    else this._localssig = this.resolver.ResolveSignature(token);

                    this._stacksize = body.MaxStackSize;
                    this._hasstacksize = true;
                }
                else
                {
                    this._code = new byte[0];
                    this._localssig = new byte[0];
                    this._hasstacksize = false;
                }
            }
        }

        public override MethodAttributes Attributes {get{ return srcmethod.Attributes;}}

        public override RuntimeMethodHandle MethodHandle {get{ return  srcmethod.MethodHandle;}}

        public override Type DeclaringType {get{ return  srcmethod.DeclaringType;}}

        public override MemberTypes MemberType {get{ return  srcmethod.MemberType;}}

        public override string Name {get{ return  srcmethod.Name;}}

        public override Type ReflectedType {get{ return  srcmethod.ReflectedType;}}
        
        public override object[] GetCustomAttributes(bool inherit)
        {
            return srcmethod.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return srcmethod.GetCustomAttributes(attributeType,inherit);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return srcmethod.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return srcmethod.GetParameters();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            return srcmethod.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return srcmethod.IsDefined(attributeType,inherit);
        }

        public override bool IsGenericMethod
        {
            get
            {
                return srcmethod.IsGenericMethod;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return srcmethod.GetGenericArguments();
        }

        public override ITokenResolver TokenResolver
        {
            get
            {
                return this.resolver;
            }
        }
        
        
        public override byte[] GetBytecode()
        {
            return this._code;
        }

        public override byte[] GetLocalVarSignature()
        {
            return this._localssig;
        }

        public override int MaxStackSize
        {
            get { return this._stacksize; }
        }

        public override bool MaxStackSizeSpecified
        {
            get { return this._hasstacksize; }
        }
    }
}
