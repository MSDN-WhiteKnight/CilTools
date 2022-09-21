using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MetadataAssemblyGenerator
{
    public class AssemblyEmitter
    {
        private static readonly Guid s_guid = Guid.NewGuid();
        private static readonly BlobContentId s_contentId = new BlobContentId(s_guid, 0x04030201);

        string name;
        Func<MetadataBuilder, AssemblyReferenceHandle, InstructionEncoder> callbackEmitMethodBody;
        MetadataBuilder builder = new MetadataBuilder();
        BlobBuilder ilBuilder = new BlobBuilder();

        public AssemblyEmitter(string assName, 
            Func<MetadataBuilder, AssemblyReferenceHandle, InstructionEncoder> emitter)
        {
            this.name = assName;
            this.callbackEmitMethodBody = emitter;
            this.Initialize();
        }

        string Name { get { return this.name; } }

        void Initialize()
        {
            MethodDefinitionHandle mdh = EmitMethod(this.Name, this.builder, ilBuilder,
                "TestMethod", ProduceMethodSignature);
        }

        public static AssemblyReferenceHandle AddAssemblyReference(MetadataBuilder metadata, string name,
            Version ver)
        {
            return metadata.AddAssemblyReference(
                name: metadata.GetOrAddString(name),
                version: ver,
                culture: default(StringHandle),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));
        }

        MethodDefinitionHandle EmitMethod(string assemblyName,
            MetadataBuilder metadata,
            BlobBuilder ilBuilder,
            string name,
            Func<MetadataBuilder, BlobBuilder> signatureCallback)
        {
            BlobBuilder methodSignature = signatureCallback(metadata);

            // Create module and assembly
            metadata.AddModule(
                0,
                metadata.GetOrAddString(assemblyName + ".dll"),
                metadata.GetOrAddGuid(s_guid),
                default(GuidHandle),
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString(assemblyName),
                version: new Version(1, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: 0,
                hashAlgorithm: AssemblyHashAlgorithm.None);

            // Create references to System.Object and System.Console types.
            AssemblyReferenceHandle corlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("System.Runtime"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            TypeReferenceHandle systemObjectTypeRef = metadata.AddTypeReference(
                corlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            // Get reference to Object's constructor.
            var parameterlessCtorSignature = new BlobBuilder();

            new BlobEncoder(parameterlessCtorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(0, returnType => returnType.Void(), parameters => { });

            BlobHandle parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

            MemberReferenceHandle objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex);

            var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);

            var codeBuilder = new BlobBuilder();
            InstructionEncoder il;

            // Emit IL for Program::.ctor
            il = new InstructionEncoder(codeBuilder);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int ctorBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            // Emit IL for a method
            il = this.callbackEmitMethodBody(metadata, corlibAssemblyRef);
            
            int methodBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();
            
            // Create method definition
            MethodDefinitionHandle methodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL,
                metadata.GetOrAddString(name),
                metadata.GetOrAddBlob(methodSignature),
                methodBodyOffset,
                parameterList: MetadataTokens.ParameterHandle(1));

            // Create method definition for Program::.ctor
            MethodDefinitionHandle ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                parameterList: MetadataTokens.ParameterHandle(1));

            // Create type definition for the special <Module> type that holds global functions
            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            // Create type definition for ConsoleApplication.Program
            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString(assemblyName),
                metadata.GetOrAddString("Program"),
                baseType: systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: methodDef);

            return methodDef;
        }

        private static void WritePEImage(
            Stream peStream,
            MetadataBuilder metadataBuilder,
            BlobBuilder ilBuilder,
            MethodDefinitionHandle entryPointHandle)
        {
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll);

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                entryPoint: entryPointHandle,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => s_contentId);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            BlobContentId contentId = peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        static BlobBuilder ProduceMethodSignature(MetadataBuilder metadataBuilder)
        {
            //void Method()
            var methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature().
                Parameters(0, returnType => returnType.Void(), parameters => { });

            return methodSignature;
        }

        public Assembly BuildAssemblyFile()
        {
            using (var peStream = new FileStream(this.Name + ".dll", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                WritePEImage(peStream, this.builder, ilBuilder, default(MethodDefinitionHandle));
            }
            
            Assembly ass = Assembly.LoadFrom(this.Name + ".dll");
            return ass;
        }

        public Assembly BuildAssemblyInMemory()
        {
            using (var peStream = new MemoryStream())
            {
                WritePEImage(peStream, this.builder, ilBuilder, default(MethodDefinitionHandle));
                Assembly ass = Assembly.Load(peStream.ToArray());
                return ass;
            }
        }

        public object Execute(Assembly ass)
        {
            Type t = ass.GetType(this.Name + ".Program");
            MethodInfo mi = t.GetMethod("TestMethod");
            object ret = mi.Invoke(null, new object[] { });
            return ret;
        }
    }
    
    // ****
    
    class Program
    {
        static InstructionEncoder EmitMethodBody(MetadataBuilder metadata, AssemblyReferenceHandle corlibAssemblyRef)
        {
            var codeBuilder = new BlobBuilder();
            var encoder = new InstructionEncoder(codeBuilder, new ControlFlowBuilder());
            
            AssemblyReferenceHandle consoleAssemblyRef = AssemblyEmitter.AddAssemblyReference(
                metadata, 
                "System.Console", 
                new Version(4, 0, 0, 0));

            TypeReferenceHandle systemConsoleTypeRefHandle = metadata.AddTypeReference(
                consoleAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            var consoleWriteLineSignature = new BlobBuilder();

            new BlobEncoder(consoleWriteLineSignature).
                MethodSignature().
                Parameters(1,
                    returnType => returnType.Void(),
                    parameters => parameters.AddParameter().Type().String());

            MemberReferenceHandle consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                metadata.GetOrAddBlob(consoleWriteLineSignature));

            // ldstr "hello"
            encoder.LoadString(metadata.GetOrAddUserString("hello"));

            // call void [mscorlib]System.Console::WriteLine(string)
            encoder.Call(consoleWriteLineMemberRef);

            // ret
            encoder.OpCode(ILOpCode.Ret);

            return encoder;
        }

        static void Main(string[] args)
        {
            AssemblyEmitter emitter = new AssemblyEmitter("MyAssembly", EmitMethodBody);
            Assembly ass = emitter.BuildAssemblyInMemory();
            object obj = emitter.Execute(ass);
            Console.WriteLine("End");
            Console.Read();
        }
    }    
}
