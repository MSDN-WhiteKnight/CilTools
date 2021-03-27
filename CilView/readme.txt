== CilView ==

Requirements: .NET Framework 4.5+
License: BSD 3-Clause
Repository URL: https://github.com/MSDN-WhiteKnight/CilTools

CilView is a windows application to display CIL code of methods in the .NET assemblies. The key features are:

- Displaying CIL code of methods in the specified assembly file or process
- Syntax highlighting
- Navigation to the referenced method's code by clicking on the method reference
- Exporting the method's code into file
- Displaying process information and stack traces of managed threads (when process is opened)

---------------------------------------------

* How to display code from the assembly file *

1. Click "File -> Open" menu or press the "Open file" button. 
2. Select the managed assembly EXE or DLL file in the file open dialog. 
3. Select type from the "Type" drop-down list in the main window

You can also search type by entering the fragment of the type name to "Find" text field and pressing the ">" button. The search will select the matching type if it's found. Press the button again to go to the next matching type. The program will display message if when the end of list is reached.

4. Select method in the left panel.
5. The right panel will display the CIL disassembly of the selected method.

All method references on the "Formatted" tab are hyperlinks to the referenced methods. Clicking one them will open the referenced method's code in the right panel, even if the method is from another assembly, as long as that assembly could be loaded. 

* How to display code from the process *

1. Click "File -> Open process" menu or press the "Open process" button. 
2. Select the process from the list in the appearing "Attach to process" dialog.

You can search process by entering the process name starting fragment or ID into the text field and pressing "Search".

3. Check the "active mode" checkbox if you want to get more accurate info.

NOTE: When attaching in active mode, the target process is suspended. Do not use active mode on the application that is currently performing critical business tasks.

4. Press "OK". The CIL View will scan the target process for loaded assemblies and open them.
5. Select assembly from the "Assembly" drop-down list in the main window 
6. Select type from the "Type" drop-down list in the main window
7. Select method in the left panel.

You can also search assemblies, types and methods by entering the fragment of their names into the "Find" text field and pressing the ">" button. CIL View searches methods in the currently selected type, if the type is selected. In a similar way, types are searched in the currently selected assembly, if one is selected. The search results are shown in the context menu. Clicking on the menu item navigates to the corresponding object.

8. The right panel will display the CIL disassembly of the selected method.

You can also view the code of dynamic methods generated at runtime in the target process by selecting the `<...DynamicMethods>` entry in the assembly drop-down list (it is displayed at the end of the list). Starting from version 2.2, methods from dynamic assemblies are not included under dynamic methods and instead can be opened under the corresponding dynamic assembly.

* Limitations when displaying code from the process *

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

NOTE: .NET Core shared host is 64-bit on 64-bit Windows, but if you want to overcome these limitations, you can build it targeting the win-x86 runtime ID and start it from the resulting EXE file rather than using shared host.

* Examining managed threads *

To examine managed threads when process is opened, click "Process -> Threads" menu. The Threads window displays a drop-down list of managed threads. Each list item contains native thread ID, thread type for special threads (GC, Finalizer, Thread pool), COM apartment type (STA or MTA), and the topmost stack frame info. Select thread from the list to view the full stack trace.

The stack trace will be displayed in the left panel. Some stack frames are hyperlinks, and clicking on them will open the corresponding method in the right panel. The instructions belonging to the approximate currently executed code fragment are highlighted in red. This feature only works reliably when attaching in active mode. 

* ClickOnce installation with auto-update *

You can install CIL View via ClickOnce if you want to download updates automatically. The ClickOnce download URL: https://msdn-whiteknight.github.io/CilTools/update/

Using auto-update requires stable internet connection and access to the https://msdn-whiteknight.github.io/ website. If you are using old Windows or .NET Framework versions, you might be unable to connect due to TLS protocol version or ciphersuite mismatch.

---------------------------------------------

Copyright (c) 2021,  MSDN.WhiteKnight

This CIL View distribution contains the binary code of ClrMD library (https://github.com/microsoft/clrmd): Copyright (c) .NET Foundation and Contributors, MIT License. 
