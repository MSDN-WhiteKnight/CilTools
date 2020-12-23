/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    /// <summary>
    /// Resolves metadata tokens in the context of specified dynamic method
    /// </summary>
    internal class ModuleWrapperDynamic : ModuleWrapper
    {        
        object resolver;
        MethodBase mbResolveToken;
        MethodBase mbGetStringLiteral;
        MethodBase mbResolveSignature;
        MethodBase mbGetTypeFromHandle;
        MethodBase mbGetMethodBase;
        MethodBase mbGetFieldInfo;
        ConstructorInfo ciRuntimeMethodHandle;
        ConstructorInfo ciRuntimeFieldInfo;

        static bool s_newruntime; //indicates that current runtime is .NET Framework 4.x, .NET Core or .NET 5

        static bool IsOnNetFramework()
        {
            string corelib = typeof(object).Assembly.GetName().Name;
            return String.Equals(corelib, "mscorlib",StringComparison.InvariantCulture);
        }

        static bool IsOnNetCore()
        {
            string corelib = typeof(object).Assembly.GetName().Name;
            return String.Equals(corelib, "System.Private.CoreLib", StringComparison.InvariantCulture);
        }

        static ModuleWrapperDynamic()
        {
            if (IsOnNetFramework() && Environment.Version.Major >= 4) s_newruntime = true;
            else if(IsOnNetCore()) s_newruntime = true;
            else s_newruntime = false;
        }

        public ModuleWrapperDynamic(MethodBase mb):base(mb)
        {
            this.srcmethod = mb;
            Type tRuntimeType = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeType");

            FieldInfo field = this.srcmethod.GetType().GetField("m_resolver",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            if (field == null) return;

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

            if (s_newruntime)
            {
                Type tRMHI = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
                Type tRFIS = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldInfoStub");
                Type tIRuntimeFieldInfo = typeof(RuntimeTypeHandle).Assembly.GetType("System.IRuntimeFieldInfo");

                this.mbGetTypeFromHandle = typeof(Type).GetMethod(
                "GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, 
                new[] { typeof(IntPtr) }, null
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
                        "GetMethodBase", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, 
                        new Type[] { tRuntimeType, tRMHI }, null
                        );

                    this.ciRuntimeMethodHandle = tRMHI.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null
                        );
                }

                if (tRFIS != null)
                {

                    this.ciRuntimeFieldInfo = tRFIS.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, 
                        new[] { typeof(IntPtr), typeof(object) }, null
                        );
                }
            }
            else
            {
                this.mbGetTypeFromHandle = typeof(Type).GetMethod(
                "GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, 
                new[] { typeof(System.RuntimeTypeHandle) }, null
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
            if (this.resolver == null) return base.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);

            if (s_newruntime)
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
            if (this.resolver == null) return base.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);

            if (s_newruntime)
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

        object GetRFIS(IntPtr fieldHandle)
        {
            //Create instance of System.FuntimeFieldInfoStub

            object stub;
            if (this.ciRuntimeFieldInfo == null) //.NET Core or .NET 5
            {
                Type tRFIS = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldInfoStub");
                stub = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(tRFIS);
                FieldInfo fi_m_fieldHandle;
                fi_m_fieldHandle = tRFIS.GetField(
                    "m_fieldHandle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );

                Type tRFHI = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldHandleInternal");
                object oRFHI = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(tRFHI);
                FieldInfo fi_m_handle = tRFHI.GetField(
                    "m_handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );
                fi_m_handle.SetValue(oRFHI, fieldHandle);
                fi_m_fieldHandle.SetValue(stub, oRFHI);
            }
            else
            {
                //.NET Framework 4.x
                stub = ciRuntimeFieldInfo.Invoke(new object[] { fieldHandle, null });
            }

            return stub;
        }

        public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this.resolver == null) return base.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);

            if (s_newruntime)
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

                object stub = this.GetRFIS(fieldHandle);

                return (FieldInfo)mbGetFieldInfo.Invoke(null, new[] { t, stub });
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
            if (this.resolver == null) return base.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);

            if (s_newruntime)
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
                    return (MethodBase)mbGetMethodBase.Invoke(
                        null, new[] { t, ciRuntimeMethodHandle.Invoke(new object[] { methodHandle }) }
                        );
                }
                
                if (fieldHandle != IntPtr.Zero)
                {
                    object stub = this.GetRFIS(fieldHandle);

                    return (FieldInfo)mbGetFieldInfo.Invoke(
                        null, new[] { t,  stub}
                        );
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
            if (this.resolver == null) return base.ResolveSignature(metadataToken);

            return (byte[])mbResolveSignature.Invoke(resolver, new object[] { metadataToken, 0 });
        }

        public override string ResolveString(int metadataToken)
        {
            if (this.resolver == null) return base.ResolveString(metadataToken);

            return (string)mbGetStringLiteral.Invoke(resolver, new object[] { metadataToken });
        }
    }
}
