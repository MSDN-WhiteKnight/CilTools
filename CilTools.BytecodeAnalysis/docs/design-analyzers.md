## Feature design: Binary analyzers

*Binary analyzer* is a user-defined component that scans the contents of assemblies and reports some information about their bytecode or metadata. For example, analyzer could return diagnostics for invalid code or calculate aggregated statistic of used APIs. Analyzers inherit from a common base class `AnalyzerBase` in `CilTools.BytecodeAnalysis` assembly.

The `AnalyzerBase` class provides virtual methods that inheritors override (*analyzer actions*):

- OnStart(Assembly) 
- OnType(Type)
- OnMethod(MethodBase,CilGraph)
- OnInstruction(CilInstruction,CilGraphNode)
- OnSyntaxNode(SyntaxNode)
- OnEnd(Assembly)
 
The analyzer runner creates one instance of the analyzer class per target assembly. `OnStart` method is invoked when assembly analysis is started, `OnEnd` - when assembly analysis is finished. This enables analyzer to track state to calculate aggregated statistics over assembly. The analyzer runner parses the target assembly and invokes actions on the corresponding elements. All actions are invoked on the same thread.

Analyzer actions return `ActionResult` class that holds the collection of `ActionResultItem` instances and the boolean flag to stop the analysis prematurely. `ActionResultItem` contains the item kind (error, warning, info), code, message, user-defined numeric value (that could, for example, represent the amount of times diagnostic is found within the analyzed target, if this information is aggregated).

The runner prints the information from action results into its text output, and also returns the collection of `AnalyzerResult` instances. Each `AnalyzerResult` contains the `ActionResultItem` and the assembly item (type, method, instruction, ...) from which it's originated.

Analyzers are grouped into the assembly called *analyzer package*. The runner scans the package assembly using reflection, picks all public classes extending `AnalyzerBase` and invokes them on the target assembly.
