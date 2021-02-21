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
        public bool IsNetFramework { get; set; }
        AssemblyFile[] assemblies = null;

        static RuntimeDir[] cache = null;

        static HashSet<string> nativelibs = new HashSet<string>(new string[] {
"alink",
"AdoNetDiag",
"clr",
"clrcompression",
"clretwrc",
"clrjit",
"compatjit",
"CORPerfMonExt",
"Culture",
"dfdll",
"diasymreader",
"EventLogMessages",
"FileTracker",
"InstallUtilLib",
"fusion",
"nlssorting",
"normalization",
"peverify",
"SOS",
"TLBREF",
"dbgshim",
"coreclr",
"hostpolicy",
"ucrtbase",
        },StringComparer.InvariantCultureIgnoreCase);

        public static RuntimeDir[] GetRuntimeDirs()
        {
            if (cache != null) return cache;

            List<RuntimeDir> ret = new List<RuntimeDir>(20);
            RuntimeDir rd;

            //.NET Framework current
            string curr_framework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            rd = new RuntimeDir();

            if (Environment.Version.Major >= 5)
            {
                rd.Name = ".NET " + Environment.Version;
            }
            else
            {
                rd.Name = ".NET Framework " + Environment.Version;
                rd.IsNetFramework = true;
            }

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
                    rd.IsNetFramework = true;
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

        static bool IsExcludedFromAssemblies(string name)
        {
            string name_low = name.ToLower();

            //native libraries
            if (nativelibs.Contains(name_low)) return true;
            if (name_low.StartsWith("mscor") && !Utils.StringEquals(name, "mscorlib")) return true;
            if (name_low.StartsWith("csc")) return true;
            if (name_low.StartsWith("sos_amd64")) return true;
            if (name_low.Contains("native")) return true;
            
            //Windows API sets
            if (name_low.StartsWith("api-ms-win-")) return true;

            //ASP.NET native tools
            if (name_low.StartsWith("aspnet_")) return true;
            if (name_low.StartsWith("webengine")) return true;

            return false;
        }

        static bool IsSystemAssembly(string name, bool netfx)
        {
            if (netfx && name.StartsWith("mscorlib", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (name.StartsWith("System", StringComparison.InvariantCultureIgnoreCase)) return true;

            return false;
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

                    if (IsExcludedFromAssemblies(name)) continue;

                    AssemblyFile file = new AssemblyFile();
                    file.Name = name;
                    file.Path = files[i];
                    ret.Add(file);
                }
            });

            AssemblyFile[] arr = ret.ToArray();

            //Sort such as system assemblies are first on the list.
            //They are more intresting than stuff like "Accessibility" that shows up first 
            //with regular alphabetical order.

            Array.Sort<AssemblyFile>(arr, (x, y) => 
            {
                if (x.Name == null) return 0;
                if (y.Name == null) return 0;
                
                if (IsSystemAssembly(x.Name,this.IsNetFramework))
                {
                    if (!IsSystemAssembly(y.Name, this.IsNetFramework)) return -1;
                }
                else if (IsSystemAssembly(y.Name, this.IsNetFramework))
                {
                    return 1;
                }

                return String.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase);
            });

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
