# CilView user manual

**Requirements:** .NET Framework 4.5+  
**License:** BSD 3-Clause  
**Repository:** https://github.com/MSDN-WhiteKnight/CilTools  

CilView is a windows application to display CIL code of methods in .NET assemblies. The key features are:

- Displaying CIL code of methods in the specified assembly file or process
- Automatically building C#/VB sources and displaying the resulting CIL
- Displaying contents of CIL assembler source files (*.il)
- Syntax highlighting
- Navigation to the referenced method's code by clicking on the method reference
- Exporting CIL code into file
- Displaying process information and stack traces of managed threads (when process is opened)
- Displaying source code from which the method's CIL was compiled
- Executing methods interactively and inspecting the results

## View assembly file

1\. Click **File** -> **Open** menu or press the **Open file** button. 

2\. Select the managed assembly EXE or DLL file in the file open dialog. 

3\. Select type from the **Type** drop-down list in the main window 

4\. Select method in the left panel.

5\. The right panel will display the CIL disassembly of the selected method.

All method references on the **Formatted** tab are hyperlinks to the referenced methods. Clicking one them will open the referenced method's code in the right panel, even if the method is from another assembly, as long as that assembly could be loaded. 

![CilView main window](../images/cilview.png)

## View the resulting CIL for C#/VB sources 

This feature enables you to view disassembled CIL code corresponding to the entered code snippet. CIL View will compile the provided code in background, and open the output binary if the compilation is successful. If the build fails, it will display the build output to you.

1. Click **File** -> **Open code** menu.

2. Paste the input source code into the text area.

3. Select the appropriate language from the combobox.

4. Click **OK**.

5. CIL View will open the output assembly. You can browse its contents as described above.

The "Open code" window only supports a self-contained code snippet without any dependencies or special build options. If you need more complex scenarios, open an MSBuild project file (.csproj/.vbproj) instead. To do this, click **File** -> **Open** menu and select the project file in the file open dialog.

CIL View can compile the provided sources in different ways, depending on your environment. First, it tries to use .NET SDK (dotnet build) if it's installed on your system. If the build fails or .NET SDK is not installed, it tries to build using the MSBuild version bundled with .NET Framework. For .NET SDK, the supported language versions depend on installed SDK version. For .NET Framework MSBuild, C# 5.0 or Visual Basic 11.0 (VS 2012) are supported.

> **NOTE:** On 64-bit Windows CIL View can only use 64-bit .NET CLI (located at `C:\Program Files\dotnet\dotnet.exe`).

## View the contents of CIL assembler source file

1. Click **File** -> **Open** menu or press the **Open file** button. 

2. Select CIL assembler source file (*.il) in the file open dialog.

You cannot navigate between types or methods in the opened CIL in this case, but the syntax highlighting works. Note that opening large files can crash or freeze the application.

## View assemblies in the specified process

1\. Click **File** -> **Open process** menu or press the **Open process** button. 

![CilView attaching to process](../images/cilview2.png)

2\. Select the process from the list in the appearing **Attach to process** dialog.

You can search process by entering the process name starting fragment or ID into the text field and pressing **Search**.

3\. Check the **active mode** checkbox if you want to get more accurate info.

> **NOTE:** When attaching in active mode, the target process is suspended. Do not use active mode on the application that is currently performing critical business tasks.

4\. Press **OK**. The CIL View will scan the target process for loaded assemblies and open them.

5\. Select assembly from the **Assembly** drop-down list in the main window 

6\. Select type from the **Type** drop-down list in the main window

7\. Select method in the left panel.

8\. The right panel will display the CIL disassembly of the selected method.

You can also view the code of dynamic methods generated at runtime in the target process by selecting the `<...DynamicMethods>` entry in the assembly drop-down list (it is displayed at the end of the list). Starting from version 2.2, methods from dynamic assemblies are not included under dynamic methods and instead can be opened under the corresponding dynamic assembly.

## Limitations when displaying code from the process

If the assembly is a dynamic assembly or an assembly that could not be loaded by CIL View as file (this is usually happens with some mixed-mode assemblies which have some PE structures stripped off, so they could be loaded by CLR, but not by System.Reflection.Metadata), the following limitations apply:

