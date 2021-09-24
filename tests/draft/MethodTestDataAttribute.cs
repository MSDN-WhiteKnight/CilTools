using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        [MethodTestData(typeof(object),"GetHashCode", MethodSource.FromReflection)]
        public void TestMethod1(MethodBase m)
        {
            Assert.AreEqual("GetHashCode", m.Name);
        }
    }

    [Flags]
    public enum MethodSource
    {
        FromReflection = 0x01,
        FromMetadata = 0x02
    }

    public class MethodTestDataAttribute:Attribute, ITestDataSource
    {
        Type _type;
        string _method;
        MethodSource _src;

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
            MethodBase m=this._type.GetMethod(this._method);
            yield return new object[] {m};
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null)
            {
                return string.Format("{0} ({1})", methodInfo.Name, string.Join(",", data));
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
