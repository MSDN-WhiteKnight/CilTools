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

    public class TypeSpec //ECMA-335 II.23.2.12 Type
    {
        //ECMA-335 II.23.1.16 Element types used in signatures 
        public const byte ELEMENT_TYPE_VOID = 0x01;
        public const byte ELEMENT_TYPE_BOOLEAN = 0x02;
        public const byte ELEMENT_TYPE_CHAR = 0x03;
        public const byte ELEMENT_TYPE_I1 = 0x04;
        public const byte ELEMENT_TYPE_U1 = 0x05;
        public const byte ELEMENT_TYPE_I2 = 0x06;
        public const byte ELEMENT_TYPE_U2 = 0x07;
        public const byte ELEMENT_TYPE_I4 = 0x08;
        public const byte ELEMENT_TYPE_U4 = 0x09;
        public const byte ELEMENT_TYPE_I8 = 0x0a;
        public const byte ELEMENT_TYPE_U8 = 0x0b;
        public const byte ELEMENT_TYPE_R4 = 0x0c;
        public const byte ELEMENT_TYPE_R8 = 0x0d;
        public const byte ELEMENT_TYPE_STRING = 0x0e;
        public const byte ELEMENT_TYPE_PTR = 0x0f;  //Followed by type 
        public const byte ELEMENT_TYPE_BYREF = 0x10;  //Followed by type 
        public const byte ELEMENT_TYPE_VALUETYPE = 0x11;  //Followed by TypeDef or TypeRef token 
        public const byte ELEMENT_TYPE_CLASS = 0x12;  //Followed by TypeDef or TypeRef token 
        public const byte ELEMENT_TYPE_VAR = 0x13;  //Generic parameter in a generic type definition, represented as number (compressed unsigned integer) 
        public const byte ELEMENT_TYPE_ARRAY = 0x14;  //type rank boundsCount bound1 … loCount lo1 … 
        public const byte ELEMENT_TYPE_GENERICINST = 0x15;  //Generic type instantiation.  Followed by type type-arg-count  type-1 ... type-n 
        public const byte ELEMENT_TYPE_TYPEDBYREF = 0x16;
        public const byte ELEMENT_TYPE_I = 0x18;  //System.IntPtr 
        public const byte ELEMENT_TYPE_U = 0x19;  //System.UIntPtr 
        public const byte ELEMENT_TYPE_FNPTR = 0x1b;  //Followed by full method signature 
        public const byte ELEMENT_TYPE_OBJECT = 0x1c;  //System.Object 
        public const byte ELEMENT_TYPE_SZARRAY = 0x1d;  //Single-dim array with 0 lower bound 
        public const byte ELEMENT_TYPE_MVAR = 0x1e;  //Generic parameter in a generic method definition, represented as number (compressed unsigned integer) 
        public const byte ELEMENT_TYPE_INTERNAL = 0x21;  //Implemented within the CLI 
        public const byte ELEMENT_TYPE_MODIFIER = 0x40;  //Or’d with following element types 
        public const byte ELEMENT_TYPE_SENTINEL = 0x41;  //Sentinel for vararg method signature

        internal const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
        internal const byte ELEMENT_TYPE_CMOD_OPT = 0x20;

        static Dictionary<byte, Type> _types = new Dictionary<byte, Type>
        {
            {ELEMENT_TYPE_VOID,typeof(void)},
            {ELEMENT_TYPE_BOOLEAN,typeof(bool)},
            {ELEMENT_TYPE_CHAR,typeof(char)},
            {ELEMENT_TYPE_I1,typeof(sbyte)},
            {ELEMENT_TYPE_U1,typeof(byte)},
            {ELEMENT_TYPE_I2,typeof(short)},
            {ELEMENT_TYPE_U2,typeof(ushort)},
            {ELEMENT_TYPE_I4,typeof(int)},
            {ELEMENT_TYPE_U4,typeof(uint)},
            {ELEMENT_TYPE_I8,typeof(long)},
            {ELEMENT_TYPE_U8,typeof(ulong)},
            {ELEMENT_TYPE_R4,typeof(float)},
            {ELEMENT_TYPE_R8,typeof(double)},
            {ELEMENT_TYPE_STRING,typeof(string)},
            {ELEMENT_TYPE_I,typeof(IntPtr)},
            {ELEMENT_TYPE_U,typeof(UIntPtr)},
            {ELEMENT_TYPE_OBJECT,typeof(object)},
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
                    case ELEMENT_TYPE_BYREF:
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
                    case ELEMENT_TYPE_CLASS:
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
                    case ELEMENT_TYPE_VALUETYPE:
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
                    case ELEMENT_TYPE_ARRAY:
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
                    case ELEMENT_TYPE_SZARRAY:
                        ts = TypeSpec.ReadFromStream(source, module);
                        restype = ts.Type.MakeArrayType();                        
                        break;
                    case ELEMENT_TYPE_PTR:
                        ts = TypeSpec.ReadFromStream(source, module);
                        restype = ts.Type.MakePointerType();
                        break;
                    case ELEMENT_TYPE_VAR: //generic type arg
                        paramnum = MetadataReader.ReadCompressed(source);                        
                        break;
                    case ELEMENT_TYPE_MVAR: //generic method arg
                        paramnum = MetadataReader.ReadCompressed(source);                        
                        break;
                    case ELEMENT_TYPE_FNPTR: throw new NotSupportedException("Parsing signatures with function pointers is not supported");
                    case ELEMENT_TYPE_GENERICINST: throw new NotSupportedException("Parsing signatures with generic types is not supported");                    
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

        public byte ElementType { get { return this._ElementType; } }

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

            if (this._InnerSpec != null && this._ElementType == ELEMENT_TYPE_SZARRAY)
            {
                sb.Append(this._InnerSpec.ToString());
                sb.Append("[]");
                if (this._Type != null)
                {
                    if (this._Type.IsByRef) sb.Append("&");
                }
            }
            else if(this._InnerSpec != null && this._ElementType == ELEMENT_TYPE_PTR)
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
            else if (this._ElementType == ELEMENT_TYPE_VAR) //generic type arg
            {
                sb.Append("!" + this._paramnum.ToString());
            }
            else if (this._ElementType == ELEMENT_TYPE_MVAR) //generic method arg
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
