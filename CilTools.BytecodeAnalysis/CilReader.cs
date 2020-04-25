/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.Reflection;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents a state of CilReader object
    /// </summary>
    public enum CilReaderState
    {
        /// <summary>
        /// CilReader can read instructions
        /// </summary>
        Reading = 1, 

        /// <summary>
        /// CilReader is in a faulty state, because previous read operation resulted in error
        /// </summary>
        Error = 2, 

        /// <summary>
        /// CilReader reached the end of its source data
        /// </summary>
        End = 3
    }

    /// <summary>
    /// Represents an error during CIL reading operation
    /// </summary>
    public class CilParserException : ApplicationException
    {
        /// <summary>
        /// Creates new CilParserException object
        /// </summary>
        /// <param name="message">Error message for this exception</param>
        public CilParserException(string message) : base(message)
        {           
        }
    }

    /// <summary>
    /// Sequentially processes CIL bytecode, reading instructions from the method body
    /// </summary>
    public class CilReader
    {
        static Dictionary<short, OpCode> opcodes = new Dictionary<short, OpCode>();

        static void LoadOpCodes()
        {
            FieldInfo[] fields = typeof(OpCodes).GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(OpCode))
                {
                    OpCode opcode = (OpCode)fields[i].GetValue(null);
                    short code = opcode.Value;
                    opcodes[code] = opcode;
                }
            }
        }

        static OpCode FindOpCode(short val)
        {
            if (!opcodes.ContainsKey(val))
            {
                throw new NotSupportedException("Unknown opcode: 0x"+val.ToString("X"));
            }
            return opcodes[val];
        }

        static CilReader()
        {
            LoadOpCodes();
        }

        internal static uint GetOperandSize(OpCode opcode)
        {
            uint size = 0;
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
        /// Raw CIL bytes which this object processes
        /// </summary>
        protected byte[] cilbytes;

        /// <summary>
        /// Current position in the source array
        /// </summary>
        protected int current_pos = 0;

        /// <summary>
        /// An ordinal number of the next instruction
        /// </summary>
        protected uint current_ordinal = 1;

        /// <summary>
        /// Current state of this object
        /// </summary>
        protected CilReaderState state;

        /// <summary>
        /// A method which body this object reads
        /// </summary>
        protected MethodBase method;

        /// <summary>
        /// Gets a method which body this CilReader object reads
        /// </summary>
        public MethodBase Method { get { return this.method; } }

        /// <summary>
        /// Gets a current state of this CilReader object
        /// </summary>
        public CilReaderState State { get { return this.state; } }

        /// <summary>
        /// Creates new CilReader object that uses specified byte array as source
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <param name="src">An array of bytecode to read from</param>
        public CilReader(byte[] src)
        {
            if (src == null) throw new ArgumentNullException("src", "Source array cannot be null");            
            if (src.Length == 0) throw new ArgumentException("Source cannot be empty array");

            this.cilbytes = src;
            this.method = null;
            this.state = CilReaderState.Reading;
        }

        /// <summary>
        /// Creates new CilReader that uses a body of specified method as a source
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Source method is null</exception>
        /// <exception cref="CilParserException">GetMethodBody returned null</exception>
        /// <param name="src">A MethodBase object that specifies a method to read from</param>
        public CilReader(MethodBase src)
        {
            if (src == null) throw new ArgumentNullException("src","Source method cannot be null");

            CustomMethod wrapper = CustomMethod.PrepareMethod(src);
            byte[] bytecode = wrapper.GetBytecode();

            if (bytecode == null) throw new CilParserException("Cannot read method bytecode: GetBytecode returned null");

            if (bytecode.Length == 0) throw new CilParserException("Cannot read method bytecode: source array is empty");

            this.cilbytes = bytecode;
            this.method = wrapper;
            this.state = CilReaderState.Reading;
        }

        /// <summary>
        /// Reads next instruction from source
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This CilReader is in faulty state or reached the end of source byte array</exception>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <returns>CilInstruction retrieved from the source</returns>
        public CilInstruction Read()
        {
            if (this.state == CilReaderState.Error)
            {
                throw new InvalidOperationException(
                    "Cannot read from this CilReader, because previous read operation resulted in error"
                    );
            }

            if (this.state == CilReaderState.End)
            {
                throw new InvalidOperationException(
                    "Cannot read from this CilReader, because it reached the end of source byte array"
                    );
            }
            
            uint byteoffset;
            CilInstruction instr=null;
            short op;
            OpCode opcode;
            uint size = 0;

            try
            {
                //получаем код операции
                if (cilbytes[current_pos] == 0xfe)
                {
                    op = (short)(cilbytes[current_pos + 1] | 0xfe00);
                }
                else
                {
                    op = (short)(cilbytes[current_pos]);
                }

                opcode = FindOpCode(op);
                byteoffset = (uint)current_pos;
                current_pos += opcode.Size;
            }
            catch (Exception)
            {
                this.state = CilReaderState.Error;
                throw;
            }

            //найдем размер операнда
            size = GetOperandSize(opcode);

            try
            {
                if (size > 0)
                {
                    //convert operand into an appropriate type
                    byte[] operand_bytes = new byte[size];
                    Array.Copy(cilbytes, current_pos, operand_bytes, 0, size);
                    current_pos += (int)size; //пропускаем нужное число байтов

                    switch (size)
                    {
                        case 1:
                            
                            instr = new CilInstructionImpl<sbyte>(
                                opcode, (sbyte)operand_bytes[0], size, byteoffset, current_ordinal, this.Method
                                );
                            break;

                        case 2:
                            
                            instr = new CilInstructionImpl<short>(
                                opcode, BitConverter.ToInt16(operand_bytes, 0), size, byteoffset, current_ordinal, this.Method
                                );
                            break;

                        case 4:
                            if (opcode.OperandType == OperandType.InlineSwitch)
                            {
                                uint count_labels = BitConverter.ToUInt32(operand_bytes, 0);
                                int[] labels = new int[count_labels];

                                for (uint i = 0; i < count_labels; i++)
                                {
                                    operand_bytes = new byte[sizeof(int)];
                                    Array.Copy(cilbytes, current_pos, operand_bytes, 0, sizeof(int));
                                    current_pos += sizeof(int);
                                    labels[i] = BitConverter.ToInt32(operand_bytes, 0);
                                    int target = (int)byteoffset + opcode.Size + (int)size + (int)count_labels * sizeof(int) + labels[i];
                                }
                                
                                instr = new CilInstructionImpl<int[]>(
                                opcode, labels, (uint)size, byteoffset, current_ordinal, this.Method
                                );
                            }
                            else if (opcode.OperandType == OperandType.ShortInlineR)
                            {
                                instr = new CilInstructionImpl<float>(
                                 opcode, BitConverter.ToSingle(operand_bytes, 0), size, byteoffset, current_ordinal, this.Method
                                );
                            }
                            else
                            {
                                if (CilInstruction.ReferencesToken(opcode))
                                {
                                    instr = new CilTokenInstruction(
                                        opcode,
                                        BitConverter.ToInt32(operand_bytes, 0),
                                         size, byteoffset, current_ordinal, this.Method
                                        );
                                }
                                else
                                {
                                    instr = new CilInstructionImpl<int>(
                                     opcode, 
                                     BitConverter.ToInt32(operand_bytes, 0), 
                                      size, byteoffset, current_ordinal, this.Method
                                    );
                                }
                            }
                            break;

                        case 8:
                            if (opcode.OperandType == OperandType.InlineR)
                            {
                                instr = new CilInstructionImpl<double>(
                                 opcode, BitConverter.ToDouble(operand_bytes, 0), size, byteoffset, current_ordinal, this.Method
                                );
                            }
                            else
                            {
                                instr = new CilInstructionImpl<long>(
                                 opcode, BitConverter.ToInt64(operand_bytes, 0), size, byteoffset, current_ordinal, this.Method
                                );
                            }
                            break;
                    }
                }
                else
                {
                    instr = new CilInstructionImpl(opcode, byteoffset, current_ordinal, this.Method);
                }
                
                current_ordinal++;

                if (current_pos >= cilbytes.Length)
                {
                    this.state = CilReaderState.End;
                }
            }
            catch (Exception)
            {
                this.state = CilReaderState.Error;
                throw;
            }

            return instr;
        }

        /// <summary>
        /// Reads all instructions from source until the end is reached
        /// </summary>        
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Unknown error occured</exception>
        /// <returns>A collection of CIL instructions</returns>
        public IEnumerable<CilInstruction> ReadAll()
        {     
            //парсим IL-код...
            while (true)
            {
                if (this.state != CilReaderState.Reading) break;

                CilInstruction instr = this.Read();
                yield return instr;
            }

            if (this.State == CilReaderState.Error) throw new CilParserException("Error occured while reading CIL instructions");
        }

        /// <summary>
        /// Reads all instructions from specified array of bytecode
        /// </summary>
        /// <param name="src">Source byte array</param>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Unknown error occured</exception>
        /// <returns>A collection of CIL instructions</returns>
        public static IEnumerable<CilInstruction> GetInstructions(byte[] src)
        {            
            CilReader reader = new CilReader(src);
            return reader.ReadAll();
        }

        /// <summary>
        /// Reads all instructions from specified method's body
        /// </summary>
        /// <param name="m">Source method</param>
        /// <exception cref="System.ArgumentNullException">Source method is null</exception>        
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Failed to retrieve method body for the method</exception>
        /// <returns>A collection of CIL instructions that form the body of this method</returns>
        public static IEnumerable<CilInstruction> GetInstructions(MethodBase m)
        {
            CilReader reader = new CilReader(m);
            return reader.ReadAll();
        }
    }
}
