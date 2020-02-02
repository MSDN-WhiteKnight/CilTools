/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilBytecodeParser
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
        /// <exception cref="CilBytecodeParser.CilParserException">GetMethodBody returned null</exception>
        /// <param name="src">A MethodBase object that specifies a method to read from</param>
        public CilReader(MethodBase src)
        {
            if (src == null) throw new ArgumentNullException("src","Source method cannot be null");   

            MethodBody mb = null;            
            mb = src.GetMethodBody();
            if (mb == null) throw new CilParserException("Cannot read method bytecode: GetMethodBody returned null");

            this.cilbytes = mb.GetILAsByteArray();
            this.method = src;
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
                throw new InvalidOperationException("Cannot read from this CilReader, because previous read operation resulted in error");
            }

            if (this.state == CilReaderState.End)
            {
                throw new InvalidOperationException("Cannot read from this CilReader, because it reached the end of source byte array");
            }
                        
            object operand=null;
            uint byteoffset;
            CilInstruction instr;     
            short op;
            OpCode opcode;
            int size = 0;

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
            switch (opcode.OperandType)
            {
                case OperandType.InlineBrTarget: size = 4; break;
                case OperandType.InlineField: size = 4; break;
                case OperandType.InlineMethod: size = 4; break;
                case OperandType.InlineSig: size = 4; break;
                case OperandType.InlineTok: size = 4; break;
                case OperandType.InlineType: size = 4; break;
                case OperandType.InlineI: size = 4; break;
                case OperandType.InlineI8: size = 8; break;
                case OperandType.InlineNone: size = 0; break;
                case OperandType.InlineR: size = 8; break;
                case OperandType.InlineString: size = 4; break;
                case OperandType.InlineSwitch: size = 4; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.ShortInlineBrTarget: size = 1; break;
                case OperandType.ShortInlineI: size = 1; break;
                case OperandType.ShortInlineR: size = 4; break;
                case OperandType.ShortInlineVar: size = 1; break;
                default:
                    this.state = CilReaderState.Error;
                    throw new NotSupportedException("Unsupported operand type: " + opcode.OperandType.ToString());
            }            

            try
            {
                if (size > 0)                
                {
                    //convert operand into an appropriate type
                    byte[] operand_bytes = new byte[size];
                    Array.Copy(cilbytes, current_pos, operand_bytes, 0, size);
                    current_pos += size; //пропускаем нужное число байтов

                    switch (size)
                    {

                        case 1: operand = (object)(sbyte)operand_bytes[0]; break;

                        case 2: operand = (object)BitConverter.ToInt16(operand_bytes, 0); break;

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
                                    int target = (int)byteoffset + opcode.Size + size + (int)count_labels*sizeof(int) + labels[i];
                                }

                                operand = (object)labels;
                            }
                            else if(opcode.OperandType == OperandType.ShortInlineR)
                                operand = (object)BitConverter.ToSingle(operand_bytes, 0); 
                            else
                                operand = (object)BitConverter.ToInt32(operand_bytes, 0);
                            break;

                        case 8: 
                            if(opcode.OperandType == OperandType.InlineR)
                                operand = (object)BitConverter.ToDouble(operand_bytes, 0); 
                            else
                                operand = (object)BitConverter.ToInt64(operand_bytes, 0); 
                            break;   
                    }
                }

                instr = new CilInstruction(opcode, operand, (uint)size, byteoffset, current_ordinal, this.Method);
                                
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
        /// <exception cref="CilBytecodeParser.CilParserException">Unknown error occured</exception>
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
        /// <exception cref="CilBytecodeParser.CilParserException">Unknown error occured</exception>
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
        /// <exception cref="CilBytecodeParser.CilParserException">Failed to retrieve method body for the method</exception>
        /// <returns>A collection of CIL instructions that form the body of this method</returns>
        public static IEnumerable<CilInstruction> GetInstructions(MethodBase m)
        {
            CilReader reader = new CilReader(m);
            return reader.ReadAll();
        }
    }
}
