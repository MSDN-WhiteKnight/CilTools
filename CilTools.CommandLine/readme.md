# CIL Tools command line

Command line tool to view disassembled CIL code of methods in .NET assemblies.

Commands:

**view** - Print CIL code of types or methods or the content of CIL source files.

*Usage*

Print disassembled CIL code of the specified type or method: 

    cil view [--nocolor] <assembly path> <type full name> [<method name>]
    
Print contents of the specified CIL source file (*.il):

    cil view [--nocolor] <source file path>

[--nocolor] - Disable syntax highlighting

**disasm** - Write disassembled CIL code of the specified type or method into the file.

*Usage* 

    cil disasm [--output <output path>] <assembly path> <type full name> [<method name>]

[--output \<output path\>] - Output file path

**help** - Print available commands.

---

Copyright (c) 2022, MSDN.WhiteKnight (<https://github.com/MSDN-WhiteKnight/CilTools>)
