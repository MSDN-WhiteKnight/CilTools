/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilView.Core.Syntax;

namespace CilView.Core.DocumentModel
{
    /// <summary>
    /// Synthesized assembly that represents the content of parsed CIL assembler source text
    /// </summary>
    public class IlasmAssembly : Assembly
    {
        DocumentSyntax _wholeDocument;
        List<IlasmType> _types;
        AssemblyName _asn;

        public IlasmAssembly(DocumentSyntax doc, string name)
        {
            this._wholeDocument = doc;
            this._types = new List<IlasmType>();
            this._asn = new AssemblyName(name);
        }

        public DocumentSyntax Syntax
        {
            get { return this._wholeDocument; }
        }

        public string GetDocumentText()
        {
            return this._wholeDocument.ToString();
        }

        public void AddType(IlasmType type)
        {
            this._types.Add(type);
        }

        public override AssemblyName GetName()
        {
            return this._asn;
        }
        
        public override string FullName => this._asn.FullName; // Called by WPF - must not throw!

        public override Type[] GetTypes()
        {
            return this._types.ToArray();
        }

        public override bool IsDynamic { get { return false; } }

        public override bool ReflectionOnly { get { return true; } }

        public override string CodeBase => string.Empty;

        public override string Location => string.Empty;
    }
}
