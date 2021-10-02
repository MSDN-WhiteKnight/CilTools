/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Metadata;

namespace CilTools.BytecodeAnalysis.Tests
{
    /// <summary>
    /// Represents APIs that could be used to fetch method's bytecode from .NET assembly
    /// </summary>
    [Flags]
    public enum BytecodeProviders
    {
        /// <summary>
        /// Standard reflection
        /// </summary>
        Reflection = 0x01,

        /// <summary>
        /// CilTools.Metadata library
        /// </summary>
        Metadata = 0x02,

        /// <summary>
        /// All supported providers
        /// </summary>
        All = Reflection|Metadata
    }

    /// <summary>
    /// Represents data for the data-oriented test that takes a method as its argument
    /// </summary>
    public class MethodTestDataAttribute : Attribute, ITestDataSource
    {
        Type _type;
        string _method;
        BytecodeProviders _prov;

        static AssemblyReader s_reader;
        static readonly object s_sync=new object();

        static MethodTestDataAttribute()
        {
            s_reader = new AssemblyReader();
        }

        /// <summary>
        /// Initializes a new method test data
        /// </summary>
        /// <param name="type">Type in which test data method is housed</param>
        /// <param name="method">Name of the test data method</param>
        /// <param name="src">
        /// Bitmask that specifies bytecode providers used to fetch method's bytecode
        /// </param>
        public MethodTestDataAttribute(Type type, string method, BytecodeProviders prov)
        {
            this._type = type;
            this._method = method;
            this._prov = prov;
        }

        public BytecodeProviders Providers { get { return this._prov; } }
        public Type Type { get { return this._type; } }
        public string Method { get { return this._method; } }

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            MethodBase m;

            if (this._prov.HasFlag(BytecodeProviders.Reflection))
            {
                m = this._type.GetMethod(this._method);
                yield return new object[] { m };
            }

            if (this._prov.HasFlag(BytecodeProviders.Metadata))
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
            if (data != null && data.Length>=1 && data[0]!=null)
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
