/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Reflection;
using CilView.Common;

namespace CilView.FileSystem
{
    public class RuntimeDir
    {
        public string Name { get; set; }
        public string Path { get; set; }
        AssemblyFile[] assemblies = null;

        static RuntimeDir[] cache = null;

        public static RuntimeDir[] GetRuntimeDirs()
        {
            if (cache != null) return cache;

            List<RuntimeDir> ret = new List<RuntimeDir>(20);
            RuntimeDir rd;

            //.NET Framework current
            string curr_framework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            rd = new RuntimeDir();

            if(Environment.Version.Major>=5) rd.Name = ".NET " + Environment.Version;
            else rd.Name = ".NET Framework " + Environment.Version;

            rd.Path = curr_framework;
            ret.Add(rd);

            string path = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App";

            //.NET Core / .NET 5
            try
            {
                if (Directory.Exists(path))
                {
                    string[] dirs = Directory.GetDirectories(path);

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        if (Utils.PathEquals(curr_framework, dirs[i]))
                        {
                            continue;
                        }

                        string name = System.IO.Path.GetFileName(dirs[i]);
                        rd = new RuntimeDir();

                        Version v;

                        if (Version.TryParse(name, out v))
                        {
                            if (v.Major >= 5) rd.Name = ".NET ";
                            else rd.Name = ".NET Core ";
                        }
                        else rd.Name = ".NET Core ";

                        rd.Name += name;
                        rd.Path = dirs[i];
                        ret.Add(rd);
                    }
                }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            //.NET Framework other
            string[] fxpaths = new string[] {
                "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727",
                "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319"
            };

            string[] fxnames = new string[] {"2.0","4.0"};

            try
            {
                for (int i = 0; i < fxpaths.Length; i++)
                {
                    if (!Directory.Exists(fxpaths[i])) continue;

                    if (Utils.PathEquals(curr_framework, fxpaths[i]))
                    {
                        continue;
                    }

                    string name = fxnames[i];
                    rd = new RuntimeDir();
                    rd.Name = ".NET Framework " + name;
                    rd.Path = fxpaths[i];
                    ret.Add(rd);
                }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            RuntimeDir[] arr = ret.ToArray();
            cache = arr;
            return arr;
        }

        public async Task<AssemblyFile[]> GetAssemblies()
        {
            if (this.assemblies != null) return this.assemblies;
            List<AssemblyFile> ret;
            ret = new List<AssemblyFile>(200);

            await Task.Run(() =>
            {
                string[] files = Directory.GetFiles(this.Path, "*.dll");
                
                for (int i = 0; i < files.Length; i++)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(files[i]);

                    try
                    {
                        AssemblyName an = AssemblyName.GetAssemblyName(files[i]);
                        if (!String.IsNullOrEmpty(an.Name)) name = an.Name;
                    }
                    catch (Exception ex)
                    {
                        if (ex is BadImageFormatException)
                        {
                            continue;//not an assembly
                        }
                        else throw;
                    }

                    AssemblyFile file = new AssemblyFile();
                    file.Name = name;
                    file.Path = files[i];
                    ret.Add(file);
                }
            });

            AssemblyFile[] arr = ret.ToArray();
            this.assemblies = arr;
            return arr;
        }

        public override string ToString()
        {
            if (this.Name == null) return "(RuntimeDir)";
            else return this.Name;
        }
    }
}
