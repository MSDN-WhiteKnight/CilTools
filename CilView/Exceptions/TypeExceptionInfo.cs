/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilView.Exceptions
{
    public class TypeExceptionInfo
    {
        string typename;
        Dictionary<string, string[]> exceptions;

        public TypeExceptionInfo(string tn, Dictionary<string, string[]> exc)
        {
            this.typename = tn;
            this.exceptions = exc;
        }

        public string TypeName { get { return this.typename; } }

        public string[] GetExceptions(string method)
        {
            string[] arr = exceptions[method];
            if (arr == null) return null;

            string[] ret = new string[arr.Length];
            Array.Copy(arr, ret, arr.Length);
            return ret;
        }

        public IEnumerable<string> Methods
        {
            get
            {
                foreach (string key in exceptions.Keys) yield return key;
            }
        }

        public static TypeExceptionInfo GetFromXML(string file)
        {
            Dictionary<string, string[]> ret = new Dictionary<string, string[]>();

            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlNode members = doc.FirstChild["Members"];
            string typename = doc.FirstChild.Attributes["FullName"].Value;

            foreach (XmlNode node in members.ChildNodes)
            {
                if (!(node.NodeType == XmlNodeType.Element)) continue;
                if (!(node.Name == "Member")) continue;
                if (node["MemberType"].InnerText != "Method") continue;

                string mname = node.Attributes["MemberName"].Value;

                //signature
                string docid = "";
                foreach (XmlNode x in node.ChildNodes)
                {
                    if (x.Name == "MemberSignature" && x.Attributes["Language"].Value == "DocId")
                    {
                        docid = x.Attributes["Value"].Value;
                        break;
                    }
                }

                if (String.IsNullOrEmpty(docid)) docid = mname;

                //exclude explicit interface implementations
                if (docid.Contains("#")) continue;

                //exceptions
                XmlNode docs = node["Docs"];
                if (docs == null) continue;
                List<string> exceptions = new List<string>(10);

                foreach (XmlNode x in docs.ChildNodes)
                {
                    if (x.Name == "exception")
                    {
                        exceptions.Add(x.Attributes["cref"].Value);
                    }
                }

                ret[docid] = exceptions.ToArray();
            }//end foreach

            return new TypeExceptionInfo(typename,ret);
        }

        public static TypeExceptionInfo GetFromType(Type t)
        {
            Dictionary<string, string[]> ret = new Dictionary<string, string[]>();

            MemberInfo[] members = t.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                );

            for (int i = 0; i < members.Length; i++)
            {
                MethodBase mb = members[i] as MethodBase;
                if (mb == null) continue;
                if (mb.IsConstructor) continue;

                //constructor check does not work for CustomMethod
                if(mb is CustomMethod && ((CustomMethod)mb).ReturnType==null) continue;

                if (mb.Name.StartsWith("get_") || mb.Name.StartsWith("set_"))
                {
                    continue; //exclude property access methods
                }

                List<string> list = new List<string>(10);
                IEnumerable<ExceptionInfo> exceptions = ExceptionInfo.GetExceptions(mb);

                //signature
                StringBuilder sb = new StringBuilder(100);
                sb.Append("M:");
                if (mb.DeclaringType != null) sb.Append(mb.DeclaringType.FullName);
                sb.Append('.');
                sb.Append(mb.Name);
                ParameterInfo[] pars = mb.GetParameters();

                if (pars.Length > 0)
                {
                    sb.Append('(');

                    for (int j = 0; j < pars.Length; j++)
                    {
                        if (j >= 1) sb.Append(',');
                        sb.Append(pars[j].ParameterType.FullName);
                    }

                    sb.Append(')');
                }

                string sig = sb.ToString();

                //exceptions
                foreach (ExceptionInfo ei in exceptions)
                {
                    list.Add("T:" + ei.ExceptionType.FullName);
                }

                ret[sig] = list.ToArray();
            }

            return new TypeExceptionInfo(t.FullName,ret);
        }

        public static void CompareMethodExceptions(string[] fromDocs, string[] fromCode, TextWriter output)
        {
            HashSet<string> fromDocsSet = new HashSet<string>(fromDocs,StringComparer.InvariantCulture);
            HashSet<string> fromCodeSet = new HashSet<string>(fromCode, StringComparer.InvariantCulture);
            int c = 0;

            for (int i = 0; i < fromCode.Length; i++)
            {
                if (!fromDocsSet.Contains(fromCode[i]))
                {
                    output.Write(fromCode[i]);
                    output.Write(" thrown by code, but not included in docs");
                    output.WriteLine();
                    c++;
                }
            }

            for (int i = 0; i < fromDocs.Length; i++)
            {
                if (!fromCodeSet.Contains(fromDocs[i]))
                {
                    output.Write(fromDocs[i]);
                    output.Write(" included in docs, but is not thrown in code");
                    output.WriteLine();
                    c++;
                }
            }

            if(c==0) output.WriteLine("(No differences found)");

            output.Flush();
        }

        public static void Compare(TypeExceptionInfo fromDocs, TypeExceptionInfo fromCode, TextWriter output)
        {
            HashSet<string> fromDocsMethods = new HashSet<string>(fromDocs.Methods, StringComparer.InvariantCulture);
            HashSet<string> fromCodeMethods = new HashSet<string>(fromCode.Methods, StringComparer.InvariantCulture);
            HashSet<string> intersect=new HashSet<string>(StringComparer.InvariantCulture);

            foreach (string m in fromDocsMethods)
            {
                if (!fromCodeMethods.Contains(m))
                {
                    output.Write(m);
                    output.Write(" included in docs, but not found in code");
                    output.WriteLine();
                }
                else
                {
                    intersect.Add(m);
                }
            }

            output.WriteLine();

            foreach (string m in fromCodeMethods)
            {
                if (!fromDocsMethods.Contains(m))
                {
                    output.Write(m);
                    output.Write(" present in code, but not found in docs");
                    output.WriteLine();
                }
            }

            output.WriteLine();

            foreach (string m in intersect)
            {
                string[] excFromCode=fromCode.GetExceptions(m);
                string[] excFromDocs=fromDocs.GetExceptions(m);
                output.WriteLine("Method: "+m);
                CompareMethodExceptions(excFromDocs, excFromCode, output);
                output.WriteLine();
            }

            output.Flush();
        }

        public static string Compare(TypeExceptionInfo fromDocs, TypeExceptionInfo fromCode)
        {
            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);
            Compare(fromDocs, fromCode, wr);
            return sb.ToString();
        }
    }
}
