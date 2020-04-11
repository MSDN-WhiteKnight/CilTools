using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CilTools.BytecodeAnalysis;

namespace CilView
{
    sealed class FileAssemblySource:AssemblySource
    {
        public FileAssemblySource(string filepath)
        {
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();
            Assembly main = Assembly.LoadFrom(filepath);
            ret.Add(main);
            this.Assemblies = ret;
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
        }

        public override void Dispose()
        {
            //just do nothing
        }
    }
}
