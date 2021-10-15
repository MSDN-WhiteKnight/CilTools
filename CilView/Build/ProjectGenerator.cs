/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilView.Build
{
    public enum CodeLanguage 
    {
        CSharp=1,VB=2
    }

    class LangItem 
    {
        public CodeLanguage Language { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.Name)) return this.Name;
            else return base.ToString();
        }
    }

    static class ProjectGenerator
    {
        /// <summary>
        /// Creates a project that can be used to build the specified code file
        /// </summary>
        /// <param name="codeFile">Path to C# or VB code file</param>
        /// <returns>Path to generated project</returns>
        public static string CreateProject(string codeFile) 
        {
            string name = Path.GetFileNameWithoutExtension(codeFile);
            string ext = Path.GetExtension(codeFile);
            CodeLanguage lang;

            switch (ext.ToLower()) 
            {
                case ".cs": lang = CodeLanguage.CSharp; break;
                case ".vb": lang = CodeLanguage.VB; break;
                default: throw new NotSupportedException("Unknown programming language: " + ext);
            }
            
            string code = File.ReadAllText(codeFile);

            return CreateProjectFromCode(code, lang, name);
        }

        /// <summary>
        /// Creates a project that can be used to build the specified code
        /// </summary>
        /// <param name="code">Code as string</param>
        /// <param name="lang">Programming language of the passed code</param>
        /// <param name="name">Name of the project (any valid file name)</param>
        /// <returns>Path to generated project</returns>
        public static string CreateProjectFromCode(string code,CodeLanguage lang,string name)
        {
            string proj_ext;
            string proj_templ;
            string ext;

            switch (lang)
            {
                case CodeLanguage.CSharp: 
                    proj_ext = ".csproj";
                    ext = ".cs";
                    proj_templ = "CilView.Build.CsTemplate.xml";
                    break;
                case CodeLanguage.VB: 
                    proj_ext = ".vbproj";
                    ext = ".vb";
                    proj_templ = "CilView.Build.VbTemplate.xml";
                    break;
                default: 
                    throw new NotSupportedException("Unknown programming language: " + lang.ToString());
            }
            
            string dir = BuildSystemInvocation.CreateTempDir(name, "proj");
            BuildSystemInvocation.RegisterTempDir(dir);

            //load project template from resources
            string template;
            Assembly ass = typeof(ProjectGenerator).Assembly;

            using (Stream stream = ass.GetManifestResourceStream(proj_templ))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = reader.ReadToEnd();
            }

            //write project file
            string proj_path = Path.Combine(dir, name + proj_ext);
            File.WriteAllText(proj_path, template);

            //write code file
            string out_path = Path.Combine(dir, "Class1"+ext);
            File.WriteAllText(out_path, code);

            return proj_path;
        }
    }
}
