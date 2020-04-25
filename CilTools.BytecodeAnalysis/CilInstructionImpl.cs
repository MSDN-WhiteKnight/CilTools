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

namespace CilTools.BytecodeAnalysis
{
    sealed class CilInstructionImpl : CilInstruction
    {
        public CilInstructionImpl(
            OpCode opc, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
            : base(opc, byteoffset, ordinalnum, mb)
        {
            //do nothing
        }

        public override object Operand
        {
            get { return null; }
        }

        public override uint OperandSize
        {
            get { return 0; }
        }
                
        public override Type OperandType
        {
            get { return null; }
        }

        public override MemberInfo ReferencedMember
        {
            get { return null; }
        }

        public override string ReferencedString
        {
            get { return null; }
        }

        public override Signature ReferencedSignature
        {
            get { return null; }
        }

        public override ParameterInfo ReferencedParameter
        {
            get { return null; }
        }

        public override LocalVariableInfo ReferencedLocal
        {
            get { return null; }
        }

        public override void OperandToString(TextWriter target)
        {
            //do nothing
        }
    }

    class CilInstructionImpl<T> : CilInstruction
    {
        protected T _operand;
        uint _operandsize;

        public CilInstructionImpl(
            OpCode opc,T operand,uint operandsize,uint byteoffset=0, uint ordinalnum=0, MethodBase mb=null
            ):base(opc,byteoffset,ordinalnum,mb)
        {
            this._operand = operand;
            this._operandsize = operandsize;
        }

        public override object Operand
        {
            get { return (object)this._operand; }
        }

        public T OperandValue
        {
            get { return this._operand; }
        }

        public override uint OperandSize
        {
            get { return _operandsize; }
        }

        public override Type OperandType
        {
            get
            {
                return typeof(T);
            }
        }
    
