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

        public MethodBaseWrapper(MethodBase mb) : base()
        {
            this.srcmethod = mb;
            this.resolver = CustomMethod.CreateResolver(mb);
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
        
        byte[] GetBytecodeDynamic()
        {
            FieldInfo fieldGenerator = srcmethod.GetType().GetField("m_ilGenerator",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object valueGenerator = fieldGenerator.GetValue(srcmethod);

            if (valueGenerator == null) throw new NotSupportedException("Cannot get bytecode for this method");

            var field = Types.ILGeneratorType.GetField("m_ILStream",
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
            return il;
        }

        byte[] GetLocalVarSignatureDynamic()
        {
            FieldInfo fieldResolver = srcmethod.GetType().GetField("m_resolver",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object valueResolver = fieldResolver.GetValue(srcmethod);

            if (valueResolver == null) throw new NotSupportedException("Cannot get local variables for this method");

            var field = valueResolver.GetType().GetField("m_localSignature",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            byte[] sigbytes = (byte[])field.GetValue(valueResolver);

            return sigbytes;
        }

        public override byte[] GetBytecode()
        {
            if (Types.IsDynamicMethod(srcmethod))
            {
                return GetBytecodeDynamic();
            }
            else
            {
                MethodBody body = srcmethod.GetMethodBody();

                if (body == null) return new byte[0];
                else return body.GetILAsByteArray();
            }
        }

        public override byte[] GetLocalVarSignature()
        {
            if (Types.IsDynamicMethod(srcmethod))
            {
                return GetLocalVarSignatureDynamic();
            }
            else
            {
                MethodBody body = srcmethod.GetMethodBody();

                if (body == null) return new byte[0];

                int token = body.LocalSignatureMetadataToken;

                return this.resolver.ResolveSignature(token);
            }
        }
    }
}
