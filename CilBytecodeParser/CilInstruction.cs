/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
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
    public class CilInstruction
    {
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
        /// Creates a new CilInstruction object initialized with specified field values
        /// </summary>
        /// <param name="opc">Opcode</param>
        /// <param name="operand">Operand object</param>
        /// <param name="opsize">Operand size</param>
        /// <param name="byteoffset">Byte offset</param>
        /// <param name="ordinalnum">Ordinal number</param>
        /// <param name="mb">Owning method</param>
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
        /// Gets a member (field or method) referenced by this instruction, if applicable
        /// </summary>
        public MemberInfo ReferencedMember
        {
            get
            {

                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                try
                {
                    if (ReferencesMethodToken(this.OpCode))
                    {
                        MethodBase method = Method.Module.ResolveMethod((int)Operand);
                        return method;
                    }
                    else if (ReferencesFieldToken(this.OpCode))
                    {

                        FieldInfo fi = Method.Module.ResolveField((int)Operand);
                        return fi;
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
                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                try
                {
                    if (ReferencesTypeToken(this.OpCode))
                    {
                        Type t = Method.Module.ResolveType((int)Operand);
                        return t;
                    }
                    else return null;
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve type token.";
                    OnError(this, new CilErrorEventArgs(ex, error));  
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a string literal referenced by this instruction, if applciable
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
     
                    int token = (int)Operand;
                    MethodBase called_method;

                    try
                    {
                        called_method = Method.Module.ResolveMethod(token);
                        Type t = called_method.DeclaringType;
                        ParameterInfo[] pars = called_method.GetParameters();

                        MethodInfo mi = called_method as MethodInfo;
                        string rt = "";
                        if (mi != null) rt = " " + CilAnalysis.GetTypeName(mi.ReturnType);

                        if (!called_method.IsStatic) sb.Append(" instance");
                                                
                        sb.Append(rt);
                        sb.Append(' ');
                        
                        sb.Append(CilAnalysis.GetTypeFullName(t));
                        sb.Append("::");
                        sb.Append(called_method.Name);
                        sb.Append('(');

                        for (int i = 0; i < pars.Length; i++)
                        {
                            if (i >= 1) sb.Append(", ");
                            sb.Append(CilAnalysis.GetTypeName(pars[i].ParameterType));                            
                        }

                        sb.Append(')');
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve method token.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (ReferencesFieldToken(this.OpCode) && this.Method != null)
                {
                    //field

                    int token = (int)Operand;
                    FieldInfo fi;

                    try
                    {
                        fi = Method.Module.ResolveField(token);
                        Type t = fi.DeclaringType;
                                           
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeFullName(t));
                        sb.Append("::");
                        sb.Append(fi.Name);
                        
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve field token.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (ReferencesTypeToken(this.OpCode) && this.Method != null)
                {
                    //type

                    int token = (int)Operand;
                    Type t;

                    try
                    {
                        t = Method.Module.ResolveType(token);  
                        
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeFullName(t));
                        
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve type token.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (OpCode.Equals(OpCodes.Ldstr) && this.Method != null)
                {
                    //string literal

                    int token = (int)Operand;
                    string s="";

                    try
                    {
                        s = Method.Module.ResolveString(token);

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
                else
                {
                    sb.Append(' ');
                    sb.Append(Operand.ToString());
                }
                
            }

            return sb.ToString();
        }
    }
}
