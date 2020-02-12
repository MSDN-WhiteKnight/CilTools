using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    public class ModuleWrapperDynamic : ModuleWrapper
    {
        MethodBase srcmethod;
        object resolver;
        MethodBase mbResolveToken;
        MethodBase mbGetStringLiteral;
        MethodBase mbResolveSignature;
        MethodBase mbGetTypeFromHandle;
        MethodBase mbGetMethodBase;
        MethodBase mbGetFieldInfo;
        ConstructorInfo ciRuntimeMethodHandle;
        ConstructorInfo ciRuntimeFieldInfo;

        public ModuleWrapperDynamic(MethodBase mb):base(mb)
        {
            this.srcmethod = mb;
            Type tRuntimeType = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeType");

            FieldInfo field = this.srcmethod.GetType().GetField("m_resolver",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            this.resolver = field.GetValue(this.srcmethod);

            this.mbResolveToken = this.resolver.GetType().GetMethod(
                "ResolveToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            this.mbGetStringLiteral = this.resolver.GetType().GetMethod(
                "GetStringLiteral", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            this.mbResolveSignature = this.resolver.GetType().GetMethod(
                "ResolveSignature", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            if (Environment.Version.Major >= 4)
            {
                Type tRMHI = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
                Type tRFIS = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldInfoStub");
                Type tIRuntimeFieldInfo = typeof(RuntimeTypeHandle).Assembly.GetType("System.IRuntimeFieldInfo");

                this.mbGetTypeFromHandle = typeof(Type).GetMethod(
                "GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null
                );

                if (tIRuntimeFieldInfo != null)
                {
                    this.mbGetFieldInfo = tRuntimeType.GetMethod(
                        "GetFieldInfo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new[] { tRuntimeType, tIRuntimeFieldInfo }, null
                        );
                }

                if (tRMHI != null)
                {

                    this.mbGetMethodBase = tRuntimeType.GetMethod(
                        "GetMethodBase", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { tRuntimeType, tRMHI }, null
                        );

                    this.ciRuntimeMethodHandle = tRMHI.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null
                        );
                }

                if (tRFIS != null)
                {

                    this.ciRuntimeFieldInfo = tRFIS.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(object) }, null
                        );
                }
            }
            else
            {
                this.mbGetTypeFromHandle = typeof(Type).GetMethod(
                "GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(System.RuntimeTypeHandle) }, null
                );

                this.mbGetFieldInfo = tRuntimeType.GetMethod(
                    "GetFieldInfo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(System.RuntimeFieldHandle) }, null
                        );

                this.mbGetMethodBase = tRuntimeType.GetMethod(
                        "GetMethodBase", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new Type[] { typeof(System.RuntimeMethodHandle) }, null
                        );
                this.ciRuntimeMethodHandle = typeof(System.RuntimeMethodHandle).GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(void*) }, null
                        );
                this.ciRuntimeFieldInfo = typeof(System.RuntimeFieldHandle).GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(void*) }, null
                        );
            }
        }

        public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (Environment.Version.Major >= 4)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                return (Type)mbGetTypeFromHandle.Invoke(null, new object[] { typeHandle });
            }
            else
            {
                object ptr = mbResolveToken.Invoke(resolver, new object[] { metadataToken });
                ConstructorInfo ci = typeof(RuntimeTypeHandle).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(void*) }, null
                    );
                RuntimeTypeHandle h = (RuntimeTypeHandle)ci.Invoke(new object[] { ptr });
                return (Type)mbGetTypeFromHandle.Invoke(null, new object[] { h });
            }
        }

        public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (Environment.Version.Major >= 4)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                Type t;

                if (typeHandle == IntPtr.Zero) t = null;
                else t = (Type)mbGetTypeFromHandle.Invoke(null, new object[] { typeHandle });

                return (MethodBase)mbGetMethodBase.Invoke(
                    null, new object[] { t, ciRuntimeMethodHandle.Invoke(new object[] { methodHandle }) }
                    );
            }
            else
            {
                object ptr = mbResolveToken.Invoke(resolver, new object[] { metadataToken });
                RuntimeMethodHandle h = (RuntimeMethodHandle)ciRuntimeMethodHandle.Invoke(new object[] { ptr });
                MethodBase mbResult = (MethodBase)mbGetMethodBase.Invoke(null, new object[] { h });
                return mbResult;
            }
        }

        public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (Environment.Version.Major >= 4)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                Type t;

                if (typeHandle == IntPtr.Zero) t = null;
                else t = (Type)mbGetTypeFromHandle.Invoke(null, new object[] { typeHandle });

                return (FieldInfo)mbGetFieldInfo.Invoke(
                    null, new object[] { t, ciRuntimeFieldInfo.Invoke(new object[] { fieldHandle, null }) }
                    );
            }
            else
            {
                object ptr = mbResolveToken.Invoke(resolver, new object[] { metadataToken });
                RuntimeFieldHandle h = (RuntimeFieldHandle)ciRuntimeFieldInfo.Invoke(new object[] { ptr });
                FieldInfo fiResult = (FieldInfo)mbGetFieldInfo.Invoke(null, new object[] { h });
                return fiResult;
            }
        }

        public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {

            if (Environment.Version.Major >= 4)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                Type t;

                if (typeHandle == IntPtr.Zero) t = null;
                else t = (Type)mbGetTypeFromHandle.Invoke(null, new object[] { typeHandle });

                if (methodHandle != IntPtr.Zero)
                {
                    return (MethodBase)mbGetMethodBase.Invoke(null, new[] { t, ciRuntimeMethodHandle.Invoke(new object[] { methodHandle }) });
                }

                if (fieldHandle != IntPtr.Zero)
                {
                    return (FieldInfo)mbGetFieldInfo.Invoke(null, new[] { t, ciRuntimeFieldInfo.Invoke(new object[] { fieldHandle, null }) });
                }

                if (typeHandle != IntPtr.Zero)
                {
                    return (Type)mbGetTypeFromHandle.Invoke(null, new object[] { typeHandle });
                }

                throw new NotSupportedException("Unknown member type");
            }
            else
            {
                MemberInfo res = this.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
                if (res != null) return res;

                res = this.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
                if (res != null) return res;

                res = this.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
                return res;
            }
        }

        public override byte[] ResolveSignature(int metadataToken)
        {
            return (byte[])mbResolveSignature.Invoke(resolver, new object[] { metadataToken, 0 });
        }

        public override string ResolveString(int metadataToken)
        {
            return (string)mbGetStringLiteral.Invoke(resolver, new object[] { metadataToken });
        }
    }
}
