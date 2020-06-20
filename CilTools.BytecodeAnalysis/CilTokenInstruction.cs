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

                if (stroperand!=null)
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

        internal override IEnumerable<SyntaxNode> OperandToSyntax()
        {
            if (ReferencesMethodToken(this.OpCode))
            {
                //method
                MethodBase called_method = this.ReferencedMember as MethodBase;

                if (called_method != null)
                {
                    yield return CilAnalysis.GetMethodRefSyntax(called_method);
                }
                else
                {
                    int token = this._operand;

                    yield return new MemberRefSyntax(
                        new SyntaxNode[]{ new IdentifierSyntax("","UnknownMethod" + token.ToString("X"),"",true)},
                        MemberTypes.Method);
                }
            }
            else if (ReferencesFieldToken(this.OpCode))
            {
                //field
                FieldInfo fi = this.ReferencedMember as FieldInfo;

                if (fi != null)
                {
                    Type t = fi.DeclaringType;
                    List<SyntaxNode> children=new List<SyntaxNode>();

                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeNameSyntax(fi.FieldType);
                    foreach (SyntaxNode node in nodes) children.Add(node);

                    children.Add(new GenericSyntax(" "));

                    nodes = CilAnalysis.GetTypeNameSyntaxInternal(t);
                    foreach (SyntaxNode node in nodes) children.Add(node);

                    children.Add(new PunctuationSyntax("", "::", ""));
                    children.Add(new IdentifierSyntax("", fi.Name, "",true));

                    yield return new MemberRefSyntax(children.ToArray(), MemberTypes.Field);
                }
                else
                {
                    int token = this._operand;

                    yield return new MemberRefSyntax(
                        new SyntaxNode[] { new IdentifierSyntax("", "UnknownField" + token.ToString("X"), "", true) },
                        MemberTypes.Field);
                }
            }
            else if (ReferencesTypeToken(this.OpCode))
            {
                //type
                Type t = this.ReferencedType;

                if (t != null)
                {
                    yield return new TypeRefSyntax(CilAnalysis.GetTypeNameSyntaxInternal(t).ToArray());
                }
                else
                {
                    int token = this._operand;

                    yield return new TypeRefSyntax(
                        new SyntaxNode[] { new IdentifierSyntax("", "UnknownType" + token.ToString("X"), "", true) }
                        );
                }
            }
            else if (OpCode.Equals(OpCodes.Ldstr))
            {
                //string literal
                int token = this._operand;

                string stroperand = this.ReferencedString;

                if (stroperand != null)
                {
                    yield return new LiteralSyntax("", stroperand, "");
                }
                else
                {
                    yield return new GenericSyntax("UnknownString" + token.ToString("X"));
                }
            }
            else if (OpCode.Equals(OpCodes.Ldtoken))
            {
                //metadata token
                int token = this._operand;

                MemberInfo mi = this.ReferencedMember;

                if (mi != null)
                {
                    if (mi is Type)
                    {
                        yield return new TypeRefSyntax(CilAnalysis.GetTypeNameSyntaxInternal((Type)mi).ToArray());
                    }
                    else
                    {
                        yield return new MemberRefSyntax(
                            new SyntaxNode[] { new IdentifierSyntax("", mi.Name, "", true) },
                            mi.MemberType);
                    }
                }
                else
                {
                    yield return new IdentifierSyntax("", "UnknownMember" + token.ToString("X"), "", true);
                }
            }
            else if (ReferencesLocal(this.OpCode))
            {
                //local variable
                yield return new IdentifierSyntax("", "V_" + this._operand.ToString(), "", false);
            }
            else if (OpCode.Equals(OpCodes.Calli) && this.Method != null)
            {
                //standalone signature token
                int token = this._operand;
                byte[] sig = null;
                SyntaxNode error_return = null;

                try
                {
                    sig = (Method as CustomMethod).TokenResolver.ResolveSignature(token);
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve signature.";
                    Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                    error_return = new GenericSyntax("StandAloneMethodSig" + token.ToString("X"));
                }

                if (error_return != null)
                {
                    yield return error_return;
                    yield break;
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
                        yield return new GenericSyntax(sg.ToString());
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(500);
                        TextWriter target = new StringWriter(sb);
                        target.Write("StandAloneMethodSig: ( ");

                        for (int i = 0; i < sig.Length; i++)
                        {
                            target.Write(sig[i].ToString("X2"));
                            target.Write(' ');
                        }

                        target.Write(')');
                        target.Flush();

                        yield return new CommentSyntax("", sb.ToString());
                    }
                } //end if (sig != null)
            }
            else
            {
                IEnumerable<SyntaxNode> nodes = base.OperandToSyntax();

                foreach (SyntaxNode node in nodes) yield return node;
            }
        }
    }
}
