/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    /// <summary>
    /// Provides static methods that return syntax trees for disassembled CIL assembler code
    /// </summary>
    public static class Disassembler
    {
        static void ArrayBytesToText(byte[] arr, TextWriter target)
        {
            if (arr == null) return;

            for (int i = 0; i < arr.Length; i++)
            {
                target.Write(arr[i].ToString("X").PadLeft(2, '0'));
                target.Write(' ');
            }

            target.Flush();
        }

        static DirectiveSyntax GetPublicKeyDirective(AssemblyName an)
        {
            // Public key or token
            StringBuilder sb;
            StringWriter wr;
            string dirname = null;
            string keyStr = null;

            if ((an.Flags & AssemblyNameFlags.PublicKey) != 0)
            {
                byte[] key = an.GetPublicKey();

                if (key != null && key.Length > 0)
                {
                    sb = new StringBuilder(100);
                    wr = new StringWriter(sb);
                    ArrayBytesToText(key, wr);
                    keyStr = sb.ToString();
                    dirname = "publickey";
                }
            }
            else
            {
                byte[] tok = an.GetPublicKeyToken();

                if (tok != null && tok.Length > 0)
                {
                    sb = new StringBuilder(100);
                    wr = new StringWriter(sb);
                    ArrayBytesToText(tok, wr);
                    keyStr = sb.ToString();
                    dirname = "publickeytoken";
                }
            }

            if (dirname != null)
            {
                SyntaxNode[] content = new SyntaxNode[] {
                        new PunctuationSyntax(string.Empty, "=", " "),
                        new PunctuationSyntax(string.Empty, "(", " "),
                        new GenericSyntax(keyStr),
                        new PunctuationSyntax(string.Empty, ")", Environment.NewLine)
                    };

                return new DirectiveSyntax("  ", dirname, content);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the collection of syntax nodes that make up an assembly manifest of the specified assembly
        /// </summary>
        public static IEnumerable<SyntaxNode> GetAssemblyManifestSyntaxNodes(Assembly ass)
        {
            SyntaxNode[] content;
            DirectiveSyntax ds;

            // Module references
            string[] modules = (string[])ReflectionInfoProperties.GetProperty(ass, ReflectionInfoProperties.ReferencedModules);

            if (modules != null)
            {
                for (int i = 0; i < modules.Length; i++)
                {
                    content = new SyntaxNode[] {
                        new KeywordSyntax(string.Empty, "extern", " ", KeywordKind.Other),
                        new IdentifierSyntax(string.Empty, modules[i], Environment.NewLine, false, null)
                    };

                    yield return new DirectiveSyntax(string.Empty, "module", content);
                }

                if (modules.Length > 0) yield return new GenericSyntax(Environment.NewLine);
            }

            // Assembly references
            AssemblyName[] refs = ass.GetReferencedAssemblies();

            for (int i = 0; i < refs.Length; i++)
            {
                content = new SyntaxNode[] {
                    new KeywordSyntax(string.Empty, "extern", " ", KeywordKind.Other),
                    new IdentifierSyntax(string.Empty, refs[i].Name, Environment.NewLine, false, null)
                };

                yield return new DirectiveSyntax(string.Empty, "assembly", content);
                yield return new PunctuationSyntax(string.Empty, "{", Environment.NewLine);

                // Public key or token
                ds = GetPublicKeyDirective(refs[i]);

                if (ds != null) yield return ds;

                // Version
                if (refs[i].Version != null)
                {
                    content = new SyntaxNode[] {
                        LiteralSyntax.CreateFromValue(string.Empty, refs[i].Version.Major, string.Empty),
                        new PunctuationSyntax(string.Empty, ":", string.Empty),
                        LiteralSyntax.CreateFromValue(string.Empty, refs[i].Version.Minor, string.Empty),
                        new PunctuationSyntax(string.Empty, ":", string.Empty),
                        LiteralSyntax.CreateFromValue(string.Empty, refs[i].Version.Build, string.Empty),
                        new PunctuationSyntax(string.Empty, ":", string.Empty),
                        LiteralSyntax.CreateFromValue(string.Empty, refs[i].Version.Revision, Environment.NewLine)
                    };

                    yield return new DirectiveSyntax("  ", "ver", content);
                }

                // Culture
                if (refs[i].CultureInfo != null && !string.IsNullOrEmpty(refs[i].CultureInfo.Name))
                {
                    content = new SyntaxNode[] {
                        LiteralSyntax.CreateFromValue(string.Empty, refs[i].CultureInfo.Name, Environment.NewLine)
                    };

                    yield return new DirectiveSyntax("  ", "culture", content);
                }

                yield return new PunctuationSyntax(string.Empty, "}", Environment.NewLine + Environment.NewLine);
            }//end for

            // Assembly definition
            AssemblyName an = ass.GetName();

            content = new SyntaxNode[] {
                new IdentifierSyntax(string.Empty, an.Name, Environment.NewLine, false, null)
            };

            yield return new DirectiveSyntax(string.Empty, "assembly", content);
            yield return new PunctuationSyntax(string.Empty, "{", Environment.NewLine);

            // Custom attributes
            SyntaxNode[] arr;

            try
            {
                arr = SyntaxNode.GetAttributesSyntax(ass, 2);
            }
            catch (Exception ex)
            {
                if (ReflectionUtils.IsExpectedException(ex))
                {
                    CommentSyntax cs = CommentSyntax.Create(SyntaxNode.GetIndentString(2),
                        "Failed to show custom attributes. " + ReflectionUtils.GetErrorShortString(ex),
                        null, false);

                    arr = new SyntaxNode[] { cs };
                    CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to show assembly custom attributes.");
                    Diagnostics.OnError(ass, ea);
                }
                else throw;
            }

            for (int j = 0; j < arr.Length; j++)
            {
                yield return arr[j];
            }

            if (arr.Length > 0) yield return new GenericSyntax(Environment.NewLine);

            // Public key or token
            ds = GetPublicKeyDirective(an);

            if (ds != null) yield return ds;

            // Hash algorithm
            if (an.HashAlgorithm != AssemblyHashAlgorithm.None)
            {
                string algStr = "0x" + ((uint)an.HashAlgorithm).ToString("X", CultureInfo.InvariantCulture);

                content = new SyntaxNode[] {
                    new KeywordSyntax(string.Empty, "algorithm", " ", KeywordKind.Other),
                    new GenericSyntax(algStr + " "),
                    CommentSyntax.Create(string.Empty, an.HashAlgorithm.ToString(), null, false)
                };

                yield return new DirectiveSyntax("  ", "hash", content);
            }

            // Version
            if (an.Version != null)
            {
                content = new SyntaxNode[] {
                    LiteralSyntax.CreateFromValue(string.Empty, an.Version.Major, string.Empty),
                    new PunctuationSyntax(string.Empty, ":", string.Empty),
                    LiteralSyntax.CreateFromValue(string.Empty, an.Version.Minor, string.Empty),
                    new PunctuationSyntax(string.Empty, ":", string.Empty),
                    LiteralSyntax.CreateFromValue(string.Empty, an.Version.Build, string.Empty),
                    new PunctuationSyntax(string.Empty, ":", string.Empty),
                    LiteralSyntax.CreateFromValue(string.Empty, an.Version.Revision, Environment.NewLine)
                };

                yield return new DirectiveSyntax("  ", "ver", content);
            }

            yield return new PunctuationSyntax(string.Empty, "}", Environment.NewLine + Environment.NewLine);

            // Manifest module definition
            Module module = ass.ManifestModule;

            if (module != null)
            {
                content = new SyntaxNode[] {
                    new IdentifierSyntax(string.Empty, module.Name, Environment.NewLine, false, null)
                };

                yield return new DirectiveSyntax(string.Empty, "module", content);

                string str = "MVID: " + module.ModuleVersionId.ToString();
                yield return CommentSyntax.Create(string.Empty, str, null, false);

                // Module custom attributes
                try
                {
                    arr = SyntaxNode.GetAttributesSyntax(module, 0);
                }
                catch (Exception ex)
                {
                    if (ReflectionUtils.IsExpectedException(ex))
                    {
                        CommentSyntax cs = CommentSyntax.Create(SyntaxNode.GetIndentString(2),
                            "Failed to show custom attributes. " + ReflectionUtils.GetErrorShortString(ex),
                            null, false);

                        arr = new SyntaxNode[] { cs };
                        CilErrorEventArgs ea = new CilErrorEventArgs(ex, "Failed to show module custom attributes.");
                        Diagnostics.OnError(ass, ea);
                    }
                    else throw;
                }

                for (int j = 0; j < arr.Length; j++)
                {
                    yield return arr[j];
                }
            }//endif
        }
    }
}
