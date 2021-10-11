/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void Test_PInvoke()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                CustomMethod mi = t.GetMember("ShowWindow")[0] as CustomMethod;

                Assert.IsTrue(
                    mi.Attributes.HasFlag(MethodAttributes.PinvokeImpl),
                    "ShowWindow should have PinvokeImpl attribute"
                    );

                PInvokeParams pars = mi.GetPInvokeParams();

                Assert.AreEqual("user32.dll", pars.ModuleName);
                Assert.IsTrue(pars.SetLastError, "SetLastError should be true for ShowWindow");
            }
        }
        
        [TestMethod]
        public void Test_BaseTypes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(object).Assembly.Location);
                Type tException = ass.GetType("System.Exception");
                Type tString = ass.GetType("System.String");
                Type tObject = ass.GetType("System.Object");
                Type t = ass.GetType("System.TypeLoadException");

                Assert.IsTrue(tException.IsAssignableFrom(tException));

                //TypeLoadException is derived from System.Exception
                Assert.IsTrue(tException.IsAssignableFrom(t));

                //reverse is not true
                Assert.IsFalse(t.IsAssignableFrom(tException));

                //String is not derived from Exception
                Assert.IsFalse(tException.IsAssignableFrom(tString));

                //every reference type is derived from object
                Assert.IsTrue(tObject.IsAssignableFrom(t));
                Assert.IsTrue(tObject.IsAssignableFrom(tException));
                Assert.IsTrue(tObject.IsAssignableFrom(tString));

                //check that we are compatible with standard reflection
                Assert.IsTrue(tException.IsAssignableFrom(typeof(Exception)));
                Assert.IsTrue(tException.IsAssignableFrom(typeof(TypeLoadException)));
                Assert.IsFalse(tException.IsAssignableFrom(typeof(string)));
            }
        }

        [TestMethod]
        public void Test_ExternalType()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("GetInterfaceCount")[0] as MethodBase;
                Type paramtype = mi.GetParameters()[0].ParameterType;

                Assert.IsTrue(paramtype.Attributes.HasFlag(TypeAttributes.Public));
                Assert.AreEqual("MemberInfo", paramtype.BaseType.Name);

                Assert.IsTrue(paramtype.IsAssignableFrom(paramtype));
                Assert.IsTrue(paramtype.IsAssignableFrom(typeof(Type)));
            }
        }

        [TestMethod]
        public void Test_TypeAttributes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.TestType");

                //reflection
                object[] attrs = t.GetCustomAttributes(false);

                AssertThat.HasOnlyOneMatch(
                    attrs,
                    (x) =>
                    {
                        return x is ICustomAttribute &&
                        String.Equals(
                            ((ICustomAttribute)x).Constructor.DeclaringType.Name,
                            "MyAttribute",
                            StringComparison.InvariantCulture);
                    }
                    );

                //syntax
                IEnumerable<SyntaxNode> syntax = SyntaxNode.GetTypeDefSyntax(t);
                AssertThat.IsSyntaxTreeCorrect(syntax);

                StringBuilder sb = new StringBuilder(1000);
                foreach (SyntaxNode node in syntax) sb.Append(node.ToString());
                string str = sb.ToString();

                AssertThat.IsMatch(str, new MatchElement[] {
                ".class", MatchElement.Any,
                ".custom", MatchElement.Any,
                "instance", MatchElement.Any,
                "void", MatchElement.Any,
                "CilTools.Tests.Common.MyAttribute", MatchElement.Any,
                ".ctor", MatchElement.Any,
                "(", MatchElement.Any,
                "int32", MatchElement.Any,
                ")", MatchElement.Any,
                "=", MatchElement.Any,
                "(", MatchElement.Any,
                "01 00 30 02 00 00 00 00", MatchElement.Any,
                ")", MatchElement.Any,
                });
            }//end using
        }

        [TestMethod]
        public void Test_NavigationGenericMethod()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("GenericsTest")[0] as MethodBase;
                ReflectionTestsCore.Test_NavigationGenericMethod(mi);
            }
        }

        [TestMethod]
        public void Test_MethodDef_ObjectDisposedException()
        {
            AssemblyReader reader = new AssemblyReader();
            CustomMethod m = null;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                m = t.GetMember("PrintHelloWorld")[0] as CustomMethod;
            }

            //test access after AssemblyReader is disposed
            AssertThat.Throws<ObjectDisposedException>(() => m.GetBytecode());
            AssertThat.Throws<ObjectDisposedException>(() => { var x = m.Attributes; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = m.MethodImplementationFlags; });
        }

        [TestMethod]
        public void Test_TypeDef_ObjectDisposedException()
        {
            AssemblyReader reader = new AssemblyReader();
            Type t = null;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            }

            //test access after AssemblyReader is disposed
            AssertThat.Throws<ObjectDisposedException>(() => { var x = t.Namespace; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = t.Name; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = t.FullName; });
            AssertThat.DoesNotThrow(() => t.ToString());
            AssertThat.DoesNotThrow(() => t.GetHashCode());
        }

        [TestMethod]
        public void Test_FunctionPointers()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                string path = Path.Combine(
                    System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
                    "WPF\\PresentationCore.dll"
                    );

                Assembly ass = reader.LoadFrom(path);
                Type mt = ass.GetType("<Module>");
                MethodBase mi = mt.GetMember(
                    "_atexit_helper",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                    )[0] as MethodBase;

                /*.method  assembly static int32 _atexit_helper(
                method void *() func, 
                uint32* __pexit_list_size, 
                method void *()** __ponexitend_e, 
                method void *()** __ponexitbegin_e
                ) cil managed*/

                ParameterInfo[] pars = mi.GetParameters();
                Type t = pars[0].ParameterType;

                Assert.IsTrue(t is ITypeInfo);
                ITypeInfo ti = (ITypeInfo)t;

                Assert.IsTrue(ti.IsFunctionPointer);
                Assert.AreEqual(CallingConvention.Default, ti.TargetSignature.CallingConvention);
                Assert.AreEqual("System.Void", ti.TargetSignature.ReturnType.FullName);
                Assert.AreEqual(0, ti.TargetSignature.ParamsCount);

                string sig_string = ti.TargetSignature.ToString();

                AssertThat.IsMatch(
                    sig_string, 
                    new MatchElement[] {
                        "void",MatchElement.Any, "(",
                        MatchElement.Any,")"
                    }
                );

                /*.method  assembly static void* bsearch(
                void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* key, 
                void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* base, 
                uint32 num, 
                uint32 width, 
                method int32 *( void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*, 
                                void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*
                              ) compare
                ) cil managed*/

                mi = mt.GetMember(
                    "bsearch",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                    )[0] as MethodBase;

                pars = mi.GetParameters();
                t = pars[4].ParameterType;

                Assert.IsTrue(t is ITypeInfo);
                ti = (ITypeInfo)t;

                Assert.IsTrue(ti.IsFunctionPointer);
                Assert.AreEqual(CallingConvention.Default, ti.TargetSignature.CallingConvention);
                Assert.AreEqual("System.Int32", ti.TargetSignature.ReturnType.FullName);
                Assert.AreEqual(2, ti.TargetSignature.ParamsCount);

                TypeSpec ts = ti.TargetSignature.GetParamType(0);
                Assert.IsTrue(ts.IsPointer);
                Assert.AreEqual("System.Void", ts.GetElementType().FullName);
                Assert.IsFalse(ts.InnerTypeSpec.GetModifier(0).IsRequired);
                Assert.AreEqual("IsConst", ts.InnerTypeSpec.GetModifier(0).ModifierType.Name);

                sig_string = ti.TargetSignature.ToString();
                
                AssertThat.IsMatch(
                    sig_string,
                    new MatchElement[] {
                        "int32",MatchElement.Any, 
                        "(",MatchElement.Any,
                        "void",MatchElement.Any,
                        "modopt",MatchElement.Any,"(",MatchElement.Any,
                        "System.Runtime.CompilerServices.IsConst",MatchElement.Any,
                        ")",MatchElement.Any,"*",MatchElement.Any,
                        ",",MatchElement.Any,
                        "void",MatchElement.Any,
                        "modopt",MatchElement.Any,"(",MatchElement.Any,
                        "System.Runtime.CompilerServices.IsConst",MatchElement.Any,
                        ")",MatchElement.Any,"*",MatchElement.Any,                        
                        ")"
                    }
                );
            }
        }

        [TestMethod]
        public void Test_AssemblyCustomAttributes()
        {
            // Verify fetching assembly-level custom attributes using CilTools.Metadata.
            AssemblyReader reader = new AssemblyReader();
            
            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                object[] attrs = ass.GetCustomAttributes(false);
                Assert.IsTrue(attrs.Length > 0);
                bool found = false;

                for (int i = 0; i < attrs.Length; i++) 
                {
                    Assert.IsTrue(attrs[i] is ICustomAttribute);
                    ICustomAttribute ica = (ICustomAttribute)attrs[i];
                    Assert.IsNotNull(ica.Constructor);
                    Assert.IsNotNull(ica.Data);

                    if (ica.Constructor.DeclaringType.Name.Equals(
                        "AssemblyCompanyAttribute", StringComparison.InvariantCulture))
                    {
                        CollectionAssert.AreEqual(
                            new byte[] {
                                0x01,0x00,0x09,0x43,0x49,0x4C,0x20,0x54,0x6F,0x6F,0x6C,0x73,0x00,0x00
                            },
                            ica.Data);
                        //\u0001\0\tCIL Tools\0\0
                        
                        found = true;
                    }
                }//end for

                if (!found)
                {
                    Assert.Fail(
                        "AssemblyCompanyAttribute is not found on assembly " + ass.FullName
                        );
                }
            }
        }
    }
}
