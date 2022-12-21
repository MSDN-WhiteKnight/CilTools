/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Runtime;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;
using CilView.Core.DocumentModel;
using CilView.Core.Syntax;

namespace CilView
{
    /// <summary>
    /// Provides synthesized assemblies that represent the content of the .il source document
    /// </summary>
    sealed class IlasmAssemblySource : AssemblySource
    {
        IlasmAssembly _ass;
        string _content;
        string _title;

        public IlasmAssemblySource(string path)
        {
            AssemblySource.TypeCacheClear();
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();
            this._content = File.ReadAllText(path);
            string title = Path.GetFileName(path);
            this._title = title;
            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(this._content);
            this._ass = IlasmParser.ParseAssembly(nodes, this._content);
            this._ass.Title = title;
            ret.Add(this._ass);
            this.Assemblies = ret;
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
        }

        public IlasmAssembly Assembly
        {
            get { return this._ass; }
        }

        public string Content
        {
            get { return this._content; }
        }

        public string Title
        {
            get { return this._title; }
        }

        public override bool HasProcessInfo => false;

        public override void Dispose() { }

        public override string GetProcessInfoString()
        {
            return string.Empty;
        }

        public override ClrThreadInfo[] GetProcessThreads()
        {
            return new ClrThreadInfo[0];
        }
    }
}
