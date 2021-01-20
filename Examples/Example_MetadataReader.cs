// CilTools.BytecodeAnalysis example: Reading methods' bytecode from PE file loaded via System.Reflection.Metadata.MetadataReader 

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using CilTools.BytecodeAnalysis;

//Required NuGet package: System.Reflection.Metadata

namespace ConsoleApplication1
{
    class Program
    {
        public static Dictionary<int, string> GetTokenTable(MetadataReader mr)
        {
            //load tokens for methods used by this assembly
            var methods = new Dictionary<int, string>();

            //own methods
            foreach (TypeDefinitionHandle t in mr.TypeDefinitions)
            {
                TypeDefinition tdef = mr.GetTypeDefinition(t);

                string name = mr.GetString(tdef.Name).ToString();

                foreach (MethodDefinitionHandle mdefh in tdef.GetMethods())
                {
                    MethodDefinition mdef = mr.GetMethodDefinition(mdefh);

                    string mname = mr.GetString(mdef.Name);
                    methods[mr.GetToken(mdefh)] = name + "." + mname;
                }
            }

            //external methods
            foreach (MemberReferenceHandle mrh in mr.MemberReferences)
            {
                MemberReference mref = mr.GetMemberReference(mrh);
                if (mref.GetKind() != MemberReferenceKind.Method) continue;
                string name = mr.GetString(mref.Name).ToString();
                int tok = mr.GetToken(mrh);

                string ns = "";
                string tname = "";
                string assname = "";
                AssemblyReferenceHandle ash = default(AssemblyReferenceHandle);

                if (mref.Parent.Kind == HandleKind.TypeReference)
                {
                    TypeReference tref = mr.GetTypeReference((TypeReferenceHandle)mref.Parent);
                    ns = mr.GetString(tref.Namespace).ToString();
                    tname = mr.GetString(tref.Name).ToString();

                    if (tref.ResolutionScope.Kind == HandleKind.AssemblyReference)
                    {
                        ash = (AssemblyReferenceHandle)tref.ResolutionScope;
                    }
                }

                if (ash != default(AssemblyReferenceHandle))
                {
                    AssemblyReference assref = mr.GetAssemblyReference(ash);
                    assname = mr.GetString(assref.Name).ToString();
                }

                methods[tok] = String.Format("[{0}]{1}.{2}.{3}", assname, ns, tname, name);                                
            }

            return methods;
        }

        public static void DumpMethods(string path)
        {
            //print CIL bytecode for all methods in the assembly

            FileStream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            using (s)
            {
                PEReader reader = new PEReader(s);

                using (reader)
                {
                    MetadataReader mr = reader.GetMetadataReader(MetadataReaderOptions.None);
                    Dictionary<int, string> methods = GetTokenTable(mr);

                    foreach (TypeDefinitionHandle t in mr.TypeDefinitions)
                    {
                        TypeDefinition tdef = mr.GetTypeDefinition(t);

                        string name = mr.GetString(tdef.Name).ToString();
                        Console.WriteLine(" *** Type: " + name + " ***");

                        MethodDefinitionHandleCollection mcoll = tdef.GetMethods();

                        if (mcoll.Count == 0) Console.WriteLine("(No methods)");

                        foreach (MethodDefinitionHandle mdefh in mcoll)
                        {
                            MethodDefinition mdef = mr.GetMethodDefinition(mdefh);

                            string mname = mr.GetString(mdef.Name);
                            Console.WriteLine(" Method: " + mname);

                            int rva = mdef.RelativeVirtualAddress;

                            if (rva == 0)
                            {
                                Console.WriteLine("Unable to find method body");
                                Console.WriteLine();
                                continue;
                            }

                            MethodBodyBlock mb = reader.GetMethodBody(rva);
                            byte[] il = mb.GetILBytes(); //load CIL as byte array

                            //read method body from byte array

                            CilInstruction[] instructions = CilReader.GetInstructions(il).ToArray();

                            for (int i = 0; i < instructions.Length; i++)
                            {
                                if (instructions[i].OpCode.FlowControl == FlowControl.Call)
                                {
                                    //attempt to resolve method tokens for call isntructions
                                    Console.Write(instructions[i].OpCode.Name.PadRight(10, ' ') + " ");

                                    int token = (int)instructions[i].Operand;
                                    if (methods.ContainsKey(token))
                                    {
                                        Console.WriteLine(methods[token]);
                                    }
                                    else
                                    {
                                        Console.WriteLine("0x" + token.ToString("X"));
                                    }
                                }
                                else Console.WriteLine(instructions[i].ToString());
                            }
                            Console.WriteLine();
                        }

                        Console.WriteLine();
                    }
                }//end using
            }//end using
        }

        static int Main(string[] args)
        {            
            if (args.Length == 0)
            {
                Console.WriteLine("Pass the assembly path in argument");
                return 1;
            }

            DumpMethods(args[0]);

            Console.ReadKey();
            return 0;
        }
    }
}

/* Example output:

*** Type: <Module> ***
(No methods)

 *** Type: Program ***
 Method: CalcSum
nop
ldarg.0
ldarg.1
add
stloc.0
br.s       0
ldloc.0
ret

 Method: Main
nop
ldc.i4.1
ldc.i4.2
call       Program.CalcSum
stloc.0
ldloca.s   V_0
call       [mscorlib]System.Int32.ToString
call       [mscorlib]System.Console.WriteLine
nop
ret

 Method: .ctor
ldarg.0
call       [mscorlib]System.Object..ctor
nop
ret
*/
