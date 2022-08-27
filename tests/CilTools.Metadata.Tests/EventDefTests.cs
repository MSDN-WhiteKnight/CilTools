/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class EventDefTests
    {
        [TestMethod]
        public void Test_EventDef_Instance()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
            Type t = ass.GetType(typeof(EventsSample).FullName);
            EventInfo ei = t.GetEvent("A");
            MethodInfo miAdd = ei.GetAddMethod();
            MethodInfo miRemove = ei.GetRemoveMethod();

            Assert.AreEqual(MemberTypes.Event, ei.MemberType);
            Assert.AreEqual(EventAttributes.None, ei.Attributes);
            Assert.AreEqual("A", ei.Name);            
            Assert.AreSame(t, ei.DeclaringType);
            Assert.AreEqual(typeof(EventsSample).FullName, ei.DeclaringType.FullName);
            Assert.AreEqual(0, ei.GetCustomAttributes(false).Length);
            
            Assert.IsFalse(miAdd.IsStatic);
            Assert.IsTrue(miAdd.IsPublic);
            Assert.AreEqual("System.Action", miAdd.GetParameters()[0].ParameterType.FullName);
            Assert.AreEqual("System.Void", miAdd.ReturnType.FullName);

            Assert.IsFalse(miRemove.IsStatic);
            Assert.IsTrue(miRemove.IsPublic);
            Assert.AreEqual("System.Action", miRemove.GetParameters()[0].ParameterType.FullName);
            Assert.AreEqual("System.Void", miRemove.ReturnType.FullName);
        }

        [TestMethod]
        public void Test_EventDef_Static()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
            Type t = ass.GetType(typeof(EventsSample).FullName);
            EventInfo ei = t.GetEvent("B");
            MethodInfo miAdd = ei.GetAddMethod();
            MethodInfo miRemove = ei.GetRemoveMethod();

            Assert.AreEqual(MemberTypes.Event, ei.MemberType);
            Assert.AreEqual(EventAttributes.None, ei.Attributes);
            Assert.AreEqual("B", ei.Name);
            Assert.AreSame(t, ei.DeclaringType);
            Assert.AreEqual(typeof(EventsSample).FullName, ei.DeclaringType.FullName);
            
            Assert.IsTrue(miAdd.IsStatic);
            Assert.IsTrue(miAdd.IsPublic);
            Assert.AreEqual("System.EventHandler`1", miAdd.GetParameters()[0].ParameterType.FullName);
            Assert.AreEqual("System.Void", miAdd.ReturnType.FullName);

            Assert.IsTrue(miRemove.IsStatic);
            Assert.IsTrue(miRemove.IsPublic);
            Assert.AreEqual("System.EventHandler`1", miAdd.GetParameters()[0].ParameterType.FullName);
            Assert.AreEqual("System.Void", miRemove.ReturnType.FullName);
        }

        [TestMethod]
        public void Test_EventDef_NonAuto()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
            Type t = ass.GetType(typeof(EventsSample).FullName);
            EventInfo ei = t.GetEvent("C", Utils.AllMembers());
            MethodInfo miAdd = ei.GetAddMethod(true);
            MethodInfo miRemove = ei.GetRemoveMethod(true);

            Assert.AreEqual(MemberTypes.Event, ei.MemberType);
            Assert.AreEqual(EventAttributes.None, ei.Attributes);
            Assert.AreEqual("C", ei.Name);
            Assert.AreSame(t, ei.DeclaringType);
            Assert.AreEqual(typeof(EventsSample).FullName, ei.DeclaringType.FullName);
            Assert.AreEqual(0, ei.GetCustomAttributes(false).Length);

            Assert.IsFalse(miAdd.IsStatic);
            Assert.IsFalse(miAdd.IsPublic);
            Assert.AreEqual("System.Action`1", miAdd.GetParameters()[0].ParameterType.FullName);
            Assert.AreEqual("System.Void", miAdd.ReturnType.FullName);

            Assert.IsFalse(miRemove.IsStatic);
            Assert.IsFalse(miRemove.IsPublic);
            Assert.AreEqual("System.Action`1", miRemove.GetParameters()[0].ParameterType.FullName);
            Assert.AreEqual("System.Void", miRemove.ReturnType.FullName);
        }

        [TestMethod]
        public void Test_EventDef_CustomAttributes()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
            Type t = ass.GetType(typeof(EventsSample).FullName);
            EventInfo ei = t.GetEvent("B");

            object[] attrs = ei.GetCustomAttributes(false);
            Assert.AreEqual(1, attrs.Length);
            Assert.IsTrue(attrs[0] is ICustomAttribute);
            ICustomAttribute attr = (ICustomAttribute)attrs[0];
            Assert.AreEqual("MyAttribute", attr.Constructor.DeclaringType.Name);
        }

        [TestMethod]
        public void Test_EventDef_ObjectDisposedException()
        {
            AssemblyReader reader = new AssemblyReader();
            EventInfo ei;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
                Type t = ass.GetType(typeof(EventsSample).FullName);
                ei = t.GetEvent("A");
            }

            AssertThat.Throws<ObjectDisposedException>(() => { var x = ei.Name; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = ei.GetAddMethod(); });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = ei.GetRaiseMethod(); });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = ei.GetRemoveMethod(); });
        }

        [TestMethod]
        public void Test_EventDef_GetAddMethod()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
            Type t = ass.GetType(typeof(EventsSample).FullName);
            EventInfo ei = t.GetEvent("C", Utils.AllMembers());
            MethodInfo miAdd;

            miAdd = ei.GetAddMethod(false);
            Assert.IsNull(miAdd);

            miAdd = ei.GetAddMethod(true);
            Assert.IsNotNull(miAdd);
            Assert.AreEqual("add_C", miAdd.Name);
            Assert.AreSame(t, miAdd.DeclaringType);
        }

        [TestMethod]
        public void Test_EventDef_GetRemoveMethod()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(EventsSample).Assembly.Location);
            Type t = ass.GetType(typeof(EventsSample).FullName);
            EventInfo ei = t.GetEvent("C", Utils.AllMembers());
            MethodInfo miRemove;

            miRemove = ei.GetRemoveMethod(false);
            Assert.IsNull(miRemove);

            miRemove = ei.GetRemoveMethod(true);
            Assert.IsNotNull(miRemove);
            Assert.AreEqual("remove_C", miRemove.Name);
            Assert.AreSame(t, miRemove.DeclaringType);
        }
    }
}
