/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.Reflection;
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
            
            foreach (SyntaxNode node in this.OperandToSyntax(DisassemblerParams.Default)) node.ToText(target);

            target.Flush();
        }

        internal override IEnumerable<SyntaxNode> OperandToSyntax(DisassemblerParams pars)
        {
            Assembly containingAssembly;

            // If we need to assembly-qualify all types, just pretend that we don't know the
            // containing assembly.
            if (pars.AssemblyQualifyAllTypes) containingAssembly = null;
            else containingAssembly = ReflectionUtils.GetContainingAssembly(this._Method);

            if (ReferencesMethodToken(this.OpCode))
            {
                //method
                MethodBase called_method = this.ReferencedMember as MethodBase;

                if (called_method != null)
                {
                    yield return CilAnalysis.GetMethodRefSyntax(called_method, inlineTok: false, 
                        forceTypeSpec: false, skipAssembly: false, containingAssembly);
                }
                else
                {
                    int token = this._operand;

                    yield return new IdentifierSyntax("","UnknownMethod" + token.ToString("X"),"",true,null);
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

                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeNameSyntax(fi.FieldType, containingAssembly);
                    foreach (SyntaxNode node in nodes) children.Add(node);

                    children.Add(new GenericSyntax(" "));

                    //append declaring type
                    if (t != null && !CilAnalysis.IsModuleType(t))
                    {
                        nodes = CilAnalysis.GetTypeSpecSyntaxAuto(t, skipAssembly: false);
                        foreach (SyntaxNode node in nodes) children.Add(node);

                        children.Add(new PunctuationSyntax("", "::", ""));
                    }

                    //append field name
                    children.Add(new IdentifierSyntax("", fi.Name, "",true,fi));

                    yield return new MemberRefSyntax(children.ToArray(), fi);
                }
                else
                {
                    int token = this._operand;
                    
                    yield return new IdentifierSyntax("", "UnknownField" + token.ToString("X"), "", true,null);
                }
            }
            else if (ReferencesTypeToken(this.OpCode))
            {
                //type
                Type t = this.ReferencedType;

                if (t != null)
                {
                    IEnumerable<SyntaxNode> referencedTypeNodes = CilAnalysis.GetTypeSpecSyntaxAuto(t, skipAssembly: false);
                    yield return new MemberRefSyntax(referencedTypeNodes.ToArray(), t);
                }
                else
                {
                    int token = this._operand;
                    
                    yield return new IdentifierSyntax("", "UnknownType" + token.ToString("X"), "", true,null);
                }
            }
            else if (OpCode.Equals(OpCodes.Ldstr))
            {
                //string literal
                int token = this._operand;

                string stroperand = this.ReferencedString;

                if (stroperand != null)
                {
                    yield return LiteralSyntax.CreateFromValue("", stroperand, "");
                }
                else
                {
                    yield return new GenericSyntax("UnknownString" + token.ToString("X"));
                }
            }
            else if (OpCode.Equals(OpCodes.Ldtoken))
            {
                //metadata token (ECMA-335 VI.C.4.13)
                int token = this._operand;

                MemberInfo mi = this.ReferencedMember;

                if (mi != null)
                {
                    if (mi is TypeSpec)
                    {
                        SyntaxNode[] nodes = CilAnalysis.GetTypeSpecSyntaxAuto((Type)mi, skipAssembly: false).ToArray();
                        yield return new MemberRefSyntax(nodes, mi);
                    }
                    else if (mi is Type)
                    {
                        //use TypeSpec syntax to avoid resolving external references
                        SyntaxNode[] nodes = CilAnalysis.GetTypeSyntax((Type)mi, isspec: true, skipAssembly: false).ToArray();
                        yield return new MemberRefSyntax(nodes, mi);
                    }
                    else if (mi is FieldInfo)
                    {
                        FieldInfo fi = (FieldInfo)mi;
                        Type t = fi.DeclaringType;
                        List<SyntaxNode> children = new List<SyntaxNode>();
                        children.Add(new KeywordSyntax("", "field", " ", KeywordKind.Other));

                        IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeNameSyntax(fi.FieldType, containingAssembly);
                        foreach (SyntaxNode node in nodes) children.Add(node);

                        children.Add(new GenericSyntax(" "));

                        nodes = CilAnalysis.GetTypeSpecSyntaxAuto(t, skipAssembly: false);
                        foreach (SyntaxNode node in nodes) children.Add(node);

                        children.Add(new PunctuationSyntax("", "::", ""));
                        children.Add(new IdentifierSyntax("", fi.Name, "", true, fi));

                        yield return new MemberRefSyntax(children.ToArray(), fi);
                    }
                    else if (mi is MethodBase)
                    {
                        MethodBase mb = (MethodBase)mi;
                        yield return CilAnalysis.GetMethodRefSyntax(mb, inlineTok: true, forceTypeSpec: false, 
                            skipAssembly: false, containingAssembly);
                    }
                    else
                    {
                        yield return new MemberRefSyntax(
                            new SyntaxNode[] { new IdentifierSyntax("", mi.Name, "", true, mi) },
                            mi);
                    }
                }
                else
                {
                    yield return new IdentifierSyntax(
                        "", "UnknownMember" + token.ToString("X"), "", true,null
                        );
                }
            }
            else if (ReferencesLocal(this.OpCode))
            {
                //local variable
                yield return new IdentifierSyntax("", "V_" + this._operand.ToString(), "", false,null);
            }
            else if (OpCode.Equals(OpCodes.Calli) && this.Method != null)
            {
                //standalone signature token
                int token = this._operand;
                byte[] sig = null;
                SyntaxNode error_return = null;

                try
                {
                    sig = (Method as ICustomMethod).TokenResolver.ResolveSignature(token);
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
                        sg = new Signature(sig, (Method as ICustomMethod).TokenResolver);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to parse signature.";
                        Diagnostics.OnError(this, new CilErrorEventArgs(ex, error));
                    }

                    if (sg != null)
                    {
                        IEnumerable<SyntaxNode> nodes = sg.ToSyntax(pointer: false, containingAssembly);
                        foreach (SyntaxNode node in nodes) yield return node;
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

                        yield return CommentSyntax.Create("", sb.ToString(), null, false);
                    }
                } //end if (sig != null)
            }
            else
            {
                IEnumerable<SyntaxNode> nodes = base.OperandToSyntax(pars);

                foreach (SyntaxNode node in nodes) yield return node;
            }
        }
    }
}
