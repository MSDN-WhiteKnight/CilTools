/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    /// <summary>
    /// Represents information about a dynamic method in the external CLR instance
    /// </summary>
    class ClrDynamicMethod : CustomMethod
    {
        ClrObject method;
        ClrObject ilgen;
        DynamicMethodsAssembly owner;
        byte[] _code;
        ExceptionBlock[] _blocks;
        DynamicResolver _resolver;

        Dictionary<int,MemberInfo> GetDynamicTokenTable()
        {
            Dictionary<int, MemberInfo> table = new Dictionary<int, MemberInfo>();

            if (method.Type.GetFieldByName("m_resolver") == null) return table;

            ClrObject resolver = method.GetObjectField("m_resolver");

            if(resolver.IsNull) return table;

            ClrObject scope = resolver.GetObjectField("m_scope");
            ClrObject dtokens = scope.GetObjectField("m_tokens");
            ClrObject items = dtokens.GetObjectField("_items");
            ClrType arrtype = items.Type;
            
            int len = arrtype.GetArrayLength(items.Address);            

            ClrRuntime r = items.Type.Module.Runtime;
            for (int i = 0; i < len; i++)
            {
                //get array element (object reference) address
                ulong addr = items.Type.GetArrayElementAddress(items, i);

                //get object address from reference
                ulong obj_addr;
                bool res = r.ReadPointer(addr, out obj_addr);
                if (res == false) throw new ApplicationException("Failed to read memory");

                if (obj_addr == 0) continue;

                ClrType t = arrtype.Heap.GetObjectType(obj_addr);
                string tn = t.Name;
                ClrObject o = new ClrObject(obj_addr, t);

                if (String.Equals(tn, "System.RuntimeMethodHandle", StringComparison.InvariantCulture))
                {
                    ClrObject val = o.GetObjectField("m_value");
                    //System.Reflection.RuntimeMethodInfo                    
                    
                    //get runtime method handle
                    long handle = val.GetField<long>("m_handle");
                    ClrMethod m = r.GetMethodByHandle((ulong)handle);
                    MethodBase mb = null;
                    ClrAssemblyInfo ass = owner.AssemblyReader.Read(m.Type.Module);

                    //try to resolve existing MethodBase by (static) token
                    mb = ass.ResolveMethod((int)m.MetadataToken);

                    //if failed, construct new ClrMethodInfo
                    if (mb == null) mb = new ClrMethodInfo(m, new ClrTypeInfo(m.Type, ass));

                    //construct dynamic token
                    int dtoken = 0x06000000 | i;

                    table[dtoken] = mb;
                }

                //System.Reflection.Emit.GenericFieldInfo
            }

            return table;
        }

        static int[] ReadIntArray(ClrObject obj)
        {
            int len = obj.Type.GetArrayLength(obj);
            int[] arr = new int[len];

            for (int j = 0; j < len; j++)
            {
                arr[j] = (int)obj.Type.GetArrayElementValue(obj.Address, j);
            }

            return arr;
        }

        ExceptionBlock[] ReadExceptionBlocks()
        {
            ClrObject exceptions;
            if (method.Type.Name.EndsWith("DynamicMethod"))
            {
                ClrObject resolver = method.GetObjectField("m_resolver");
                if (resolver != null && !resolver.IsNull) exceptions = resolver.GetObjectField("m_exceptions");
                else exceptions = new ClrObject();
            }
            else
            {
                exceptions = method.GetObjectField("m_exceptions");
            }

            if (exceptions.IsNull) return new ExceptionBlock[0];

            int len = exceptions.Type.GetArrayLength(exceptions);
            ClrRuntime r = exceptions.Type.Module.Runtime;
            List<ExceptionBlock> blocks = new List<ExceptionBlock>(len * 2);
            byte[] code = this.GetBytecode();

            for (int i = 0; i < len; i++)
            {
                //get array element (object reference) address
                ulong addr = exceptions.Type.GetArrayElementAddress(exceptions, i);
                
                //get object address from reference
                ulong obj_addr;
                bool res = r.ReadPointer(addr, out obj_addr);
                if (res == false) throw new ApplicationException("Failed to read memory");

                ClrObject o = new ClrObject(obj_addr, exceptions.Type.ComponentType);

                if (o.Type.GetFieldByName("m_startAddr") == null) continue;
                if (o.Type.GetFieldByName("m_endAddr") == null) continue;

                int startaddr = o.GetField<int>("m_startAddr");
                int endAddr = o.GetField<int>("m_endAddr");

                ClrObject obj_catchAddr = o.GetObjectField("m_catchAddr");
                int[] catchAddr = ReadIntArray(obj_catchAddr);

                ClrObject obj_catchEndAddr = o.GetObjectField("m_catchEndAddr");
                int[] catchEndAddr = ReadIntArray(obj_catchEndAddr);

                ClrObject obj_filterAddr = o.GetObjectField("m_filterAddr");
                int[] filterAddr = ReadIntArray(obj_filterAddr);

                ClrObject obj_type = o.GetObjectField("m_type");
                int[] type = ReadIntArray(obj_type);

                //get array of catch types
                ClrObject catchClass = o.GetObjectField("m_catchClass");
                int catchClass_len = catchClass.Type.GetArrayLength(catchClass);
                Type[] catchTypes = new Type[catchClass_len];

                for (int j = 0; j < len; j++)
                {
                    //get array element (object reference) address
                    addr = catchClass.Type.GetArrayElementAddress(catchClass, j);

                    //get object address from reference
                    res = r.ReadPointer(addr, out obj_addr);
                    if (res == false) throw new ApplicationException("Failed to read memory");

                    //get underlying type (subclass of System.Type) from object address
                    ClrType real_type = catchClass.Type.Heap.GetObjectType(obj_addr);

                    if (real_type==null || !real_type.IsRuntimeType)
                    {
                        catchTypes[j] = UnknownType.Value;
                        continue;
                    }

                    //get ClrType for runtime type
                    ClrType rt=real_type.GetRuntimeType(obj_addr);

                    if (rt == null)
                    {
                        catchTypes[j] = UnknownType.Value;
                        continue;
                    }

                    //get Type object for the given catch type
                    ClrAssemblyInfo ass = owner.AssemblyReader.Read(rt.Module);
                    Type t=null;

                    //try to resolve existing type by token
                    t = ass.ResolveType((int)rt.MetadataToken);

                    //if failed, construct new ClrTypeInfo
                    if (t==null) t = new ClrTypeInfo(rt, ass);
                    catchTypes[j] = t;
                }

                for (int j = 0; j < type.Length; j++)
                {
                    int length = endAddr - startaddr;
                    int handler_length = catchEndAddr[j] - catchAddr[j];
                    int filter_offset = filterAddr[j];

                    if (handler_length <= 0) continue;

                    if (filter_offset < 0 || filter_offset > code.Length) filter_offset = 0;

                    blocks.Add(new ExceptionBlock(
                                (ExceptionHandlingClauseOptions)type[j], startaddr, length,
                                catchTypes[j], catchAddr[j],
                                handler_length, filter_offset
                                ));
                }
            }

            return blocks.ToArray();
        }

        public ClrDynamicMethod(ClrObject m, DynamicMethodsAssembly ass)
        {
            this.method = m;
            ClrObject ilg = m.GetObjectField("m_ilGenerator");
            this.ilgen = ilg;
            this.owner = ass;
        }

        public override Type ReturnType
        {
            get { return UnknownType.Value; }
        }

        public override ITokenResolver TokenResolver
        {
            get 
            {
                if (this._resolver == null)
                {
                    Dictionary<int, MemberInfo> table = GetDynamicTokenTable();
                    this._resolver = new DynamicResolver(table);
                }

                return this._resolver; 
            }
        }

        public override byte[] GetBytecode()
        {
            if (this._code == null)
            {
                int size = ilgen.GetField<int>("m_length");
                ClrObject stream = ilgen.GetObjectField("m_ILStream");
                ClrType type = stream.Type;
                ulong obj = stream.Address;
                int len = type.GetArrayLength(obj);
                byte[] il = new byte[size];
                for (int i = 0; i < size; i++) il[i] = (byte)type.GetArrayElementValue(obj, i);
                this._code = il;
            }

            return this._code;
        }

        public override int MaxStackSize
        {
            get { return 0; }
        }

        public override bool MaxStackSizeSpecified
        {
            get { return false; }
        }

        public override byte[] GetLocalVarSignature()
        {
            throw new NotImplementedException();
        }

        public override ExceptionBlock[] GetExceptionBlocks()
        {
            if (this._blocks == null)
            {
                this._blocks = ReadExceptionBlocks();
            }

            return this._blocks;
        }

        public override MethodAttributes Attributes
        {
            get 
            {
                MethodAttributes ret = (MethodAttributes)0;
                ret |= MethodAttributes.Public;
                ret |= MethodAttributes.Static;
                return ret;
            }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            return new ParameterInfo[] { };
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, 
            System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return this.owner.ChildType; }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new object[] { };
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[] { };
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override MemberTypes MemberType
        {
            get { return MemberTypes.Method; }
        }

        public override string Name
        {
            get 
            {
                string mname;
                try
                {                    
                    if (method.Type.Name.EndsWith("DynamicMethod"))
                    {
                        ClrObject rtdm = method.GetObjectField("m_dynMethod");
                        mname = rtdm.GetStringField("m_name");
                    }
                    else
                    {
                        mname = method.GetStringField("m_strName");
                    }
                }
                catch (ArgumentException)
                {
                    mname = "<UnknownDynamicMethod>";
                }
                return mname;
            }
        }

        public override Type ReflectedType
        {
            get { return UnknownType.Value; }
        }

        public override bool InitLocals
        {
            get { throw new NotImplementedException(); }
        }

        public override bool InitLocalsSpecified
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                return 0;
            }
        }
    }
}
