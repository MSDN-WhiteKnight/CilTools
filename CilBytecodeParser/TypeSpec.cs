/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CilBytecodeParser
{
    /// <summary>
    /// Represents signature element type as defined in ECMA-335 CLI specification
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

    public class TypeSpec //ECMA-335 II.23.2.12 Type
    {
        //ECMA-335 II.23.1.16 Element types used in signatures
        internal const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
        internal const byte ELEMENT_TYPE_CMOD_OPT = 0x20;

        static Dictionary<byte, Type> _types = new Dictionary<byte, Type>
        {
            {(byte)CilBytecodeParser.ElementType.Void,typeof(void)},
            {(byte)CilBytecodeParser.ElementType.Boolean,typeof(bool)},
            {(byte)CilBytecodeParser.ElementType.Char,typeof(char)},
            {(byte)CilBytecodeParser.ElementType.I1,typeof(sbyte)},
            {(byte)CilBytecodeParser.ElementType.U1,typeof(byte)},
            {(byte)CilBytecodeParser.ElementType.I2,typeof(short)},
            {(byte)CilBytecodeParser.ElementType.U2,typeof(ushort)},
            {(byte)CilBytecodeParser.ElementType.I4,typeof(int)},
            {(byte)CilBytecodeParser.ElementType.U4,typeof(uint)},
            {(byte)CilBytecodeParser.ElementType.I8,typeof(long)},
            {(byte)CilBytecodeParser.ElementType.U8,typeof(ulong)},
            {(byte)CilBytecodeParser.ElementType.R4,typeof(float)},
            {(byte)CilBytecodeParser.ElementType.R8,typeof(double)},
            {(byte)CilBytecodeParser.ElementType.String,typeof(string)},
            {(byte)CilBytecodeParser.ElementType.I,typeof(IntPtr)},
            {(byte)CilBytecodeParser.ElementType.U,typeof(UIntPtr)},
            {(byte)CilBytecodeParser.ElementType.Object,typeof(object)},
        };    

        static int DecodeToken(uint decompressed) //ECMA-335 II.23.2.8 TypeDefOrRefOrSpecEncoded
        {
            byte table_index = (byte)((int)decompressed & 0x03);
            int value_index = ((int)decompressed & ~0x03) >> 2;

            byte[] value_bytes = BitConverter.GetBytes(value_index);
            byte[] res_bytes = new byte[4];
            res_bytes[3] = table_index;
            res_bytes[0] = value_bytes[0];
            res_bytes[1] = value_bytes[1];
            res_bytes[2] = value_bytes[2];
            return BitConverter.ToInt32(res_bytes, 0);
        }

        internal static TypeSpec ReadFromStream(Stream source, Module module) //ECMA-335 II.23.2.12 Type
        {            
            CustomModifier mod;            
            byte b;
            int typetok;
            Type t;                       
            bool found_type;
            bool isbyref = false;

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
                            t = module.ResolveType(typetok);
                            mod = new CustomModifier(false, typetok, t);
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve type token for modopt: 0x" + typetok.ToString("X")));
                            mod = new CustomModifier(false, typetok, null);
                        }

                        mods.Add(mod);

                        break;
                    case ELEMENT_TYPE_CMOD_REQD:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            t = module.ResolveType(typetok);
                            mod = new CustomModifier(true, typetok, t);
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve type token for modreq: 0x" + typetok.ToString("X")));
                            mod = new CustomModifier(true, typetok, null);
                        }

                        mods.Add(mod);

                        break;
                    case (byte)CilBytecodeParser.ElementType.ByRef:
                        isbyref = true;
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
                    case (byte)CilBytecodeParser.ElementType.Class:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            restype = module.ResolveType(typetok);                            
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve class token: 0x" + typetok.ToString("X")));
                        }
                        
                        break;
                    case (byte)CilBytecodeParser.ElementType.ValueType:
                        typetok = DecodeToken(MetadataReader.ReadCompressed(source));

                        try
                        {
                            restype = module.ResolveType(typetok);                            
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve valuetype token: 0x" + typetok.ToString("X")));
                        }
                                                
                        break;
                    case (byte)CilBytecodeParser.ElementType.Array:
                        ts = TypeSpec.ReadFromStream(source, module);                        

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
                                                
                        restype = ts.Type.MakeArrayType((int)rank);                        
                        break;
                    case (byte)CilBytecodeParser.ElementType.SzArray:
                        ts = TypeSpec.ReadFromStream(source, module);
                        restype = ts.Type.MakeArrayType();                        
                        break;
                    case (byte)CilBytecodeParser.ElementType.Ptr:
                        ts = TypeSpec.ReadFromStream(source, module);
                        restype = ts.Type.MakePointerType();
                        break;
                    case (byte)CilBytecodeParser.ElementType.Var: //generic type arg
                        paramnum = MetadataReader.ReadCompressed(source);                        
                        break;
                    case (byte)CilBytecodeParser.ElementType.MVar: //generic method arg
                        paramnum = MetadataReader.ReadCompressed(source);                        
                        break;
                    case (byte)CilBytecodeParser.ElementType.FnPtr: 
                        throw new NotSupportedException("Parsing signatures with function pointers is not supported");
                    case (byte)CilBytecodeParser.ElementType.GenericInst: 
                        throw new NotSupportedException("Parsing signatures with generic types is not supported");
                }//end switch
            }

            if (isbyref) restype = restype.MakeByRefType();

            return new TypeSpec(mods.ToArray(),type,restype,ts,paramnum);
        }

        /// <summary>
        /// Raised when error occurs in one of the methods in this class
        /// </summary>
        public static event EventHandler<CilErrorEventArgs> Error;

        /// <summary>
        /// Raises a 'Error' event
        /// </summary>
        /// <param name="sender">Object that caused this event</param>
        /// <param name="e">Event arguments</param>
        protected static void OnError(object sender, CilErrorEventArgs e)
        {
            EventHandler<CilErrorEventArgs> handler = Error;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        // *** instance members ***
        byte _ElementType;
        CustomModifier[] _Modifiers;        
        TypeSpec _InnerSpec;
        Type _Type;
        uint _paramnum;

        internal TypeSpec(CustomModifier[] mods, byte elemtype, Type t, TypeSpec ts = null, uint parnum = 0)
        {
            this._Modifiers = mods;
            this._ElementType = elemtype;
            this._Type = t;
            this._InnerSpec = ts;
            this._paramnum = parnum;
        }

        public ElementType ElementType { get { return (ElementType)this._ElementType; } }

        public int ModifiersCount { get { return this._Modifiers.Length; } }

        public CustomModifier GetModifier(int index)
        {
            if (index < 0 || index >= this._Modifiers.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index must be non-negative and within the size of collection");
            }

            return this._Modifiers[index];
        }

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

        public CustomModifier[] GetModifiers()
        {
            CustomModifier[] res = new CustomModifier[this._Modifiers.Length];
            this._Modifiers.CopyTo(res, 0);
            return res;            
        }

        public TypeSpec InnerTypeSpec { get { return this._InnerSpec; } }

        public Type Type { get { return this._Type; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(50);

            if (this._InnerSpec != null && this._ElementType == (byte)CilBytecodeParser.ElementType.SzArray)
            {
                sb.Append(this._InnerSpec.ToString());
                sb.Append("[]");
                if (this._Type != null)
                {
                    if (this._Type.IsByRef) sb.Append("&");
                }
            }
            else if(this._InnerSpec != null && this._ElementType == (byte)CilBytecodeParser.ElementType.Ptr)
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
            else if (this._ElementType == (byte)CilBytecodeParser.ElementType.Var) //generic type arg
            {
                sb.Append("!" + this._paramnum.ToString());
            }
            else if (this._ElementType == (byte)CilBytecodeParser.ElementType.MVar) //generic method arg
            {
                sb.Append("!!" + this._paramnum.ToString());
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
    }
}
