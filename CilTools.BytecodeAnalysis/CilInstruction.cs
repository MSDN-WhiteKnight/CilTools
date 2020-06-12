/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.Reflection;
using System.Diagnostics;
using CilTools.Syntax;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents CIL instruction, a main structural element of the method body which consists of operation code and operand.
    /// </summary>
    /// <remarks>To retrieve a collection of CIL instructions for the specified method, use methods of <see cref="CilReader"/> class.</remarks>
    public abstract class CilInstruction
    {
        internal static bool ReferencesMethodToken(OpCode op)
        {
            return (op.Equals(OpCodes.Call) || op.Equals(OpCodes.Callvirt) || op.Equals(OpCodes.Newobj)
                || op.Equals(OpCodes.Ldftn) || op.Equals(OpCodes.Ldvirtftn));
        }

        internal static bool ReferencesFieldToken(OpCode op)
        {
            return (op.Equals(OpCodes.Stfld) || op.Equals(OpCodes.Stsfld) ||
                    op.Equals(OpCodes.Ldsfld) || op.Equals(OpCodes.Ldfld));
        }

        internal static bool ReferencesTypeToken(OpCode op)
        {
            return (op.Equals(OpCodes.Newarr) || op.Equals(OpCodes.Box) || op.Equals(OpCodes.Isinst)
                || op.Equals(OpCodes.Castclass) || op.Equals(OpCodes.Initobj)
                || op.Equals(OpCodes.Unbox) || op.Equals(OpCodes.Unbox_Any));
        }

        internal static bool ReferencesLocal(OpCode op)
        {
            return (op.Equals(OpCodes.Ldloc) || op.Equals(OpCodes.Ldloca) || op.Equals(OpCodes.Ldloc_S)
                || op.Equals(OpCodes.Ldloca_S) || op.Equals(OpCodes.Stloc) || op.Equals(OpCodes.Stloc_S));
        }

        internal static bool ReferencesParam(OpCode op)
        {
            return (op.Equals(OpCodes.Ldarg) || op.Equals(OpCodes.Ldarg_S) || op.Equals(OpCodes.Ldarga)
                || op.Equals(OpCodes.Ldarga_S));
        }

        internal static bool ReferencesToken(OpCode opc)
        {
            return opc.OperandType == System.Reflection.Emit.OperandType.InlineMethod ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineField ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineTok ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineString ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineSig ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineType;
        }

        internal static bool ReferencesMemberToken(OpCode opc)
        {
            return opc.OperandType == System.Reflection.Emit.OperandType.InlineMethod ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineField ||
                opc.OperandType == System.Reflection.Emit.OperandType.InlineTok || 
                opc.OperandType == System.Reflection.Emit.OperandType.InlineType;
        }

        /// <summary>
        /// A reference to a method which this instruction belongs to
        /// </summary>
        internal MethodBase _Method;

        /// <summary>
        /// Opcode of this instruction
        /// </summary>
        internal OpCode _OpCode;

        /// <summary>
        /// Byte offset of this instruction from the beginning of the method body
        /// </summary>
        internal uint _ByteOffset;

        /// <summary>
        /// Ordinal number of the place this instruction takes in method body, starting from one.
        /// </summary>
        internal uint _OrdinalNumber;

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
        public abstract object Operand { get; }

        /// <summary>
        /// Gets the size, in bytes, of this instruction's operand
        /// </summary>
        public abstract uint OperandSize { get; }

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
                if (this.OpCode == null) return 0;

                if (this.OpCode.OperandType == System.Reflection.Emit.OperandType.InlineSwitch)
                {
                    uint res = (uint)this.OpCode.Size + this.OperandSize;
                    int[] arr = this.Operand as int[];

                    if (arr != null) res += (uint)arr.Length * sizeof(int);

                    return res;
                }
                else return (uint)this.OpCode.Size + this.OperandSize;
            }
        }

        /// <summary>
        /// Creates a new CilInstruction object initialized with specified field values (infrastructure)
        /// </summary>
        /// <param name="opc">Opcode</param>
        /// <param name="byteoffset">Byte offset</param>
        /// <param name="ordinalnum">Ordinal number</param>
        /// <param name="mb">Owning method</param>
        protected CilInstruction(
            OpCode opc, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
        {
            this._OpCode = opc;
            this._ByteOffset = byteoffset;
            this._OrdinalNumber = ordinalnum;
            this._Method = CustomMethod.PrepareMethod(mb);
        }

        /// <summary>
        /// Creates new CilInstruction object that represents an empty instruction
        /// </summary>
        /// <param name="mb">Owning method</param>
        /// <returns>Empty CilInstruction object</returns>
        public static CilInstruction CreateEmptyInstruction(MethodBase mb)
        {
            CilInstructionImpl instr = new CilInstructionImpl(OpCodes.Nop, 0, 0, mb);
            return instr;
        }

        /// <summary>
        /// Creates new CilInstruction object for instruction with operand
        /// </summary>
        /// <typeparam name="T">Operand type</typeparam>
        /// <param name="opc">Instruction opcode</param>
        /// <param name="operand">Operand value</param>
        /// <param name="operandsize">Operand size in bytes</param>
        /// <param name="byteoffset">Byte offset</param>
        /// <param name="ordinalnum">Ordinal number</param>
        /// <param name="mb">Owning method</param>
        public static CilInstruction Create<T>(
            OpCode opc, T operand, uint operandsize, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
        {
            if (typeof(T) == typeof(int) && ReferencesToken(opc))
            {
                return new CilTokenInstruction(opc, Convert.ToInt32(operand), operandsize, byteoffset, ordinalnum, mb);
            }
            else
            {
                return new CilInstructionImpl<T>(opc, operand, operandsize, byteoffset, ordinalnum, mb);
            }
        }

        /// <summary>
        /// Creates new CilInstruction object for instruction without operand
        /// </summary>
        /// <param name="opc">Instruction opcode</param>
        /// <param name="byteoffset">Byte offset</param>
        /// <param name="ordinalnum">Ordinal number</param>
        /// <param name="mb">Owning method</param>
        public static CilInstruction Create(
            OpCode opc, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
        {
            return new CilInstructionImpl(opc, byteoffset, ordinalnum, mb);
        }

        /// <summary>
        /// Gets this instruction's operand type, or null if there's no operand
        /// </summary>
        public abstract Type OperandType { get; }

        internal MemberInfo ResolveMemberToken(int token)
        {
            if (this.Method == null) return null;

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
            
            if(ReferencesMemberToken(this.OpCode))
            {
                return (Method as CustomMethod).TokenResolver.ResolveMember(token, t_args, m_args);
            }
            else return null;
        }

        internal string ResolveStringToken(int token)
        {
            if (this.Method == null) return null;
            if (!(this.OpCode.OperandType == System.Reflection.Emit.OperandType.InlineString)) return null;

            return (Method as CustomMethod).TokenResolver.ResolveString((int)Operand);
        }

        internal Signature ResolveSignatureToken(int token)
        {
            if (this.Method == null) return null;
            if (!(this.OpCode.OperandType == System.Reflection.Emit.OperandType.InlineSig)) return null;

            //standalone signature token            
            byte[] sig = null;

            try
            {
                sig = (Method as CustomMethod).TokenResolver.ResolveSignature(token);
            }
            catch (Exception ex)
            {
                string error = "Exception occured when trying to resolve signature.";
                Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                return null;
            }

            Signature res = new Signature(sig, (Method as CustomMethod).TokenResolver);
            return res;
        }

        /// <summary>
        /// Gets a member (type, field or method) referenced by this instruction, if applicable
        /// </summary>
        public abstract MemberInfo ReferencedMember
        {
            get;
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
        public abstract string ReferencedString
        {
            get;
        }

        /// <summary>
        /// Gets a signature referenced by this instruction, if applicable
        /// </summary>
        public abstract Signature ReferencedSignature
        {
            get;
        }

        /// <summary>
        /// Gets the information about method parameter referenced by this instruction, if applicable
        /// </summary>
        public abstract ParameterInfo ReferencedParameter
        {
            get;
        }

        /// <summary>
        /// Gets the information about local variable referenced by this instruction, if applicable
        /// </summary>
        public abstract LocalVariableInfo ReferencedLocal
        {
            get;
        }

        /// <summary>
        /// Writes the text representation of this instruction's operand into the specified TextWriter
        /// </summary>
        /// <param name="target">The destination TextWriter</param>
        public abstract void OperandToString(TextWriter target);

        internal abstract IEnumerable<SyntaxNode> OperandToSyntax();
        
        /// <summary>
        /// Returns a text representation of this instruction as a line of CIL code
        /// </summary>
        /// <returns>String containing text representation of this instruction</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.Name.PadRight(10));
            sb.Append(' ');

            TextWriter wr = new StringWriter(sb);

            using (wr)
            {
                try
                {
                    this.OperandToString(wr);
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to convert operand to string.";
                    Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                }
            }

            return sb.ToString();
        }

#if !NETSTANDARD
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
                else if (ReferencesMemberToken(this.OpCode))
                {
                    if (this.ReferencedMember is MethodInfo)
                    {
                        ilg.Emit(this.OpCode, (MethodInfo)this.ReferencedMember);
                    }
                    else if (this.ReferencedMember is ConstructorInfo)
                    {
                        ilg.Emit(this.OpCode, (ConstructorInfo)this.ReferencedMember);
                    }
                    else if (this.ReferencedMember is Type)
                    {
                        ilg.Emit(this.OpCode, (Type)this.ReferencedMember);
                    }
                    else if (this.ReferencedMember is FieldInfo)
                    {
                        ilg.Emit(this.OpCode, (FieldInfo)this.ReferencedMember);
                    }
                    else
                    {
                        string s;

                        if (this.ReferencedMember != null) s = this.ReferencedMember.Name;
                        else s = "(null)";

                        throw new NotSupportedException(
                            "Unknown member type: " + s +" (OpCode: " + this.OpCode.ToString()+")"
                            );
                    }
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
#endif

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

        /// <summary>
        /// Converts CIL instruction textual representation into the corresponding CilInstruction object
        /// </summary>
        /// <param name="str">The line of CIL code representing instruction</param>
        /// <returns>CilInstruction object for the specified string</returns>
        public static CilInstruction Parse(string str)
        {
            if (String.IsNullOrEmpty(str)) throw new ArgumentException("str parameter can't be null or empty string");

            CilInstruction res = null;

            List<string> tokens = new List<string>(10);
            StringBuilder curr_token = new StringBuilder(100);
            bool IsInComment = false;
            bool IsInLiteral = false;
            bool IsInToken = false;
            char c;
            char c_next;

            for (int i = 0; i < str.Length; i++)
            {
                c = str[i];
                if (i + 1 < str.Length) c_next = str[i + 1];
                else c_next = (char)0;

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
                    (c == ':' && c_next != ':')
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

                if (IsInToken && IsInLiteral && c == '"' && str[i - 1] != '\\')
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

                if (IsInToken && IsInLiteral && !(c == '"' && str[i - 1] != '\\'))
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
            args_start = 1;
            if (opname[opname.Length - 1] == ':')
            {
                //skip label
                if (tokens.Count == 1) return null;
                opname = tokens[1].Trim();
                args_start = 2;
            }

            string args = "";

            for (int j = args_start; j < tokens.Count; j++) args += tokens[j];

            args = args.Trim();

            OpCode op = FindOpCode(opname);
            uint opsize = CilReader.GetOperandSize(op);

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

                case 0:
                    res = new CilInstructionImpl(op, 0, 0, null);
                    break;
                case 1:
                    if (SByte.TryParse(args, numstyle, fmt, out byteval))
                    {
                        res = new CilInstructionImpl<sbyte>(op, byteval, opsize, 0, 0, null);
                    }
                    break;

                case 2:
                    if (Int16.TryParse(args, numstyle, fmt, out shortval))
                    {
                        res = new CilInstructionImpl<short>(op, shortval, opsize, 0, 0, null);
                    }
                    break;

                case 4:
                    if (op.OperandType == System.Reflection.Emit.OperandType.ShortInlineR)
                    {
                        if (Single.TryParse(args, out floatval))
                        {
                            res = new CilInstructionImpl<float>(op, floatval, opsize, 0, 0, null);
                        }
                    }
                    else
                    {
                        if (Int32.TryParse(args, numstyle, fmt, out intval))
                        {
                            res = CilInstruction.Create<int>(op, intval, opsize, 0, 0, null);
                        }
                    }
                    break;

                case 8:
                    if (op.OperandType == System.Reflection.Emit.OperandType.InlineR)
                    {
                        if (Double.TryParse(args, out doubleval))
                        {
                            res = new CilInstructionImpl<double>(op, doubleval, opsize, 0, 0, null);
                        }
                    }
                    else
                    {
                        if (Int64.TryParse(args, numstyle, fmt, out longval))
                        {
                            res = new CilInstructionImpl<long>(op, longval, opsize, 0, 0, null);
                        }
                    }
                    break;
            }

            return res;
        }
    }
}
