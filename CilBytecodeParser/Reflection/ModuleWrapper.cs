using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    public class ModuleWrapper
    {
        MethodBase srcmethod;
        object resolver;
        MethodBase mbResolveToken;
        MethodBase mbGetStringLiteral;
        MethodBase mbResolveSignature;
        MethodBase mbGetTypeFromHandleUnsafe;                
        MethodBase mbGetMethodBase;        
        MethodBase mbGetFieldInfo;
        ConstructorInfo ciRuntimeMethodHandle;
        ConstructorInfo ciRuntimeFieldInfoStub;

        public static ModuleWrapper Create(MethodBase mb)
        {
            ModuleWrapper res = new ModuleWrapper();
            res.srcmethod = mb;

            if (mb is DynamicMethod)
            {
                Type tRuntimeType = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeType");
                Type tRMHI = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
                Type tRFIS = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldInfoStub");

                FieldInfo field = res.srcmethod.GetType().GetField("m_resolver",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                res.resolver = field.GetValue(res.srcmethod);

                res.mbResolveToken = res.resolver.GetType().GetMethod(
                    "ResolveToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                res.mbGetStringLiteral = res.resolver.GetType().GetMethod(
                    "GetStringLiteral", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                res.mbResolveSignature = res.resolver.GetType().GetMethod(
                    "ResolveSignature", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                res.mbGetTypeFromHandleUnsafe = typeof(Type).GetMethod(
                    "GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null
                    );

                if (tRMHI != null)
                {

                    res.mbGetMethodBase = tRuntimeType.GetMethod(
                        "GetMethodBase", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { tRuntimeType, tRMHI }, null
                        );

                    res.ciRuntimeMethodHandle = tRMHI.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null
                        );
                }

                if (tRFIS != null)
                {

                    res.ciRuntimeFieldInfoStub = tRFIS.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(object) }, null
                        );
                }

                Type tIRuntimeFieldInfo = typeof(RuntimeTypeHandle).Assembly.GetType("System.IRuntimeFieldInfo");

                if (tIRuntimeFieldInfo != null)
                {
                    res.mbGetFieldInfo = tRuntimeType.GetMethod(
                        "GetFieldInfo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new[] { tRuntimeType, tIRuntimeFieldInfo }, null
                        );
                }
            }

            return res;
        }

        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (srcmethod is DynamicMethod)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                return (Type)mbGetTypeFromHandleUnsafe.Invoke(null, new object[] { typeHandle });
            }
            else return srcmethod.Module.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (srcmethod is DynamicMethod)
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
                    else t = (Type)mbGetTypeFromHandleUnsafe.Invoke(null, new object[] { typeHandle });

                    return (MethodBase)mbGetMethodBase.Invoke(
                        null, new object[] { t, ciRuntimeMethodHandle.Invoke(new object[] { methodHandle }) }
                        );
                }
                else
                {
                    object ptr = mbResolveToken.Invoke(resolver, new object[]{metadataToken});
                    
                    return null;
                }
            }
            else return srcmethod.Module.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (srcmethod is DynamicMethod)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                Type t;

                if (typeHandle == IntPtr.Zero) t = null;
                else t = (Type)mbGetTypeFromHandleUnsafe.Invoke(null, new object[] { typeHandle });

                return (FieldInfo)mbGetFieldInfo.Invoke(
                    null, new object[] { t, ciRuntimeFieldInfoStub.Invoke(new object[] { fieldHandle, null }) }
                    );
            }
            else return srcmethod.Module.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (srcmethod is DynamicMethod)
            {
                IntPtr typeHandle, methodHandle, fieldHandle;
                object[] args = new object[] { metadataToken, new IntPtr(), new IntPtr(), new IntPtr() };
                mbResolveToken.Invoke(resolver, args);
                typeHandle = (IntPtr)args[1];
                methodHandle = (IntPtr)args[2];
                fieldHandle = (IntPtr)args[3];
                Type t;

                if (typeHandle == IntPtr.Zero) t = null;
                else t = (Type)mbGetTypeFromHandleUnsafe.Invoke(null, new object[] { typeHandle });

                if (methodHandle != IntPtr.Zero)
                {
                    return (MethodBase)mbGetMethodBase.Invoke(null, new[] { t, ciRuntimeMethodHandle.Invoke(new object[] { methodHandle }) });
                }

                if (fieldHandle != IntPtr.Zero)
                {
                    return (FieldInfo)mbGetFieldInfo.Invoke(null, new[] { t, ciRuntimeFieldInfoStub.Invoke(new object[] { fieldHandle, null }) });
                }

                if (typeHandle != IntPtr.Zero)
                {
                    return (Type)mbGetTypeFromHandleUnsafe.Invoke(null, new object[] { typeHandle });
                }

                throw new NotSupportedException("Unknown member type");
            }
            else return srcmethod.Module.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public byte[] ResolveSignature(int metadataToken)
        {
            if (srcmethod is DynamicMethod)
            {
                return (byte[])mbResolveSignature.Invoke(resolver, new object[] { metadataToken, 0 });
            }
            else return srcmethod.Module.ResolveSignature(metadataToken);
        }

        public string ResolveString(int metadataToken)
        {
            if (srcmethod is DynamicMethod) return (string)mbGetStringLiteral.Invoke(resolver, new object[] { metadataToken });
            else return srcmethod.Module.ResolveString(metadataToken);
        }
    }
}