- Method return value or parameter types are not shown 
- String literal tokens are not resolved
- Standalone signature tokens are not resolved
- Tokens of external assembly members are not resolved
- Local variables are not shown 
- Exception handling blocks are not shown (except for methods from dynamic assemblies)

For dynamic methods, the following limitations apply:

- Method signatures are not shown 
- All tokens, except for method tokens, are not resolved
- Local variables are not shown

When opening the 64-bit process, the following limitations apply:

- Dynamic assemblies and dynamic methods are not shown
- Assemblies that could not be loaded by CIL View as files (see above) are not shown
- Process and threads information is not available

> **NOTE:** .NET Core shared host is 64-bit on 64-bit Windows, but if you want to overcome these limitations, you can build it targeting the win-x86 runtime ID and start it from the resulting EXE file rather than using shared host.

## Examining managed threads

To examine managed threads when process is opened, click **Process** -> **Threads** menu. The Threads window displays a drop-down list of managed threads. Each list item contains native thread ID, thread type for special threads (GC, Finalizer, Thread pool), COM apartment type (STA or MTA), and the topmost stack frame info. Select thread from the list to view the full stack trace.

The stack trace will be displayed in the left panel. Some stack frames are hyperlinks, and clicking on them will open the corresponding method in the right panel. The instructions belonging to the approximate currently executed code fragment are highlighted in red. This feature only works reliably when attaching in active mode. 

![CilView threads window](../images/cilview3.png)

## Using search

You can search assemblies, types and methods by entering the fragment of their names into the **Find** text field and pressing the "**>**" button. CIL View searches methods in the currently selected type, if the type is selected. In a similar way, types are searched in the currently selected assembly, if one is selected. The search results are shown in the context menu. Clicking on the menu item navigates to the corresponding object.

## Disassembler options

There are a couple of options that control the output of disassembler displayed in CIL View. They are enabled or disabled using checkboxes in **View** menu:

- **View** -> **Include code size**. Enables outputting the method's bytecode size, in bytes, as a code comment at the start of the method body.

- **View** -> **Include source code**. Enables outputting of source code fragments from which particular instructions were compiled. 

CIL View can output source code if symbol files for the disassembled assembly are available and point into valid local source file locations. Supported symbol formats are Windows PDB and Portable PDB. Source code is included as code comments preceding their corresponding instructions. 

## Exception analysis

To figure out exceptions that the method could potentially throw, open that method and use **Tools** -> **Show exception (methods)** menu command. In the opened window you'll see the list of exception types as well as the call stack that could trigger them. The CIL View recursively scans the analysed method and all methods called by it, and searches for exceptions that are thrown and not handled up the stack. Not that exception analysis might be inaccurate (bot false positives and false negatives, so it's only good for a quick estimate of thrown exceptions. To perform exception analysis on all methods of the current type, use **Tools** -> **Show exception (type)**.

To compare exceptions actually thrown by methods of the type and exceptions mentioned in their documentation, select the type and use **Tools** -> **Compare exceptions** command. In the appearing dialog box, select the XML documentation file to compare. CIL View supports both regular ECMA XML emitted by C# compiler and monodoc XML format. The opened window will show the differences between exceptions reported by analysis and exceptions documented in ECMA XML `<exception>` tags.

The exception analysis is supported when opening both files and processes. However, when you hit any limitations mentioned above, the analysis accuracy decreases.

## Source code viewer

This feature enables you to display source code corresponding to disassembled CIL code. Source code viewer can fetch source code using symbols line data (see *Disassembler options* section above for information about symbols support) or display the decompiled code (currently only supported for abstract methods).

To view the source code for individual instructions, click **Tools** -> **Show source code**. Alternatively, you could right-click the instruction of interest in formatted view. In the **View source** window you'll see the CIL code of the sequence point on the left and its corresponding source code fragment on the right. Use **<** and **>** buttons to navigate between sequence points, or the file hyperlink to open the whole source file in your default editor. 

To view the source code for the whole method, **Tools** -> **Show source code (method)**. The **Source code** window will display the method's source code as well as the information about where it was fetched from.

