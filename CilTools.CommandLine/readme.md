# CIL Tools command line

Command line tool to view disassembled CIL code of methods in .NET assemblies.

Commands:

**view** - Print CIL code of methods or the content of CIL source files.

*Usage*

Print disassembled CIL code of method or methods with the specified name: 

    cil view [--nocolor] <assembly path> <type full name> <method name>
    
Print contents of the specified CIL source file (*.il):

    cil view [--nocolor] <source file path>

[--nocolor] - Disable syntax highlighting

**disasm** - Write CIL code of the method or methods with the specified name into file.

*Usage* 

    cil disasm [--output <output path>] <assembly path> <type full name> <method name>

[--output \<output path\>] - Output file path

**help** - Print available commands.

---

Copyright (c) 2022, MSDN.WhiteKnight (<https://github.com/MSDN-WhiteKnight/CilTools>)
