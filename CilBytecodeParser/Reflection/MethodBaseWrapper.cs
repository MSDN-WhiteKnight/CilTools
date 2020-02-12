using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Globalization;

namespace CilBytecodeParser.Reflection
{
    internal class MethodBaseWrapper : MethodBase
    {        
        MethodBase srcmethod;
        ModuleWrapper module;

        public MethodBaseWrapper(MethodBase mb) : base()
        {
            this.srcmethod = mb;
            this.module = ModuleWrapper.Create(srcmethod);
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

        public ModuleWrapper ModuleWrapper
        {
            get
            {
                return this.module;
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

        public byte[] GetBytecode()
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
    }
}
