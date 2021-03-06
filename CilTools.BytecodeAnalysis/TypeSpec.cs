﻿/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Globalization;
using CilTools.Reflection;
using CilTools.Syntax;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents signature element type as defined in ECMA-335
    /// </summary>
    public enum ElementType : byte //ECMA-335 II.23.1.16 Element types used in signatures
    {
        /// <summary>
        /// The absence of return value
        /// </summary>
        Void = 0x01,

        /// <summary>
        /// System.Boolean
        /// </summary>
        Boolean = 0x02,

        /// <summary>
        /// System.Char
        /// </summary>
        Char = 0x03,

        /// <summary>
        /// sbyte
        /// </summary>
        I1 = 0x04,

        /// <summary>
        /// byte
        /// </summary>
        U1 = 0x05,

        /// <summary>
        /// short
        /// </summary>
        I2 = 0x06,

        /// <summary>
        /// ushort
        /// </summary>
        U2 = 0x07,

        /// <summary>
        /// int
        /// </summary>
        I4 = 0x08,

        /// <summary>
        /// uint
        /// </summary>
        U4 = 0x09,

        /// <summary>
        /// long
        /// </summary>
        I8 = 0x0a,

        /// <summary>
        /// ulong
        /// </summary>
        U8 = 0x0b,

        /// <summary>
        /// float
        /// </summary>
        R4 = 0x0c,

        /// <summary>
        /// double
        /// </summary>
        R8 = 0x0d,

        /// <summary>
        /// Sytem.String
        /// </summary>
        String = 0x0e,

        /// <summary>
        /// Unmanaged pointer
        /// </summary>
        Ptr = 0x0f,  //Followed by type 

        /// <summary>
        /// Passed by reference
        /// </summary>
        ByRef = 0x10,  //Followed by type 

        /// <summary>
        /// Value type
        /// </summary>
        ValueType = 0x11,  //Followed by TypeDef or TypeRef token 

        /// <summary>
        /// Reference type
        /// </summary>
        Class = 0x12,  //Followed by TypeDef or TypeRef token 

        /// <summary>
        /// Generic parameter in a generic type definition
        /// </summary>
        Var = 0x13,  //represented as number (compressed unsigned integer) 

        /// <summary>
        /// Array
        /// </summary>
        Array = 0x14,  //type rank boundsCount bound1 … loCount lo1 … 

        /// <summary>
        /// Generic type instantiation
        /// </summary>
        GenericInst = 0x15,  //Followed by type type-arg-count  type-1 ... type-n 

        /// <summary>
        /// Passed by typed reference
        /// </summary>
        TypedByRef = 0x16,

        /// <summary>
        /// System.IntPtr
        /// </summary>
        I = 0x18,

        /// <summary>
        /// System.UIntPtr
        /// </summary>
        U = 0x19,

        /// <summary>
        /// Function pointer
        /// </summary>
        FnPtr = 0x1b,  //Followed by full method signature 

        /// <summary>
        /// System.Object
        /// </summary>
        Object = 0x1c, 

        /// <summary>
        /// Single-dimensional array with 0 lower bound
        /// </summary>
        SzArray = 0x1d,  

        /// <summary>
        /// Generic parameter in a generic method definition
        /// </summary>
        MVar = 0x1e, //Represented as number (compressed unsigned integer)
 
        /// <summary>
        /// Implemented within the CLI
        /// </summary>
        Internal = 0x21, 

        /// <summary>
        /// Modifier
        /// </summary>
        Modifier = 0x40, 

        /// <summary>
        /// Sentinel for vararg method signature
        /// </summary>
        Sentinel = 0x41 
    }

    /// <summary>
    /// Represents type specification, the set of type information stored in the signature, as defined by ECMA-335
    /// </summary>
    public class TypeSpec:Type,ITypeInfo //ECMA-335 II.23.2.12 Type
    {
        //ECMA-335 II.23.1.16 Element types used in signatures
        internal const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
        internal const byte ELEMENT_TYPE_CMOD_OPT = 0x20;
        internal const byte ELEMENT_TYPE_PINNED = 0x45;

        static Dictionary<byte, Type> _types = new Dictionary<byte, Type>
        {
            {(byte)CilTools.BytecodeAnalysis.ElementType.Void,typeof(void)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.Boolean,typeof(bool)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.Char,typeof(char)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.I1,typeof(sbyte)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.U1,typeof(byte)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.I2,typeof(short)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.U2,typeof(ushort)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.I4,typeof(int)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.U4,typeof(uint)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.I8,typeof(long)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.U8,typeof(ulong)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.R4,typeof(float)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.R8,typeof(double)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.String,typeof(string)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.I,typeof(IntPtr)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.U,typeof(UIntPtr)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.Object,typeof(object)},
            {(byte)CilTools.BytecodeAnalysis.ElementType.TypedByRef,typeof(TypedReference)}
        };    

        static int DecodeToken(uint decompressed) //ECMA-335 II.23.2.8 TypeDefOrRefOrSpecEncoded
        {
            byte table_index = (byte)((int)decompressed & 0x03);

            //decode table index

            if(table_index == 2) table_index = 0x1B; //TypeSpec
            else if (table_index == 0) table_index = 0x02;//TypeDef
            //TypeRef index is unchanged by encoding

            int value_index = ((int)decompressed & ~0x03) >> 2;

            byte[] value_bytes = BitConverter.GetBytes(value_index);
            byte[] res_bytes = new byte[4];
            res_bytes[3] = table_index;
            res_bytes[0] = value_bytes[0];
            res_bytes[1] = value_bytes[1];
            res_bytes[2] = value_bytes[2];
            return BitConverter.ToInt32(res_bytes, 0);
        }

        /// <summary>
        /// Reads <c>TypeSpec</c> object from the specified byte array
        /// </summary>
        /// <param name="data">Byte array to read data from</param>
        /// <param name="resolver">Object used to resolve metadata tokens</param>
        /// <param name="member">
        /// Method that identifies generic context for generic method params, or null if this <c>TypeSpec</c> 
        /// does not belong to a generic method
        /// </param>
        /// <returns></returns>
        public static TypeSpec ReadFromArray(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            //ECMA-335 II.23.2.14 TypeSpec
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");
            MemoryStream ms = new MemoryStream(data);

            using (ms)
            {
                return TypeSpec.ReadFromStream(ms, resolver,member);
            }
        }

        internal static TypeSpec ReadFromStream(Stream source, ITokenResolver resolver)
        {
            return TypeSpec.ReadFromStream(source, resolver, null);
        }

        internal static TypeSpec ReadFromStream(
            Stream source, ITokenResolver resolver, MemberInfo member
            ) //ECMA-335 II.23.2.12 Type
        {
            Debug.Assert(source != null, "Source stream is null");

            CustomModifier mod;            
            byte b;
            int typetok;
            Type t;                       
            bool found_type;
            bool isbyref = false;
            bool ispinned = false;

            byte type = 0; //element type
            List<CustomModifier> mods = new List<CustomModifier>(5);
            TypeSpec ts = null; //inner typespec for ptr/array
            Type restype = null; //resolved type
            uint paramnum = 0; //generic argument number

            //read modifiers
            while (true)
            {
                b = MetadataReader.ReadByte(source);
                found_type = false;

                switch (b)
                {
                    case ELEMENT_TYPE_CMOD_OPT:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            t = resolver.ResolveType(typetok);
                            mod = new CustomModifier(false, typetok, t);
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.OnError(null, new CilErrorEventArgs(
                                ex, "Failed to resolve type token for modopt: 0x" + typetok.ToString("X"))
                                );
                            mod = new CustomModifier(false, typetok, null);
                        }

                        mods.Add(mod);

                        break;
                    case ELEMENT_TYPE_CMOD_REQD:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            t = resolver.ResolveType(typetok);
                            mod = new CustomModifier(true, typetok, t);
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.OnError(
                                null, 
                                new CilErrorEventArgs(ex, "Failed to resolve type token for modreq: 0x" + typetok.ToString("X"))
                                );
                            mod = new CustomModifier(true, typetok, null);
                        }

                        mods.Add(mod);

                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.ByRef:
                        isbyref = true;
                        break;
                    case ELEMENT_TYPE_PINNED:
                        ispinned = true;
                        break;
                    default:
                        type = b;
                        found_type = true;
                        break;
                }
                if (found_type) break;
            }//end while

            //read type
            typetok = 0;            
            
            if (_types.ContainsKey(type))
            {
                restype = _types[type];                
            }
            else
            {
                switch (type)
                {
                    case (byte)CilTools.BytecodeAnalysis.ElementType.Class:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            restype = resolver.ResolveType(typetok);
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.OnError(
                                null, 
                                new CilErrorEventArgs(ex, "Failed to resolve class token: 0x" + typetok.ToString("X"))
                                );

                            throw new NotSupportedException("The signature contains TypeSpec that cannot be parsed");
                        }
                        
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.ValueType:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            restype = resolver.ResolveType(typetok, null, null);
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.OnError(
                                null, 
                                new CilErrorEventArgs(ex, "Failed to resolve valuetype token: 0x" + typetok.ToString("X"))
                                );

                            throw new NotSupportedException("The signature contains TypeSpec that cannot be parsed");
                        }
                                                
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.Array:
                        ts = TypeSpec.ReadFromStream(source, resolver,member);

                        //II.23.2.13 ArrayShape
                        uint rank = MetadataReader.ReadCompressed(source);
                        if (rank == 0) throw new CilParserException("Fatal parse error: array rank cannot be zero!");

                        uint numsizes = MetadataReader.ReadCompressed(source);
                        int[] sizes = new int[numsizes];

                        for (uint n = 0; n < numsizes; n++)
                        {
                            uint dsize = MetadataReader.ReadCompressed(source);
                            sizes[n] = (int)dsize;
                        }

                        uint numbounds = MetadataReader.ReadCompressed(source);
                        if (numbounds > 0) throw new NotSupportedException("Parsing array shapes that specify lower bounds is not supported");

                        if (ts.Type != null) restype = ts.Type.MakeArrayType((int)rank);
                        
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.SzArray:
                        ts = TypeSpec.ReadFromStream(source, resolver, member);

                        if(ts.Type!=null) restype = ts.Type.MakeArrayType();

                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.Ptr:
                        ts = TypeSpec.ReadFromStream(source, resolver, member);

                        if (ts.Type != null) restype = ts.Type.MakePointerType();

                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.Var: //generic type arg
                        paramnum = MetadataReader.ReadCompressed(source);
                        restype = new GenericParamType(null, (int)paramnum);
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.MVar: //generic method arg
                        paramnum = MetadataReader.ReadCompressed(source);
                        restype = new GenericParamType(member as MethodBase, (int)paramnum);
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.Internal:
                        //skip sizeof(IntPtr) bytes
                        byte[] buf = new byte[System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr))];
                        source.Read(buf, 0, buf.Length);
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.FnPtr:
                        Signature psig = new Signature(source, resolver, member);
                        restype = new FunctionPointerType(psig);
                        break;
                    case (byte)CilTools.BytecodeAnalysis.ElementType.GenericInst:
                        byte tdef = MetadataReader.ReadByte(source);
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));
                        Type tdef_t=null;

                        try
                        {
                            tdef_t = resolver.ResolveType(typetok);
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.OnError(null, new CilErrorEventArgs(ex, 
                                "Failed to resolve generic type definition token: 0x" + typetok.ToString("X")
                                ));

                            throw new NotSupportedException("The signature contains TypeSpec that cannot be parsed");
                        }

                        uint genargs_count = MetadataReader.ReadCompressed(source);
                        TypeSpec[] genargs = new TypeSpec[genargs_count];
                        Type[] arg_types = new Type[genargs_count];

                        for (uint i = 0; i < genargs_count; i++)
                        {
                            genargs[i] = TypeSpec.ReadFromStream(source, resolver, member);
                            arg_types[i] = genargs[i].Type;
                        }

                        restype = tdef_t.MakeGenericType(arg_types);

                        break;
                }//end switch
            }

            if (isbyref) restype = restype.MakeByRefType();

            return new TypeSpec(mods.ToArray(),type,restype,ts,paramnum,ispinned);
        }

        internal static TypeSpec FromType(Type t, bool pinned)
        {
            Debug.Assert(t != null, "Input type should not be null");

            byte et = 0;
            uint genpos = 0;
            TypeSpec inner=null;

            //try find primitive type
            foreach (byte key in _types.Keys)
            {
                if (t == _types[key])
                {
                    et = key;
                    break;
                }
            }

            if (et == 0) //if not found, determine complex type
            {
                if (t.IsGenericParameter)
                {
                    if (t.DeclaringMethod == null) et = (byte)ElementType.Var;
                    else et = (byte)ElementType.MVar;

                    genpos = (uint)t.GenericParameterPosition;
                }
                else if (t.IsArray)
                {
                    et = (byte)ElementType.Array;
                    inner = TypeSpec.FromType(t.GetElementType(),false);
                }
                else if (t.IsPointer)
                {
                    et = (byte)ElementType.Ptr;
                    inner = TypeSpec.FromType(t.GetElementType(), false);
                }
                else if (t.IsGenericType)
                {
                    et = (byte)ElementType.GenericInst;
                }
                else if (t.IsValueType)
                {
                    et = (byte)ElementType.ValueType;
                }
                else if (t.IsClass)
                {
                    et = (byte)ElementType.Class;
                }
            }

            return new TypeSpec(new CustomModifier[0], et,t, inner, genpos, pinned);
        }

        // *** instance members ***
        byte _ElementType;
        CustomModifier[] _Modifiers;        
        TypeSpec _InnerSpec;
        Type _Type;
        uint _paramnum;
        bool _pinned;

        internal TypeSpec(CustomModifier[] mods, byte elemtype, Type t, TypeSpec ts = null, uint parnum = 0, bool pinned = false)
        {            
            this._Modifiers = mods;
            this._ElementType = elemtype;
            this._Type = t;
            this._InnerSpec = ts;
            this._paramnum = parnum;
            this._pinned = pinned;
        }

        /// <summary>
        /// Gets the element type of this type specification 
        /// </summary>
        public ElementType ElementType { get { return (ElementType)this._ElementType; } }

        /// <summary>
        /// Gets the amount of custom modifiers associated with this type specification 
        /// </summary>
        public int ModifiersCount { get { return this._Modifiers.Length; } }

        /// <summary>
        /// Gets the custom modifier with the specified index
        /// </summary>
        /// <param name="index">Index of the requested modifier</param>
        /// <returns>The requested custom modifier</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Index is negative or outside the bounds of the collection</exception>
        public CustomModifier GetModifier(int index)
        {
            if (index < 0 || index >= this._Modifiers.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index must be non-negative and within the size of collection");
            }

            return this._Modifiers[index];
        }

        /// <summary>
        /// Enumerates custom modifiers associated with this type specification
        /// </summary>
        public IEnumerable<CustomModifier> Modifiers
        {
            get
            {
                for (int i = 0; i < this._Modifiers.Length; i++)
                {
                    yield return this._Modifiers[i];
                }
            }
        }

        /// <summary>
        /// Gets the array of custom modifiers associated with this type specification
        /// </summary>
        /// <returns>The array of custom modifiers</returns>
        public CustomModifier[] GetModifiers()
        {
            CustomModifier[] res = new CustomModifier[this._Modifiers.Length];
            this._Modifiers.CopyTo(res, 0);
            return res;            
        }

        /// <summary>
        /// Gets the inner type specification if this instance represents an array or pointer type. For other types, the value is null
        /// </summary>
        public TypeSpec InnerTypeSpec { get { return this._InnerSpec; } }

        /// <summary>
        /// Gets the type which this type specification represents (deprecated)
        /// </summary>
        /// <remarks>
        /// Starting from the version 2.1, the <c>TypeSpec</c> itself extends <c>System.Type</c>, so this property is not needed. 
        /// Use this object directly when you need an instance of <c>System.Type</c>, or the <see cref="UnderlyingSystemType"/> property 
        /// if you need the runtime type. This property will be removed in future releases.
        /// </remarks>
        public Type Type { get { return this; } }

        /// <summary>
        /// Gets the value indicating whether this TypeSpec represents pinned local variable
        /// </summary>
        public bool IsPinned { get { return this._pinned; } }

        /// <inheritdoc/>
        public override Guid GUID => this._Type.GUID;

        /// <inheritdoc/>
        public override Module Module => this._Type.Module;

        /// <inheritdoc/>
        public override Assembly Assembly => this._Type.Assembly;

        /// <inheritdoc/>
        public override string FullName => this._Type.FullName;

        /// <inheritdoc/>
        public override string Namespace => this._Type.Namespace;

        /// <inheritdoc/>
        public override string AssemblyQualifiedName => this._Type.AssemblyQualifiedName;

        /// <inheritdoc/>
        public override Type BaseType => this._Type.BaseType;

        /// <inheritdoc/>
        public override Type UnderlyingSystemType => this._Type.UnderlyingSystemType;

        /// <inheritdoc/>
        public override string Name => this._Type.Name;

        /// <inheritdoc/>
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target,
            object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            return this._Type.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        /// <inheritdoc/>
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return this._Type.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        /// <inheritdoc/>
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return this._Type.GetConstructors(bindingAttr);
        }

        /// <inheritdoc/>
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return this._Type.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        /// <inheritdoc/>
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return this._Type.GetMethods(bindingAttr);
        }

        /// <inheritdoc/>
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return this._Type.GetField(name, bindingAttr);
        }

        /// <inheritdoc/>
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return this._Type.GetFields(bindingAttr);
        }

        /// <inheritdoc/>
        public override Type GetInterface(string name, bool ignoreCase)
        {
            return this._Type.GetInterface(name, ignoreCase);
        }

        /// <inheritdoc/>
        public override Type[] GetInterfaces()
        {
            return this._Type.GetInterfaces();
        }

        /// <inheritdoc/>
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return this._Type.GetEvent(name, bindingAttr);
        }

        /// <inheritdoc/>
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return this._Type.GetEvents();
        }

        /// <inheritdoc/>
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder,
            Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            return this._Type.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
        }

        /// <inheritdoc/>
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return this._Type.GetProperties();
        }

        /// <inheritdoc/>
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return this._Type.GetNestedTypes();
        }

        /// <inheritdoc/>
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            return this._Type.GetNestedType(name, bindingAttr);
        }

        /// <inheritdoc/>
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return this._Type.GetMembers(bindingAttr);
        }

        /// <inheritdoc/>
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return this._Type.Attributes;
        }

        /// <inheritdoc/>
        protected override bool IsArrayImpl()
        {
            return this._ElementType == (byte)ElementType.SzArray ||
                this._ElementType == (byte)ElementType.Array;
        }

        /// <inheritdoc/>
        protected override bool IsByRefImpl()
        {
            return this._Type.IsByRef;
        }

        /// <inheritdoc/>
        protected override bool IsPointerImpl()
        {
            return this._ElementType == (byte)ElementType.Ptr;
        }

        /// <inheritdoc/>
        protected override bool IsPrimitiveImpl()
        {
            return this._Type.IsPrimitive;
        }

        /// <inheritdoc/>
        protected override bool IsCOMObjectImpl()
        {
            return this._Type.IsCOMObject;
        }

        /// <inheritdoc/>
        public override Type GetElementType()
        {
            if (this._InnerSpec != null) return this._InnerSpec;
            else return this._Type.GetElementType();
        }

        /// <inheritdoc/>
        protected override bool HasElementTypeImpl()
        {
            return this._Type.HasElementType;
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return this._Type.GetCustomAttributes(inherit);
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return this._Type.GetCustomAttributes(attributeType, inherit);
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return this._Type.IsDefined(attributeType, inherit);
        }

        /// <inheritdoc/>
        public override int MetadataToken => this._Type.MetadataToken;

        /// <inheritdoc/>
        public override int GetArrayRank()
        {
            return this._Type.GetArrayRank();
        }

        /// <inheritdoc/>
        public override Type MakeArrayType()
        {
            return this._Type.MakeArrayType();
        }

        /// <inheritdoc/>
        public override Type MakeByRefType()
        {
            return this._Type.MakeByRefType();
        }

        /// <inheritdoc/>
        public override Type MakePointerType()
        {
            return this._Type.MakePointerType();
        }

        /// <inheritdoc/>
        public override Type MakeGenericType(params Type[] typeArguments)
        {
            return this._Type.MakeGenericType(typeArguments);
        }

        /// <inheritdoc/>
        public override Type DeclaringType => this._Type.DeclaringType;

        /// <inheritdoc/>
        public override MethodBase DeclaringMethod => this._Type.DeclaringMethod;

        /// <inheritdoc/>
        public override bool IsGenericParameter
        {
            get
            {
                return this._ElementType == (byte)ElementType.Var ||
                    this._ElementType == (byte)ElementType.MVar;
            }
        }

        /// <inheritdoc/>
        public override int GenericParameterPosition => (int)this._paramnum;

        /// <inheritdoc/>
        public override bool IsGenericType => this._ElementType == (byte)ElementType.GenericInst;

        /// <inheritdoc/>
        public override bool IsGenericTypeDefinition => false;
        
        /// <summary>
        /// Gets the target function signature, if this <c>TypeSpec</c> represents a function pointer. Otherwise, returns null.
        /// </summary>
        public Signature TargetSignature
        {
            get 
            {
                if (this._Type is FunctionPointerType)
                {
                    return ((FunctionPointerType)this._Type).TargetSignature;
                }
                else return null;
            }
        }

        /// <inheritdoc/>
        public override Type[] GetGenericArguments()
        {
            return this._Type.GetGenericArguments();
        }

        /// <inheritdoc/>
        public override Type GetGenericTypeDefinition()
        {
            return this._Type.GetGenericTypeDefinition();
        }

        /// <inheritdoc/>
        public override bool IsAssignableFrom(Type c)
        {
            if (this._Type == null) return base.IsAssignableFrom(c);
            else return this._Type.IsAssignableFrom(c);
        }

        /// <summary>
        /// Returns textual representation of this type specification as CIL code
        /// </summary>        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(50);

            if (this._InnerSpec != null && this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.SzArray)
            {
                sb.Append(this._InnerSpec.ToString());
                sb.Append("[]");
                if (this._Type != null)
                {
                    if (this._Type.IsByRef) sb.Append("&");
                }
            }
            else if (this._InnerSpec != null && this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.Ptr)
            {
                sb.Append(this._InnerSpec.ToString());
                sb.Append("*");
                if (this._Type != null)
                {
                    if (this._Type.IsByRef) sb.Append("&");
                }
            }
            else if (this._Type != null)
            {
                sb.Append(CilAnalysis.GetTypeName(this._Type));
            }
            else if (this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.Var) //generic type arg
            {
                sb.Append("!" + this._paramnum.ToString());
            }
            else if (this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.MVar) //generic method arg
            {
                sb.Append("!!" + this._paramnum.ToString());
            }
            else if (this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.Internal)
            {
                sb.Append("ClrInternal");
            }
            else
            {
                sb.Append("Type" + this._ElementType.ToString("X"));
            }

            foreach (CustomModifier mod in this._Modifiers)
            {
                sb.Append(' ');
                sb.Append(mod.ToString());
            }

            return sb.ToString();
        }

        internal MemberRefSyntax ToSyntax()
        {
            List<SyntaxNode> ret = new List<SyntaxNode>();

            if (this._InnerSpec != null && this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.SzArray)
            {
                ret.Add(this._InnerSpec.ToSyntax());
                ret.Add(new PunctuationSyntax(String.Empty, "[]", String.Empty));

                if (this._Type != null)
                {
                    if (this._Type.IsByRef) ret.Add(new PunctuationSyntax(String.Empty, "&", String.Empty));
                }
            }
            else if (this._InnerSpec != null && this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.Ptr)
            {
                ret.Add(this._InnerSpec.ToSyntax());
                ret.Add(new PunctuationSyntax(String.Empty, "*", String.Empty));

                if (this._Type != null)
                {
                    if (this._Type.IsByRef) ret.Add(new PunctuationSyntax(String.Empty, "&", String.Empty));
                }
            }
            else if (this._Type != null)
            {
                IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeNameSyntax(this);

                foreach (SyntaxNode x in nodes) ret.Add(x);

                //GetTypeNameSyntax will add modifiers when called on TypeSpec
                return new MemberRefSyntax(ret.ToArray(), this._Type);
            }
            else if (this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.Var) //generic type arg
            {
                ret.Add(new GenericSyntax("!" + this._paramnum.ToString()));
            }
            else if (this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.MVar) //generic method arg
            {
                ret.Add(new GenericSyntax("!!" + this._paramnum.ToString()));
            }
            else if (this._ElementType == (byte)CilTools.BytecodeAnalysis.ElementType.Internal)
            {
                ret.Add(new GenericSyntax("ClrInternal"));
            }
            else
            {
                ret.Add(new GenericSyntax("Type" + this._ElementType.ToString("X")));
            }

            foreach (CustomModifier mod in this._Modifiers)
            {
                foreach(SyntaxNode node in mod.ToSyntax()) ret.Add(node);
            }

            return new MemberRefSyntax(ret.ToArray(), this._Type);
        }

        /// <summary>
        /// Gets a value indicating whether this <c>TypeSpec</c> represents a function pointer
        /// </summary>
        /// <returns></returns>
        public bool IsFunctionPointer()
        {
            return this._ElementType == (byte)ElementType.FnPtr;
        }

        /// <inheritdoc/>
        protected override bool IsValueTypeImpl()
        {
            //For struct and class we can deterimine IsValueType without checking the base type.
            //This fixes exception when the actual type in an external assembly which can't be 
            //resolved.
            if (this._ElementType == (byte)ElementType.Class) return false;
            else if (this._ElementType == (byte)ElementType.ValueType) return true;
            else return base.IsValueTypeImpl();
        }
    }
}