## Method execution

> **WARNING:** Executing a method from unknown origin could be dangerous. Don't use this feature unless you are sure a method does not contain a malicious code! There's no isolation or security control provided for the code being executed.

To execute the current method interactively, use the **Tools** -> **Execute** menu command. In the **Execute method** window, enter values for method parameters, if it has them, specify the timeout value that limits method execution time, and press **Execute**. The CIL View will load the method's assembly for execution and invoke the method using reflection, showing the results to you in a dialog box when it returns. The dialog box will show a method's return value, output parameter values (`ref`/`out`) or exception details, it the method would have thrown an exception. Note that if a method crashes or otherwise somehow corrupts the application state (the assembly is loaded into the main application domain), you might not see the mentioned output at all.

Method execution feature has some limitations. It currently only supports primitive types or types convertible from `string` as method parameters. Instance methods, generic methods and constructors are not supported. Also the assembly in which the method is housed must target .NET Framework or .NET Standard and have x86 or AnyCPU processor architecture.

## ClickOnce installation with auto-update

You can install CIL View via ClickOnce if you want to download updates automatically. The ClickOnce download URL: https://msdn-whiteknight.github.io/CilTools/update/

Using auto-update requires stable internet connection and access to the https://msdn-whiteknight.github.io/ website. If you are using old Windows or .NET Framework versions, you might be unable to connect due to TLS protocol version or ciphersuite mismatch.

## Changelog

2.1

- Update assembly reading mechanism to use CilTools.Metadata instead of loading assemblies directly into the current process. This enables inspecting assemblies for another target framework (such as .NET Standard) or when some dependencies could not be resolved. This also means assemblies can be unloaded from memory when they are no longer needed.
- Add support for method implementation flags when outputting method signatures
- Add support for function pointer types
- Add support for module-level ("global") functions and fields
- Add syntax highlighting for calli instruction's operand
- Add full custom modifiers support (they were previously only supported in standalone signatures)
- Add custom modifier syntax highlighting
- Add pinvokeimpl support
- Add exception block support for dynamic methods
- Add method token resolution for dynamic methods
- Support auto-completion in assembly and type combo boxes
- Improve performance for type combo box when assembly has a lot of types
- Rework search. Search results are now displayed in context menu. Method search is added.
- Format char default values as hex
- Improve custom attribute support (now custom attribute data is properly shown for any attribute)
- Improve empty method body handling. Now empty body is ignored only when method is is abstract, P/Invoke or implemented by runtime. In other cases the error message is displayed.
- Place custom attributes before default parameter values in disassembled method code
- Fix ldloc.s/stloc.s instruction handling
- Fix ldtoken instruction handling with field operand
- Fix exception when method implementation is provided by runtime
- Fix possible null reference exception when reading array/pointer of generic args
- Fix BadImageFormatException on C++/CLI assemblies
- Fix signatures with function pointers being incorrectly displayed in left panel

2.2

- Add support for `constrained.` instruction prefix
- Add type definition disassembler
- Add **Open BCL assembly** dialog
- Add navigation history
- Add partial support for 64-bit processes
- Add support for dynamic assemblies
- Add exception analysis
- Disable wrapping in search textbox
- Method navigation hyperlink now spans only over the method name identifier, instead of the whole method reference syntax
- Method navigation hyperlink is no longer underlined (to fix cases where it was obscuring _ chars in name)
- Improve performance of "Open process" by preloading assemblies from files instead of reading target process memory, where it's possible
- Fix null reference on typedref parameter
- Fix unhandled exception when opening file on background thread
- Fix token resolution bug after navigating to generic method instantiation
- Fix crashes on access to disposed assemblies

2.3

- Escape IL assembler keywords when used as identifiers
- Make search in **Open process** window case-insensitive
- Add support for displaying dynamic assembly names when inspecting process (.NET Framework only)
- Show loaded modules in process info

---------------------------------------------

*Copyright (c) 2022,  MSDN.WhiteKnight*

*This CIL View distribution contains the binary code of [ClrMD](https://github.com/microsoft/clrmd) library: Copyright (c) .NET Foundation and Contributors, MIT License.*
