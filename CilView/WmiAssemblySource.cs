/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Text;
using CilTools.Metadata;
using CilView.Common;
using CilView.FileSystem;

namespace CilView
{
    class WmiAssemblySource : AssemblySource
    {
        Process process;
        AssemblyReader rd;
        OperationBase op;

        public WmiAssemblySource(Process pr, OperationBase op = null)
        {
            this.process = pr;
            this.op = op;
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
            this.rd = new AssemblyReader();
            this.Assemblies = this.LoadAssemblies(pr.Id);
        }
        
        static string[] GetProcessModules(int id, OperationBase op = null)
        {
            if (op != null)
            {
                op.ReportProgress("Loading process modules...", 0, 0);
                op.DoEvents();
                if (op.Stopped) return new string[0];
            }

            List<string> res = new List<string>(100);

            string query = "references of {win32_process.Handle=" + id.ToString() +
                "} WHERE ResultClass = CIM_ProcessExecutable";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            using (ManagementObjectCollection results = searcher.Get())
            {
                int i = 0;

                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                windir = windir.ToLowerInvariant();
                string sysdir = Path.Combine(windir, "system32");
                string winsxs = Path.Combine(windir, "winsxs");

                foreach (ManagementObject x in results)
                {
                    try
                    {
                        i++;
                        ManagementObject antecedent = new ManagementObject((string)x["Antecedent"]);
                        if (antecedent["Name"] == null) continue;
                        string name = antecedent["Name"].ToString().Trim();
                        if (name.Length == 0) continue;

                        //exclude common native OS library paths
                        string name_lower = name.ToLowerInvariant();
                        if (name_lower.StartsWith(sysdir)) continue;
                        if (name_lower.StartsWith(winsxs)) continue;

                        //exclude common native runtime libraries
                        string shortname = Path.GetFileNameWithoutExtension(name);

                        if (!Utils.StringEquals(shortname.ToLowerInvariant(), "mscorlib.ni"))
                        {
                            if (RuntimeDir.IsNativeLibrary(shortname)) continue;
                        }
                        
                        res.Add(name);

                        if (op != null)
                        {
                            op.Window.ReportProgress(
                                "Loading " + shortname + "...", i, results.Count
                                );

                            op.DoEvents();
                            if (op.Stopped) return res.ToArray();
                        }
                    }
                    catch (ManagementException) { }
                }
            }

            return res.ToArray();
        }

        ObservableCollection<Assembly> LoadAssemblies(int pid)
        {
            //get process modules (managed and umnanaged) with WMI
            string[] modules = GetProcessModules(pid,this.op);

            if (this.op != null)
            {
                op.ReportProgress("Reading metadata...", 0, 0);
                op.DoEvents();
                if (op.Stopped) return new ObservableCollection<Assembly>();
            }

            List<Assembly> ret = new List<Assembly>();
            
            string name;

            for(int i=0;i<modules.Length;i++)
            {
                name = String.Empty;
                string path = modules[i];
                string dir = Path.GetDirectoryName(path).ToLower();

                if (!dir.Contains("assembly\\gac") && !dir.Contains("assembly\\nativeimages"))
                {
                    this.rd.AddResolutionDirectory(dir);
                }

                Assembly ass = null;

                //load with CilTools.Metadata
                if (!path.EndsWith(".ni.dll"))
                {
                    name = Path.GetFileNameWithoutExtension(path);

                    //try full path
                    try
                    {
                        ass = rd.LoadFrom(path);
                    }
                    catch (FileNotFoundException) { }
                    catch (InvalidOperationException) { }

                    if (ass == null)
                    {
                        //try name
                        
                        try
                        {
                            ass = rd.Load(name);
                        }
                        catch (FileNotFoundException) { }
                        catch (InvalidOperationException) { }
                    }
                }
                else
                {
                    //native image - resolve using name instead of path
                    name = Path.GetFileName(path);
                    int endIndex = name.Length - 7; //".ni.dll".Length

                    if (endIndex > 0)
                    {
                        name = name.Substring(0, endIndex);
                    }

                    try
                    {
                        ass = rd.Load(name);
                    }
                    catch (FileNotFoundException) { }
                    catch (InvalidOperationException) { }
                }

                if (this.op != null)
                {
                    op.Window.ReportProgress("Reading " + name + "...", i, modules.Length);
                    op.DoEvents();
                    if (op.Stopped) return new ObservableCollection<Assembly>(ret);
                }

                if (ass!=null)ret.Add(ass);
            }//end for

            if (ret.Count == 0)
            {
                throw new ArgumentException("Process does not have managed assemblies");
            }

            ret.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.InvariantCulture));
            
            return new ObservableCollection<Assembly>(ret);
        }

        public override bool HasProcessInfo
        {
            get { return false; }
        }

        public override string GetProcessInfoString()
        {
            return "";
        }

        public override CilTools.Runtime.ClrThreadInfo[] GetProcessThreads()
        {
            return new CilTools.Runtime.ClrThreadInfo[0];
        }

        public override void Dispose()
        {
            if (this.rd != null)
            {
                this.process.Dispose();
                this.rd.Dispose();
                this.rd = null;
            }
        }
    }
}
