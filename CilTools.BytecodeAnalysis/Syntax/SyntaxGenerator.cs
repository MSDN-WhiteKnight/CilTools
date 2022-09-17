/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    internal static class SyntaxGenerator
    {
        public static SyntaxNode[] GetGenericParameterSyntax(Type t)
        {
            if (!t.IsGenericParameter)
            {
                return new SyntaxNode[]{
                    new IdentifierSyntax(string.Empty, CilAnalysis.GetTypeName(t), string.Empty, false, null) 
                };
            }

            //ECMA-335 II.10.1.7 Generic parameters
            List<SyntaxNode> ret = new List<SyntaxNode>();

            if ((t.GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
            {
                ret.Add(new PunctuationSyntax(string.Empty, "+", " "));
            }
            else if ((t.GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
            {
                ret.Add(new PunctuationSyntax(string.Empty, "-", " "));
            }

            if ((t.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                ret.Add(new KeywordSyntax(string.Empty, "class", " ", KeywordKind.Other));
            }
            else if((t.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                ret.Add(new KeywordSyntax(string.Empty, "valuetype", " ", KeywordKind.Other));
            }

            if ((t.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                ret.Add(new KeywordSyntax(string.Empty, ".ctor", " ", KeywordKind.Other));
            }

            Type[] constrs = t.GetGenericParameterConstraints();

            if (constrs.Length > 0)
            {
                ret.Add(new PunctuationSyntax(string.Empty, "(", string.Empty));

                for (int i = 0; i < constrs.Length; i++)
                {
                    if(i>=1) ret.Add(new PunctuationSyntax(string.Empty, ",", " "));

                    IEnumerable<SyntaxNode> nodes = CilAnalysis.GetTypeSpecSyntax(constrs[i]);

                    foreach (SyntaxNode node in nodes) ret.Add(node);
                }

                ret.Add(new PunctuationSyntax(string.Empty, ")", " "));
            }

            ret.Add(new IdentifierSyntax(string.Empty, t.Name, string.Empty, false, null));
            return ret.ToArray();
        }

        public static IEnumerable<SyntaxNode> GetEventsSyntax(Type t, int startIndent)
        {
            //ECMA_335 II.18 - Defining events
            EventInfo[] events = t.GetEvents(ReflectionUtils.AllMembers);
            
            for (int i = 0; i < events.Length; i++)
            {
                List<SyntaxNode> inner = new List<SyntaxNode>(10);

                if (events[i].IsSpecialName)
                {
                    inner.Add(new KeywordSyntax(string.Empty, "specialname", " ", KeywordKind.Other));
                }

                inner.Add(new MemberRefSyntax(CilAnalysis.GetTypeNameSyntax(events[i].EventHandlerType).ToArray(), 
                    events[i].EventHandlerType));

                inner.Add(new IdentifierSyntax(" ", events[i].Name, Environment.NewLine, true, events[i]));
                inner.Add(new PunctuationSyntax(SyntaxNode.GetIndentString(startIndent + 1), "{", Environment.NewLine));

                //custom attributes
                try
                {
                    SyntaxNode[] arr = SyntaxNode.GetAttributesSyntax(events[i], startIndent + 2);

                    for (int j = 0; j < arr.Length; j++)
                    {
                        inner.Add(arr[j]);
                    }
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        CommentSyntax cs = CommentSyntax.Create(SyntaxNode.GetIndentString(startIndent + 2),
                            "Failed to show custom attributes. " + ReflectionUtils.GetErrorShortString(ex), 
                            null, false);

                        inner.Add(cs);
                        CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to show event custom attributes.");
                        Diagnostics.OnError(events[i], ea);
                    }
                    else throw;
                }

                //accessors
                MethodInfo adder = events[i].GetAddMethod(true);

                if (adder != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(adder, false);

                    DirectiveSyntax dirAdd = new DirectiveSyntax(SyntaxNode.GetIndentString(startIndent + 2),
                        "addon", new SyntaxNode[] { mref });

                    inner.Add(dirAdd);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                MethodInfo remover = events[i].GetRemoveMethod(true);

                if (remover != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(remover, false);

                    DirectiveSyntax dirRemove = new DirectiveSyntax(SyntaxNode.GetIndentString(startIndent + 2),
                        "removeon", new SyntaxNode[] { mref });

                    inner.Add(dirRemove);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                MethodInfo raiser = events[i].GetRaiseMethod(true);

                if (raiser != null)
                {
                    MemberRefSyntax mref = CilAnalysis.GetMethodRefSyntax(raiser, false);

                    DirectiveSyntax dirFire = new DirectiveSyntax(SyntaxNode.GetIndentString(startIndent + 2),
                        "fire", new SyntaxNode[] { mref });

                    inner.Add(dirFire);
                    inner.Add(new GenericSyntax(Environment.NewLine));
                }

                PunctuationSyntax ps = new PunctuationSyntax(SyntaxNode.GetIndentString(startIndent + 1), "}",
                    Environment.NewLine + Environment.NewLine);
                inner.Add(ps);

                DirectiveSyntax dirEvent = new DirectiveSyntax(SyntaxNode.GetIndentString(startIndent + 1), 
                    "event", inner.ToArray());
                yield return dirEvent;
            }//end for
        }
    }
}
