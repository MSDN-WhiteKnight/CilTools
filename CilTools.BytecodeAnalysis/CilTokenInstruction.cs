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
    sealed class CilTokenInstruction:CilInstructionImpl<int>
    {
        public CilTokenInstruction(
            OpCode opc, int operand, uint operandsize, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
            : base(opc,operand,operandsize, byteoffset, ordinalnum, mb)
        {
            //do nothing
        }

        //optimized implementations of Referenced... line of properties - to avoid boxing/unboxing of integer operand

        object _obj = null; //referenced object

        public override MemberInfo ReferencedMember
        {
            get
            {
                if (this._obj == null)
                {
                    try
                    {
                        this._obj = this.ResolveMemberToken(this._operand);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve member token.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        return null;
                    }
                }

                return this._obj as MemberInfo;
            }
        }

        /// <summary>
        /// Gets a string literal referenced by this instruction, if applicable
        /// </summary>
        public override string ReferencedString
        {
            get
            {
                if (this._obj == null)
                {
                    try
                    {
                        this._obj = this.ResolveStringToken(this._operand);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve string token.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        return null;
                    }
                }

                return this._obj as string;
            }
        }

        /// <summary>
        /// Gets a signature referenced by this instruction, if applicable
        /// </summary>
        public override Signature ReferencedSignature
        {
            get
            {
                if (this._obj == null)
                {
                    try
                    {
                        this._obj = this.ResolveSignatureToken(this._operand);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to parse signature.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                        return null;
                    }
                }

                return this._obj as Signature;
            }
        }

        public override void OperandToString(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

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
                    int token = this._operand;
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
                    int token = this._operand;
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
                    int token = this._operand;
                    target.Write("UnknownType" + token.ToString("X"));
                }
            }
            else if (OpCode.Equals(OpCodes.Ldstr))
            {
                //string literal
                int token = this._operand;

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
                int token = this._operand;

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
                target.Write("V_" + this._operand.ToString());
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
                    target.Write("par" + this._operand.ToString());
                }
            }
            else if (OpCode.Equals(OpCodes.Calli) && this.Method != null)
            {
                //standalone signature token
                int token = this._operand;
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
                } //end if (sig != null)
            }
            else
            {
                base.OperandToString(target);
            }
        }
    }
}