        /// <summary>
        /// Gets a member (type, field or method) referenced by this instruction, if applicable
        /// </summary>
        public override MemberInfo ReferencedMember
        {
            get
            {
                if (typeof(T) != typeof(int)) return null;
                
                try
                {
                    return ResolveMemberToken((int)Operand);
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve member token.";
                    Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a string literal referenced by this instruction, if applicable
        /// </summary>
        public override string ReferencedString
        {
            get
            {
                if (typeof(T) != typeof(int)) return null;

                try
                {
                    return this.ResolveStringToken((int)Operand);
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve string token.";
                    Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a signature referenced by this instruction, if applicable
        /// </summary>
        public override Signature ReferencedSignature
        {
            get
            {
                if (typeof(T) != typeof(int)) return null;

                try
                {
                    return this.ResolveSignatureToken((int)Operand);
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to parse signature.";
                    Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the information about method parameter referenced by this instruction, if applicable
        /// </summary>
        public override ParameterInfo ReferencedParameter
        {
            get
            {
                if (typeof(T)!=typeof(int)) return null;
                if (this._Method == null) return null;

                if (ReferencesParam(this._OpCode))
                {
                    try
                    {
                        int param_index = (int)this.Operand;
                        ParameterInfo[] pars = this._Method.GetParameters();

                        if (this._Method.IsStatic == false) param_index--;

                        if (param_index >= pars.Length) return null; //prevent IndexOutOfRangeException on our ClrMD impl

                        if (param_index >= 0) return pars[param_index];
                        else return null;
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve parameter.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        return null;
                    }
                }
                else return null;
            }
        }

        /// <summary>
        /// Gets the information about local variable referenced by this instruction, if applicable
        /// </summary>
        public override LocalVariableInfo ReferencedLocal
        {
            get
            {
                if (typeof(T) != typeof(int)) return null;
                if (this._Method == null) return null;

                if (ReferencesLocal(this._OpCode))
                {
                    try
                    {
                        int local_index = (int)this.Operand;
                        MethodBody mb = this._Method.GetMethodBody();

                        if (mb == null) return null;

                        LocalVariableInfo res = mb.LocalVariables[local_index];
                        return res;
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve local variable.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        return null;
                    }
                }
                else return null;
            }
        }

        public override void OperandToString(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            if (typeof(T) == typeof(int))
            {
                if (ReferencesMethodToken(this.OpCode))
                {
                    //method
                    MethodBase called_method = this.ReferencedMember as MethodBase;

                    if (called_method != null)
                    {
                        target.Write(CilAnalysis.MethodToString(called_method));
                    }
                    else
                    {
                        int token = (int)Operand;
                        target.Write("UnknownMethod" + token.ToString("X"));
                    }
                }
                else if (ReferencesFieldToken(this.OpCode))
                {
                    //field
                    FieldInfo fi = this.ReferencedMember as FieldInfo;

                    if (fi != null)
                    {
                        Type t = fi.DeclaringType;

                        target.Write(CilAnalysis.GetTypeName(fi.FieldType));
                        target.Write(' ');
                        target.Write(CilAnalysis.GetTypeNameInternal(t));
                        target.Write("::");
                        target.Write(fi.Name);
                    }
                    else
                    {
                        int token = (int)Operand;
                        target.Write("UnknownField" + token.ToString("X"));
                    }
                }
                else if (ReferencesTypeToken(this.OpCode))
                {
                    //type
                    Type t = this.ReferencedType;

                    if (t != null)
                    {
                        target.Write(CilAnalysis.GetTypeNameInternal(t));
                    }
                    else
                    {
                        int token = (int)Operand;
                        target.Write("UnknownType" + token.ToString("X"));
                    }
                }
                else if (OpCode.Equals(OpCodes.Ldstr))
                {
                    //string literal
                    int token = (int)Operand;

                    string stroperand = this.ReferencedString;

                    if (!String.IsNullOrEmpty(stroperand))
                    {
                        stroperand = "\"" + CilAnalysis.EscapeString(stroperand) + "\"";
                        target.Write(stroperand);
                    }
                    else
                    {
                        target.Write("UnknownString" + token.ToString("X"));
                    }
                }
                else if (OpCode.Equals(OpCodes.Ldtoken))
                {
                    //metadata token
                    int token = (int)Operand;

                    MemberInfo mi = this.ReferencedMember;

                    if (mi != null)
                    {
                        if (mi is Type) target.Write(CilAnalysis.GetTypeNameInternal((Type)mi));
                        else target.Write(mi.Name);
                    }
                    else
                    {
                        target.Write("UnknownMember" + token.ToString("X"));
                    }
                }
                else if (ReferencesLocal(this.OpCode))
                {
                    //local variable
                    target.Write("V_" + this.Operand.ToString());
                }
                else if (ReferencesParam(this.OpCode) && this.Method != null)
                {
                    //parameter
                    ParameterInfo par = this.ReferencedParameter;

                    if (par != null)
                    {
                        if (String.IsNullOrEmpty(par.Name)) target.Write("par" + (par.Position + 1).ToString());
                        else target.Write(par.Name);
                    }
                    else
                    {
                        target.Write("par" + this.Operand.ToString());
                    }
                }
                else if (OpCode.Equals(OpCodes.Calli) && this.Method != null)
                {
                    //standalone signature token
                    int token = (int)Operand;
                    byte[] sig = null;

                    try
                    {
                        sig = (Method as CustomMethod).TokenResolver.ResolveSignature(token);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve signature.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        target.Write("StandAloneMethodSig" + token.ToString("X"));
                    }

                    if (sig != null) //parse signature
                    {
                        Signature sg = null;

                        try
                        {
                            sg = new Signature(sig, (Method as CustomMethod).TokenResolver);
                        }
                        catch (Exception ex)
                        {
                            string error = "Exception occured when trying to parse signature.";
                            Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        }

                        if (sg != null)
                        {
                            target.Write(sg.ToString());
                        }
                        else
                        {
                            target.Write("//StandAloneMethodSig: ( ");

                            for (int i = 0; i < sig.Length; i++)
                            {
                                target.Write(sig[i].ToString("X2"));
                                target.Write(' ');
                            }

                            target.Write(')');
                        }
                    }//end if (sig != null)
                }
                else
                {
                    target.Write(Convert.ToString(Operand, System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else if (typeof(T) == typeof(int[]) && OpCode.OperandType == System.Reflection.Emit.OperandType.InlineSwitch)
            {
                int[] labels = (int[])this.Operand;

                target.Write('(');

                for (int i = 0; i < labels.Length; i++)
                {
                    if (i >= 1) target.Write(", ");
                    int targ = (int)this._ByteOffset + (int)this.TotalSize + labels[i];
                    target.Write("0x");
                    target.Write(targ.ToString("X4"));
                }

                target.Write(')');
            }
            else
            {
                target.Write(Convert.ToString(Operand, System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}
