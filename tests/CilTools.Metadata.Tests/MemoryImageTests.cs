/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class MemoryImageTests
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
        static extern bool ReadProcessMemory_Byte(
            IntPtr hProcess, IntPtr lpBaseAddress, out byte lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead
            );

        [TestMethod]
        public void Test_MemoryImage_Load()
        {
            //find corelib module
            string path = typeof(object).Assembly.Location;
            string assname = typeof(object).Assembly.GetName().Name;
            string name = assname + ".dll";

            IntPtr hProcess = Process.GetCurrentProcess().Handle;
            Assert.AreNotEqual(IntPtr.Zero, hProcess);

            IntPtr hModule = LoadLibraryEx(path, IntPtr.Zero, 0);
            if (hModule == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());

            MODULEINFO mi = new MODULEINFO();
            bool res = GetModuleInformation(hProcess, hModule, out mi, (uint)Marshal.SizeOf(mi));
            if (res == false) throw new Win32Exception(Marshal.GetLastWin32Error());

            //get corelib image as byte array
            byte[] data = new byte[mi.SizeOfImage];

            IntPtr c;
            int count_errors = 0;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = 0;
                res = ReadProcessMemory_Byte(hProcess, mi.lpBaseOfDll + i, out b, 1, out c);

                if (res == false || c == IntPtr.Zero) count_errors++;
                else data[i] = b;
            }

            Assert.IsTrue(count_errors < data.Length);

            //load memory image
            MemoryImage img = new MemoryImage(data, path,false);

            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadImage(img);
                Assert.IsNotNull(ass);
                Assert.AreEqual(assname, ass.GetName().Name);
                Type t = ass.GetType("System.Object");
                Assert.IsNotNull(t);
                Assert.AreEqual("System.Object", t.FullName);
            }
        }

        [TestMethod]
        public void Test_MemoryImage_FileLayout()
        {
            //load PE file contents to memory
            string path = typeof(SampleMethods).Assembly.Location;
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            MemoryStream ms = new MemoryStream();

            using (fs) 
            {
                fs.CopyTo(ms);
            }

            byte[] imagedata=ms.GetBuffer();

            //load memory image
            MemoryImage img = new MemoryImage(imagedata, path, true);
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadImage(img);
                Assert.IsNotNull(ass);
                Assert.AreEqual("CilTools.Tests.Common", ass.GetName().Name);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                Assert.IsNotNull(t);
                Assert.AreEqual("CilTools.Tests.Common.SampleMethods", t.FullName);
            }
        }
    }
}
