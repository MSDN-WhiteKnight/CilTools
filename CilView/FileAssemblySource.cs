﻿using System;
using System.IO;
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
        string _path; //assembly directory

        HashSet<string> resolved = new HashSet<string>();

        Assembly LoadAssembly(string filepath)
        {
            Assembly main = null;

            //try load to execution context
            try
            {
                main = Assembly.LoadFile(filepath);
            }
            catch(BadImageFormatException) { }
            catch(NotSupportedException) { }

            if (main != null)
            {
                //Assembly.LoadFile can load wrong assembly from GAC

                if (String.Equals(main.Location, filepath, StringComparison.InvariantCultureIgnoreCase))
                {
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    return main;
                }
                else main = null;
            }

            //if failed, try reflection only context
            main = Assembly.ReflectionOnlyLoadFrom(filepath);
            
            if (main != null)
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
                return main;
            }
            else return null;
        }

        public FileAssemblySource(string filepath)
        {
            this._path = Path.GetDirectoryName(filepath);
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();

            Assembly main=null;

            main = this.LoadAssembly(filepath);
            
            if (main == null) throw new ApplicationException("Cannot load assembly " + filepath + " due to unknown error!");

            ret.Add(main);
            this.Assemblies = ret;
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            string path = Path.Combine(this._path, an.Name + ".dll");
            Assembly ret = null;

            //attempt to resolve assembly from current assembly directory

            try
            {
                if (File.Exists(path)) ret = Assembly.LoadFile(path);
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }
            catch (NotSupportedException) { }

            if (ret != null) return ret;

            if(resolved.Contains(args.Name)) return null; //prevent stack overflow

            //if failed, resolve by full assembly name
            resolved.Add(args.Name);
            return Assembly.Load(args.Name);
        }

        Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            string path = Path.Combine(this._path, an.Name + ".dll");
            Assembly ret = null;

            //atempt to resolve assembly from current assembly directory

            try
            {
                if (File.Exists(path)) ret = Assembly.ReflectionOnlyLoadFrom(path);
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }

            if (ret != null) return ret;

            if (resolved.Contains(args.Name)) return null; //prevent stack overflow

            //if failed, resolve by full assembly name
            resolved.Add(args.Name);
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        public override void Dispose()
        {
            //just do nothing
        }
    }
}
