/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
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
    }
}
