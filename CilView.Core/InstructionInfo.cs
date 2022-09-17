/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilView.Core
{
    public static class InstructionInfo
    {
        static byte[] GetMethodBytecodeForInstruction(CilInstruction instr)
        {
            MethodBase mb = instr.Method;
            ICustomMethod cm = mb as ICustomMethod;

            if (cm == null) return new byte[0];
            
            return cm.GetBytecode();
        }

        static byte[] GetOpCodeBytes(CilInstruction instr)
        {
            if (instr.OpCode.Size == 1)
            {
                return new byte[] { (byte)instr.Code };
            }
            else
            {
                return BitConverter.GetBytes(instr.Code);
            }
        }

        static byte[] GetOperandBytes(CilInstruction instr)
        {
            if (instr.OperandSize == 0) return new byte[0];

            byte[] bytecode = GetMethodBytecodeForInstruction(instr);

            int offset = (int)instr.ByteOffset + instr.OpCode.Size;
            int size = (int)instr.OperandSize;

            Debug.Assert(offset <= bytecode.Length);
            Debug.Assert(offset + size <= bytecode.Length);
            
            byte[] ret = new byte[size];
            Array.Copy(bytecode, offset, ret, 0, size);
            return ret;
        }

        static bool IsOperandToken(CilInstruction instr)
        {
            OpCode opc = instr.OpCode;

            return opc.OperandType == OperandType.InlineMethod ||
                opc.OperandType == OperandType.InlineField ||
                opc.OperandType == OperandType.InlineTok ||
                opc.OperandType == OperandType.InlineString ||
                opc.OperandType == OperandType.InlineSig ||
                opc.OperandType == OperandType.InlineType;
        }

        public static string GetInstructionInfo(CilInstruction instr)
        {
            StringBuilder sb = new StringBuilder(1000);
            sb.Append("Opcode: " + instr.Name);
            sb.AppendLine(" (0x" + instr.Code.ToString("X") + ")");
            sb.AppendLine("Opcode type: " + instr.OpCode.OpCodeType.ToString());

            byte[] arr = GetOpCodeBytes(instr);
            sb.Append("Opcode bytes: ");

            for (int i = 0; i < arr.Length; i++)
            {
                sb.Append(arr[i].ToString("X").PadLeft(2, '0'));
                sb.Append(' ');
            }

            sb.AppendLine();
            sb.AppendLine();

            if (instr.Operand != null)
            {
                if (IsOperandToken(instr))
                {
                    sb.AppendLine("Operand: 0x" + ((int)instr.Operand).ToString("X"));
                }
                else
                {
                    sb.AppendLine("Operand: " + instr.Operand.ToString());
                }

                sb.AppendLine("Operand type: " + instr.OpCode.OperandType.ToString());
                sb.AppendLine("Operand size, bytes: " + instr.OperandSize.ToString());

                arr = InstructionInfo.GetOperandBytes(instr);
                sb.Append("Operand bytes: ");

                for (int i = 0; i < arr.Length; i++)
                {
                    sb.Append(arr[i].ToString("X").PadLeft(2, '0'));
                    sb.Append(' ');
                }

                sb.AppendLine();

                if (instr.ReferencedMember != null)
                {
                    if (instr.ReferencedMember is Type)
                    {
                        sb.AppendLine("Member type: Type");
                        sb.AppendLine("Member name: " + instr.ReferencedType.FullName);
                    }
                    else
                    {
                        sb.AppendLine("Member type: " + instr.ReferencedMember.MemberType.ToString());
                        sb.AppendLine("Member name: " + instr.ReferencedMember.Name);
                    }
                }
                else if (instr.ReferencedString != null)
                {
                    sb.AppendLine("String: " + instr.ReferencedString);
                }
            }
            else
            {
                sb.AppendLine("Operand size, bytes: 0");
            }

            sb.AppendLine();
            sb.AppendLine("Ordinal number: " + instr.OrdinalNumber.ToString());
            sb.AppendLine("Byte offset: " + instr.ByteOffset.ToString());
            sb.AppendLine("Total size, bytes: " + instr.TotalSize.ToString());
            return sb.ToString();
        }
    }
}
