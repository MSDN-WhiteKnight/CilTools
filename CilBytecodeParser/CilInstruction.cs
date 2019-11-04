/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilBytecodeParser
{
    /// <summary>
    /// Represents CIL instruction, a main structural element of the method body which consists of operation code and operand.
    /// </summary>
    /// <remarks>To retreive a collection of CIL instructions for the specified method, use methods of <see cref="CilReader"/> class.</remarks>
    public class CilInstruction
    {
        //ECMA-335 II.23.1.16 Element types used in signatures
        const int ELEMENT_TYPE_CMOD_REQD = 0x1f;
        const int ELEMENT_TYPE_CMOD_OPT = 0x20;

        const int ELEMENT_TYPE_VOID = 0x01; 
        const int ELEMENT_TYPE_BOOLEAN  = 0x02;   
        const int ELEMENT_TYPE_CHAR =  0x03;   
        const int ELEMENT_TYPE_I1 =  0x04;   
        const int ELEMENT_TYPE_U1 =  0x05;   
        const int ELEMENT_TYPE_I2 =  0x06;
        const int ELEMENT_TYPE_U2 =  0x07;
        const int ELEMENT_TYPE_I4 =  0x08;   
        const int ELEMENT_TYPE_U4 =  0x09;   
        const int ELEMENT_TYPE_I8 =  0x0a;   
        const int ELEMENT_TYPE_U8 =  0x0b;   
        const int ELEMENT_TYPE_R4 =  0x0c;   
        const int ELEMENT_TYPE_R8 =  0x0d;
        const int ELEMENT_TYPE_STRING = 0x0e;
        const int ELEMENT_TYPE_PTR = 0x0f;  //Followed by type 
        const int ELEMENT_TYPE_BYREF =  0x10;  //Followed by type 
        const int ELEMENT_TYPE_VALUETYPE =  0x11;  //Followed by TypeDef or TypeRef token 
        const int ELEMENT_TYPE_CLASS =  0x12;  //Followed by TypeDef or TypeRef token 
        const int ELEMENT_TYPE_VAR = 0x13;  //Generic parameter in a generic type definition, represented as number (compressed unsigned integer) 
        const int ELEMENT_TYPE_ARRAY =  0x14;  //type rank boundsCount bound1 … loCount lo1 … 
        const int ELEMENT_TYPE_GENERICINST = 0x15;  //Generic type instantiation.  Followed by type type-arg-count  type-1 ... type-n 
        const int ELEMENT_TYPE_TYPEDBYREF = 0x16; 
        const int ELEMENT_TYPE_I = 0x18;  //System.IntPtr 
        const int ELEMENT_TYPE_U =  0x19;  //System.UIntPtr 
        const int ELEMENT_TYPE_FNPTR = 0x1b;  //Followed by full method signature 
        const int ELEMENT_TYPE_OBJECT = 0x1c;  //System.Object 
        const int ELEMENT_TYPE_SZARRAY = 0x1d;  //Single-dim array with 0 lower bound 
        const int ELEMENT_TYPE_MVAR = 0x1e;  //Generic parameter in a generic method definition, represented as number (compressed unsigned integer) 
        const int ELEMENT_TYPE_INTERNAL = 0x21;  //Implemented within the CLI 
        const int ELEMENT_TYPE_MODIFIER =  0x40;  //Or’d with following element types 
        const int ELEMENT_TYPE_SENTINEL = 0x41;  //Sentinel for vararg method signature 
        
        static Dictionary<int, Type> types = new Dictionary<int, Type>
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

        //ECMA-335 II.23.2.3: StandAloneMethodSig
        const int CALLCONV_DEFAULT  = 0x00;
        const int CALLCONV_CDECL    = 0x01;
        const int CALLCONV_STDCALL  = 0x02;
        const int CALLCONV_THISCALL = 0x03;
        const int CALLCONV_FASTCALL = 0x04;
        const int CALLCONV_VARARG   = 0x05;
        const int CALLCONV_MASK     = 0x0F;

        const int MFLAG_HASTHIS = 0x20;
        const int MFLAG_EXPLICITTHIS = 0x40;                

        static byte ReadByte(Stream source)
        {
            int res = source.ReadByte();
            if (res < 0) throw new EndOfStreamException();
            return (byte)res;
        }

        static uint ReadCompressed(Stream source) //ECMA-335 II.23.2 Blobs and signatures
        {
            byte[] paramcount_bytes = new byte[4];            
            byte b1,b2,b3,b4;
            b1 = ReadByte(source);           

            if ((b1 & 0x80) == 0x80)
            {
                b2 = ReadByte(source);

                if ((b2 & 0x40) == 0x40) //4 bytes
                {
                    paramcount_bytes[0] = b1;
                    paramcount_bytes[1] = b2;

                    b3 = ReadByte(source);
                    b4 = ReadByte(source);

                    paramcount_bytes[2] = b3;
                    paramcount_bytes[3] = b4;                    
                }
                else //2 bytes
                {
                    paramcount_bytes[0] = b1;
                    paramcount_bytes[1] = b2;                    
                }
            }
            else //1 byte
            {
                paramcount_bytes[0] = b1;                
            }

            return BitConverter.ToUInt32(paramcount_bytes, 0);
        }

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

        static string ReadTypeSpec(Stream source, Module module) //ECMA-335 II.23.2.12 Type
        {            
            StringBuilder sb = new StringBuilder();
            StringBuilder sb_mods = new StringBuilder();
            byte b;
            int typetok;
            Type t;
            uint paramnum;
            byte type = 0;
            bool found_type;
            bool isbyref = false;
            bool isptr = false;
            
            //read modifiers
            while (true)
            {
                b = ReadByte(source);
                found_type = false;

                switch (b)
                {
                    case ELEMENT_TYPE_CMOD_OPT:
                        typetok = DecodeToken(ReadCompressed(source));
                        try
                        {
                            t = module.ResolveType(typetok);
                            sb_mods.Append(" modopt(" + CilAnalysis.GetTypeNameInternal(t) + ")");
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve type token for modopt: 0x" + typetok.ToString("X")));                            
                            sb_mods.Append(" modopt(Type" + typetok.ToString("X") + ")");
                        }
                        break;
                    case ELEMENT_TYPE_CMOD_REQD:
                        typetok = DecodeToken(ReadCompressed(source));
                        try
                        {
                            t = module.ResolveType(typetok);
                            sb_mods.Append(" modreq(" + CilAnalysis.GetTypeNameInternal(t) + ")");
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve type token for modreq: 0x" + typetok.ToString("X")));
                            sb_mods.Append(" modreq(Type" + typetok.ToString("X") + ")");
                        }
                        break;
                    case ELEMENT_TYPE_BYREF:
                        isbyref = true;
                        break;
                    case ELEMENT_TYPE_PTR:
                        isptr = true;
                        break;
                    default:
                        type = b;
                        found_type = true;
                        break;
                }
                if (found_type) break;
            }//end while

            //read type
            Type rettype = null;
            if (types.ContainsKey((int)type))
            {
                rettype = types[(int)type];
                sb.Append(CilAnalysis.GetTypeName(rettype));
                if (isbyref) sb.Append('&');
                else if (isptr) sb.Append('*');
            }
            else
            {
                switch (type)
                {
                    case ELEMENT_TYPE_CLASS:
                        typetok = DecodeToken(ReadCompressed(source));
                        try
                        {
                            t = module.ResolveType(typetok);
                            sb.Append(CilAnalysis.GetTypeName(t));
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve class token: 0x" + typetok.ToString("X")));
                            sb.Append("class Type" + typetok.ToString("X"));                        
                        }
                        break;
                    case ELEMENT_TYPE_VALUETYPE:
                        typetok = DecodeToken(ReadCompressed(source));
                        try
                        {
                            t = module.ResolveType(typetok);
                            sb.Append(CilAnalysis.GetTypeName(t));
                        }
                        catch (Exception ex)
                        {
                            OnError(null, new CilErrorEventArgs(ex, "Failed to resolve valuetype token: 0x" + typetok.ToString("X")));
                            sb.Append("valuetype Type" + typetok.ToString("X"));
                        }                        
                        break;
                    case ELEMENT_TYPE_VAR: //generic type arg
                        paramnum = ReadCompressed(source);
                        sb.Append("!" + paramnum.ToString());
                        break;
                    case ELEMENT_TYPE_MVAR: //generic method arg
                        paramnum = ReadCompressed(source);
                        sb.Append("!!" + paramnum.ToString());
                        break;
                    default:
                        sb.Append("Type" + type.ToString("X"));
                        break;
                }
            }
                                                                
            sb.Append(sb_mods.ToString()); 
            return sb.ToString();
        }

        static bool ReferencesMethodToken(OpCode op)
        {
            return (op.Equals(OpCodes.Call) || op.Equals(OpCodes.Callvirt) || op.Equals(OpCodes.Newobj)
                || op.Equals(OpCodes.Ldftn));
        }

        static bool ReferencesFieldToken(OpCode op)
        {
            return (op.Equals(OpCodes.Stfld) || op.Equals(OpCodes.Stsfld) ||
                    op.Equals(OpCodes.Ldsfld) || op.Equals(OpCodes.Ldfld));
        }

        static bool ReferencesTypeToken(OpCode op)
        {
            return (op.Equals(OpCodes.Newarr) || op.Equals(OpCodes.Box) || op.Equals(OpCodes.Isinst)
                || op.Equals(OpCodes.Castclass) || op.Equals(OpCodes.Initobj)
                || op.Equals(OpCodes.Unbox) || op.Equals(OpCodes.Unbox_Any));
        }

        static bool ReferencesLocal(OpCode op)
        {
            return (op.Equals(OpCodes.Ldloc) || op.Equals(OpCodes.Ldloca) || op.Equals(OpCodes.Ldloc_S)
                || op.Equals(OpCodes.Ldloca_S) || op.Equals(OpCodes.Stloc) || op.Equals(OpCodes.Stloc_S));
        }

        static bool ReferencesParam(OpCode op)
        {
            return (op.Equals(OpCodes.Ldarg) || op.Equals(OpCodes.Ldarg_S) || op.Equals(OpCodes.Ldarga)
                || op.Equals(OpCodes.Ldarga_S) );
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

        /// <summary>
        /// A reference to a method which this instruction belongs to
        /// </summary>
        protected MethodBase _Method;

        /// <summary>
        /// Opcode of this instruction
        /// </summary>
        protected OpCode _OpCode;

        /// <summary>
        /// Operand object of this instruction, if applicable
        /// </summary>
        protected object _Operand;

        /// <summary>
        /// Size, in bytes, of this instruction's operand
        /// </summary>
        protected uint _OperandSize;

        /// <summary>
        /// Byte offset of this instruction from the beginning of the method body
        /// </summary>
        protected uint _ByteOffset;

        /// <summary>
        /// Ordinal number of the place this instruction takes in method body, starting from one.
        /// </summary>
        protected uint _OrdinalNumber;

        /// <summary>
        /// Gets a reference to a method which this instruction belongs to
        /// </summary>
        public MethodBase Method { get { return this._Method; } }

        /// <summary>
        /// Gets the operation code (opcode) of this instruction
        /// </summary>
        public OpCode OpCode { get { return this._OpCode; } }

        /// <summary>
        /// Gets the operand object of this instruction
        /// </summary>
        public object Operand { get { return this._Operand; } }

        /// <summary>
        /// Gets the size, in bytes, of this instruction's operand
        /// </summary>
        public uint OperandSize { get { return this._OperandSize; } }

        /// <summary>
        /// Gets a byte offset of this instruction from the beginning of the method body
        /// </summary>
        public uint ByteOffset { get { return this._ByteOffset; } }

        /// <summary>
        /// Gets ordinal number of the place this instruction takes in method body, starting from one.
        /// </summary>
        public uint OrdinalNumber { get { return this._OrdinalNumber; } }
        
        /// <summary>
        /// Gets opcode of this instruction as a numerical value
        /// </summary>
        public short Code { get { return this.OpCode.Value; } }

        /// <summary>
        /// Gets a name of this instruction
        /// </summary>
        public string Name { get { return this.OpCode.Name; } }

        /// <summary>
        /// Gets total size, in bytes, that this instruction occupies in the method body
        /// </summary>
        public uint TotalSize
        {
            get
            {
                if (this.OpCode != null) return (uint)this.OpCode.Size + OperandSize;
                else return 0;
            }
        }

        /// <summary>
        /// Creates a new CilInstruction object initialized with specified field values (infrastructure)
        /// </summary>
        /// <param name="opc">Opcode</param>
        /// <param name="operand">Operand object</param>
        /// <param name="opsize">Operand size</param>
        /// <param name="byteoffset">Byte offset</param>
        /// <param name="ordinalnum">Ordinal number</param>
        /// <param name="mb">Owning method</param>
        /// <remarks>Do not use this constructor directly. To retreive a collection of CIL instructions for the specified method, use methods of <see cref="CilReader"/> class instead.</remarks>
        public CilInstruction(
            OpCode opc, object operand=null, uint opsize=0, uint byteoffset=0, uint ordinalnum=0, MethodBase mb=null
            )
        {
            this._OpCode = opc;
            this._Operand = operand;
            this._OperandSize = opsize;
            this._ByteOffset = byteoffset;
            this._OrdinalNumber = ordinalnum;
            this._Method = mb;
        }
        
        /// <summary>
        /// Creates new CilInstruction object that represents an empty instruction
        /// </summary>
        /// <param name="mb">Owning method</param>
        /// <returns>Empty CilInstruction object</returns>
        public static CilInstruction CreateEmptyInstruction(MethodBase mb)
        {
            CilInstruction instr = new CilInstruction(OpCodes.Nop,null,0,0,0,mb);            
            return instr;
        }
        
        /// <summary>
        /// Gets this instruction's operand type, or null if there's no operand
        /// </summary>
        public Type OperandType
        {
            get
            {
                if (Operand == null) return null;
                else return Operand.GetType();
            }
        }

        /// <summary>
        /// Gets a member (type, field or method) referenced by this instruction, if applicable
        /// </summary>
        public MemberInfo ReferencedMember
        {
            get
            {

                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                Type[] t_args = null;
                Type[] m_args = null;

                //get generic type args
                if (Method.DeclaringType != null)
                {
                    if (Method.DeclaringType.IsGenericType)
                    {
                        t_args = Method.DeclaringType.GetGenericArguments();
                    }
                }

                //get generic method args
                if (Method.IsGenericMethod)
                {
                    m_args = Method.GetGenericArguments();
                }

                try
                {
                    if (ReferencesMethodToken(this.OpCode))
                    {    
                        return Method.Module.ResolveMethod((int)Operand, t_args, m_args);
                    }
                    else if (ReferencesFieldToken(this.OpCode))
                    {
                        return Method.Module.ResolveField((int)Operand, t_args, m_args);                        
                    }
                    else if (ReferencesTypeToken(this.OpCode))
                    {
                        return Method.Module.ResolveType((int)Operand, t_args, m_args);                        
                    }
                    else return null;
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve member token.";
                    OnError(this, new CilErrorEventArgs(ex, error));   
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a type referenced by this instruction, if applicable
        /// </summary>
        public Type ReferencedType
        {
            get
            { 
                return this.ReferencedMember as Type;                                
            }
        }

        /// <summary>
        /// Gets a string literal referenced by this instruction, if applicable
        /// </summary>
        public string ReferencedString
        {
            get
            {
                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                try
                {
                    if (this.OpCode.Equals(OpCodes.Ldstr))
                    {
                        string s = Method.Module.ResolveString((int)Operand);
                        return s;
                    }
                    else return null;
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve string token.";
                    OnError(this, new CilErrorEventArgs(ex, error));  
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a text representation of this instruction as a line of CIL code
        /// </summary>
        /// <returns>String containing text representation of this instruction</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();                        

            sb.Append(this.Name.PadRight(10));            

            if (Operand != null)
            {

                if (ReferencesMethodToken(this.OpCode) && this.Method != null)
                {
                    //method           
                    MethodBase called_method = this.ReferencedMember as MethodBase;

                    if (called_method != null)
                    {
                        sb.Append(' ');
                        sb.Append(CilAnalysis.MethodToString(called_method));
                    }
                }
                else if (ReferencesFieldToken(this.OpCode) && this.Method != null)
                {
                    //field                       
                    FieldInfo fi = this.ReferencedMember as FieldInfo;

                    if (fi != null)
                    {
                        Type t = fi.DeclaringType;
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeName(fi.FieldType));
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeNameInternal(t));
                        sb.Append("::");
                        sb.Append(fi.Name);
                    }
                }
                else if (ReferencesTypeToken(this.OpCode) && this.Method != null)
                {
                    //type                                         
                    Type t = this.ReferencedType;

                    if (t != null)
                    {
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeNameInternal(t));
                    }
                }
                else if (OpCode.Equals(OpCodes.Ldstr) && this.Method != null)
                {
                    //string literal

                    try
                    {
                        int token = (int)Operand;
                        string s = "";

                        s = Method.Module.ResolveString(token);
                        s = CilAnalysis.EscapeString(s);

                        sb.Append(" \"");
                        sb.Append(s);
                        sb.Append('"');

                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve string.";
                        OnError(this, new CilErrorEventArgs(ex, error));
                    }
                }
                else if ( OpCode.Equals(OpCodes.Ldtoken) && this.Method != null)
                {
                    //metadata token
                    int token = (int)Operand;

                    try
                    {
                        MemberInfo mi = Method.Module.ResolveMember(token); 
                        sb.Append(' ');
                        if (mi is Type) sb.Append(CilAnalysis.GetTypeNameInternal((Type)mi));
                        else sb.Append(mi.Name);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve token.";
                        OnError(this, new CilErrorEventArgs(ex, error));
                        sb.Append(" 0x" + token.ToString("X"));
                    }
                }
                else if (ReferencesLocal(this.OpCode))
                {
                    //local variable   

                    try
                    {
                        sb.Append(" V_" + this.Operand.ToString());
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to process local variable.";
                        OnError(this, new CilErrorEventArgs(ex, error));
                    }
                }
                else if (ReferencesParam(this.OpCode) && this.Method != null)
                {
                    //parameter   

                    try
                    {
                        int param_index = Convert.ToInt32(this._Operand);
                        ParameterInfo[] pars = this._Method.GetParameters();

                        if (this._Method.IsStatic == false) param_index--;

                        sb.Append(' ');

                        if(param_index>=0) sb.Append(pars[param_index].Name);
                        else sb.Append("this");
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to process parameter.";
                        OnError(this, new CilErrorEventArgs(ex, error));
                    }
                }
                else if (OpCode.Equals(OpCodes.Calli) && this.Method != null)
                {
                    //standalone signature token
                    int token = (int)Operand;
                    byte[] sig = null;

                    try
                    {
                        sig = Method.Module.ResolveSignature(token);                        
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve signature.";
                        OnError(this, new CilErrorEventArgs(ex, error));
                        sb.Append(" StandAloneMethodSig" + token.ToString("X"));
                    }

                    if (sig != null) //parse signature
                    {
                        sb.Append(' ');

                        try
                        {
                            StringBuilder sb_sig = new StringBuilder();
                            MemoryStream ms = new MemoryStream(sig);
                            using (ms)
                            {
                                byte b = ReadByte(ms); //calling convention & method flags

                                switch (b & CALLCONV_MASK)
                                {
                                    case CALLCONV_CDECL: sb_sig.Append("unmanaged cdecl "); break;
                                    case CALLCONV_STDCALL: sb_sig.Append("unmanaged stdcall "); break;
                                    case CALLCONV_THISCALL: sb_sig.Append("unmanaged thiscall "); break;
                                    case CALLCONV_FASTCALL: sb_sig.Append("unmanaged fastcall "); break;
                                    case CALLCONV_VARARG: sb_sig.Append("vararg "); break;
                                }

                                if ((b & MFLAG_HASTHIS) == MFLAG_HASTHIS) sb_sig.Append("instance ");
                                if ((b & MFLAG_EXPLICITTHIS) == MFLAG_HASTHIS) sb_sig.Append("explicit ");

                                uint paramcount = ReadCompressed(ms);

                                sb_sig.Append(ReadTypeSpec(ms, this.Method.Module));
                                sb_sig.Append(" (");

                                for (int i = 0; i < paramcount; i++)
                                {
                                    if (i >= 1) sb_sig.Append(", ");
                                    sb_sig.Append(ReadTypeSpec(ms, this.Method.Module));
                                }
                                sb_sig.Append(')');

                            }//end using
                            sb.Append(sb_sig.ToString());
                        }
                        catch (Exception ex)
                        {
                            string error = "Exception occured when trying to parse signature.";
                            OnError(this, new CilErrorEventArgs(ex, error));
                            sb.Append("//StandAloneMethodSig: ( ");

                            for (int i = 0; i < sig.Length; i++)
                            {
                                sb.Append(sig[i].ToString("X2"));
                                sb.Append(' ');
                            }

                            sb.Append(")");
                        }
                    }//end if (sig != null)                    
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(Convert.ToString(Operand, System.Globalization.CultureInfo.InvariantCulture));
                }
                
            }

            return sb.ToString();
        }

        /// <summary>
        /// Emits CIL code for this instruction into the specified IL generator.
        /// </summary>
        /// <param name="ilg">Target IL generator.</param>
        public void EmitTo(ILGenerator ilg)
        {
            if (this.OpCode == OpCodes.Call || this.OpCode == OpCodes.Callvirt)
            {
                ilg.EmitCall(this.OpCode, (MethodInfo)this.ReferencedMember, null);
            }
            else if (this.Operand == null)
            {
                ilg.Emit(this.OpCode);
            }
            else if (this.OperandType == typeof(float))
            {
                ilg.Emit(this.OpCode, (float)this.Operand);
            }
            else if (this.OperandType == typeof(double))
            {
                ilg.Emit(this.OpCode, (double)this.Operand);
            }
            else if (this.OperandType == typeof(long))
            {
                ilg.Emit(this.OpCode, (long)this.Operand);
            }
            else if (this.OperandType == typeof(short))
            {
                ilg.Emit(this.OpCode, (short)this.Operand);
            }
            else if (this.OperandType == typeof(int))
            {
                if (this.OpCode.Equals(OpCodes.Ldstr))
                {
                    ilg.Emit(this.OpCode, this.ReferencedString);
                }
                else if (ReferencesFieldToken(this.OpCode))
                {
                    ilg.Emit(this.OpCode, (FieldInfo)this.ReferencedMember);
                }
                else if (ReferencesTypeToken(this.OpCode))
                {
                    ilg.Emit(this.OpCode, (Type)this.ReferencedMember);
                }
                else if (ReferencesMethodToken(this.OpCode) && (this.ReferencedMember as ConstructorInfo) != null)
                {
                    ilg.Emit(this.OpCode, (ConstructorInfo)this.ReferencedMember);
                }
                else if (this.OpCode.OperandType != System.Reflection.Emit.OperandType.InlineBrTarget)
                {
                    //emit all int32-referencing instructions, except jumps
                    ilg.Emit(this.OpCode, (int)this.Operand);
                }
                else throw new NotSupportedException("OpCode not supported: " + this.OpCode.ToString());
            }
            else if (this.OperandType == typeof(sbyte) && this.OpCode.OperandType != System.Reflection.Emit.OperandType.ShortInlineBrTarget)
            {
                //emit all sbyte-referencing instructions, except jumps
                ilg.Emit(this.OpCode, (sbyte)this.Operand);
            }
            else throw new NotSupportedException("OperandType not supported: " + this.OperandType.ToString());
        }

        //*** TEXT PARSER ***

        static readonly object opcodes_sync = new object();
        static Dictionary<string, OpCode> opcodes = null;

        static void LoadOpCodes()
        {      
            FieldInfo[] fields = typeof(OpCodes).GetFields();
            opcodes = new Dictionary<string, OpCode>(fields.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(OpCode))
                {
                    OpCode opcode = (OpCode)fields[i].GetValue(null);
                    opcodes[opcode.Name] = opcode;
                }
            }
        }

        static OpCode FindOpCode(string name)
        {
            lock (opcodes_sync)
            {
                if (opcodes == null) LoadOpCodes(); //on first run, initialize static dictionary

                if (!opcodes.ContainsKey(name))
                {
                    throw new NotSupportedException("Unknown opcode: " + name);
                }
                return opcodes[name];
            }
        }        

        static uint GetOperandSize(OpCode opcode)
        {
            uint size=0;
            switch (opcode.OperandType)
            {
                case System.Reflection.Emit.OperandType.InlineBrTarget: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineField: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineMethod: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineSig: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineTok: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineType: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineI: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineI8: size = 8; break;
                case System.Reflection.Emit.OperandType.InlineNone: size = 0; break;
                case System.Reflection.Emit.OperandType.InlineR: size = 8; break;
                case System.Reflection.Emit.OperandType.InlineString: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineSwitch: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineVar: size = 2; break;
                case System.Reflection.Emit.OperandType.ShortInlineBrTarget: size = 1; break;
                case System.Reflection.Emit.OperandType.ShortInlineI: size = 1; break;
                case System.Reflection.Emit.OperandType.ShortInlineR: size = 4; break;
                case System.Reflection.Emit.OperandType.ShortInlineVar: size = 1; break;
                default:                    
                    throw new NotSupportedException("Unsupported operand type: " + opcode.OperandType.ToString());
            }
            return size;
        }

        /// <summary>
        /// Converts CIL instruction textual representation into the corresponding CilInstruction object
        /// </summary>
        /// <param name="str">The line of CIL code representing instruction</param>
        /// <returns>CilInstruction object for the specified string</returns>
        public static CilInstruction Parse(string str)
        {
            if (String.IsNullOrEmpty(str)) throw new ArgumentException("str parameter can't be null or empty string");

            CilInstruction res=null;

            List<string> tokens = new List<string>(10);
            StringBuilder curr_token = new StringBuilder(100);
            bool IsInComment = false;
            bool IsInLiteral = false;
            bool IsInToken = false;
            char c;
            char c_next;

            for(int i=0;i<str.Length;i++)
            {
                c = str[i];
                if(i+1 < str.Length)c_next = str[i+1];
                else c_next=(char)0;                

                if (!IsInToken && !IsInLiteral && !IsInComment &&
                    (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsSymbol(c))
                    && c != '/')
                {
                    //start new token
                    IsInToken = true;

                    if (c == '"') IsInLiteral = true;
                    else IsInLiteral = false;

                    curr_token = new StringBuilder(100);
                    curr_token.Append(c);
                    continue;
                }

                if (IsInToken && !IsInLiteral && Char.IsWhiteSpace(c))
                {
                    //end token
                    IsInToken = false;                    
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);
                    continue;
                }

                if (IsInToken && !IsInLiteral && 
                    (c==':' && c_next!=':')
                    )
                {
                    //end token
                    curr_token.Append(c);
                    IsInToken = false;
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);
                    continue;
                }

                if (IsInToken && !IsInLiteral && c == '/' && (c_next == '/' || c_next == '*'))
                {
                    //end token
                    IsInToken = false;                    
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);

                    //start comment
                    IsInComment = true;
                    continue;
                }

                if (IsInToken && IsInLiteral && c=='"' && str[i-1]!='\\')
                {
                    //end token
                    curr_token.Append(c);
                    IsInToken = false;
                    IsInLiteral = false;
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);
                    continue;
                }

                if (!IsInComment && !IsInToken && !IsInLiteral && c == '/' && (c_next == '/' || c_next == '*'))
                {
                    //start comment
                    IsInComment = true;
                    continue;
                }

                if (IsInComment && c == '/' && str[i - 1] == '*')
                {
                    //end comment
                    IsInComment = false;
                    continue;
                }

                if (IsInToken && !IsInLiteral && (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsSymbol(c)))
                {
                    //append new char to the token
                    curr_token.Append(c);
                }

                if (IsInToken && IsInLiteral && !(c=='"' && str[i-1]!='\\'))
                {
                    //append new char to the token
                    curr_token.Append(c);
                }
            }//end for

            if (IsInToken)
            {
                tokens.Add(curr_token.ToString());
            }
            
            if (tokens.Count == 0) return null;
            int args_start;

            string opname = tokens[0].Trim();
            args_start=1;
            if (opname[opname.Length - 1] == ':')
            {
                //skip label
                if (tokens.Count == 1) return null;
                opname = tokens[1].Trim();
                args_start=2;
            }

            string args = "";

            for(int j=args_start;j<tokens.Count;j++) args+=tokens[j];
            
            args = args.Trim();

            OpCode op = FindOpCode(opname);
            uint opsize = GetOperandSize(op);
            object operand = null;

            var numstyle = System.Globalization.NumberStyles.Integer;
            if (args.StartsWith("0x"))
            {
                numstyle = System.Globalization.NumberStyles.HexNumber;
                args = args.Substring(2);
            }

            var fmt = System.Globalization.CultureInfo.InvariantCulture;

            sbyte byteval;
            short shortval;
            float floatval;
            int intval;
            double doubleval;
            long longval;

            switch (opsize)
            {

                case 1: 
                    if(SByte.TryParse(args,numstyle,fmt, out byteval)) operand = (object)byteval;                     
                    break;

                case 2:
                    if (Int16.TryParse(args,numstyle,fmt, out shortval)) operand = (object)shortval; 
                    break;

                case 4:
                    if (op.OperandType == System.Reflection.Emit.OperandType.ShortInlineR)
                    {
                        if (Single.TryParse(args, out floatval)) operand = (object)floatval;
                    }
                    else
                    {
                        if (Int32.TryParse(args, numstyle, fmt, out intval))
                        {
                            operand = (object)intval;
                        }                        
                    }
                    break;

                case 8:
                    if (op.OperandType == System.Reflection.Emit.OperandType.InlineR)
                    {
                        if (Double.TryParse(args, out doubleval)) operand = (object)doubleval;
                    }
                    else
                    {
                        if (Int64.TryParse(args, numstyle, fmt, out longval)) operand = (object)longval;
                    }
                    break;
            }

            res = new CilInstruction(op, operand, opsize, 0, 0, null);                                   
            
            return res;
        }
    }
}
