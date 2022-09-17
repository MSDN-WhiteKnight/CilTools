/* CIL Tools tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Metadata;

namespace CilTools.Tests.Common.Attributes
{
    /// <summary>
    /// Represents data for the data-oriented test that takes a type as its argument
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TypeTestDataAttribute : Attribute, ITestDataSource
    {
        Type _type;        
        BytecodeProviders _prov;

        static AssemblyReader s_reader;
        static readonly object s_sync = new object();

        static TypeTestDataAttribute()
        {
            s_reader = new AssemblyReader();
        }

        /// <summary>
        /// Initializes a new type test data
        /// </summary>
        /// <param name="type">Type in which test data type is housed</param>
        /// <param name="src">
        /// Bitmask that specifies bytecode providers used to fetch type
        /// </param>
        public TypeTestDataAttribute(Type type, BytecodeProviders prov)
        {
            this._type = type;
            this._prov = prov;
        }

        public BytecodeProviders Providers { get { return this._prov; } }
        public Type Type { get { return this._type; } }
        
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            if (this._prov.HasFlag(BytecodeProviders.Reflection))
            {
                yield return new object[] { this._type };
            }

            if (this._prov.HasFlag(BytecodeProviders.Metadata))
            {
                lock (s_sync)
                {
                    Assembly ass = s_reader.LoadFrom(this._type.Assembly.Location);
                    Type t = ass.GetType(this._type.FullName);
                    yield return new object[] { t };
                }
            }
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null && data.Length >= 1 && data[0] != null)
            {
                string name = string.Empty;

                try { name = (data[0] as Type).Name; }
                catch (Exception) { }

                StringBuilder sb = new StringBuilder(150);
                sb.Append(methodInfo.Name);
                sb.AppendFormat(" ({0} {1})", data[0].GetType().Name, name);
                return sb.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
