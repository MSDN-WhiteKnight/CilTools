/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Metadata;

namespace CilTools.BytecodeAnalysis.Tests
{
    [Flags]
    public enum MethodSource
    {
        FromReflection = 0x01,
        FromMetadata = 0x02,
        All = FromReflection|FromMetadata
    }

    public class MethodTestDataAttribute : Attribute, ITestDataSource
    {
        Type _type;
        string _method;
        MethodSource _src;

        static AssemblyReader s_reader;
        static readonly object s_sync=new object();

        static MethodTestDataAttribute()
        {
            s_reader = new AssemblyReader();
        }

        public MethodTestDataAttribute(Type type, string method, MethodSource src)
        {
            this._type = type;
            this._method = method;
            this._src = src;
        }

        public MethodSource Source { get { return this._src; } }
        public Type Type { get { return this._type; } }
        public string Method { get { return this._method; } }

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            MethodBase m;

            if (this._src.HasFlag(MethodSource.FromReflection))
            {
                m = this._type.GetMethod(this._method);
                yield return new object[] { m };
            }

            if (this._src.HasFlag(MethodSource.FromMetadata))
            {
                lock (s_sync)
                {
                    Assembly ass = s_reader.LoadFrom(this._type.Assembly.Location);
                    Type t = ass.GetType(this._type.FullName);
                    m = t.GetMember(this._method)[0] as MethodBase;
                    yield return new object[] { m };
                }
            }
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null && data.Length>=1)
            {
                StringBuilder sb = new StringBuilder(150);
                sb.Append(methodInfo.Name);
                sb.AppendFormat(" ({0})",data[0].GetType().ToString());
                return sb.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
