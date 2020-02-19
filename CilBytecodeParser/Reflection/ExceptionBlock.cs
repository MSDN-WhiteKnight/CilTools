using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    internal struct ExceptionBlock
    {
        Type _CatchType;
        int _FilterOffset;
        ExceptionHandlingClauseOptions _Flags;
        int _HandlerLength;
        int _HandlerOffset;
        int _TryLength;
        int _TryOffset;

        public ExceptionBlock(
            ExceptionHandlingClauseOptions pFlags,
            int pTryOffset,
            int pTryLength,            
            Type pCatchType,
            int pHandlerOffset,
            int pHandlerLength,            
            int pFilterOffset
            )
        {
            _CatchType = pCatchType;
            _FilterOffset = pFilterOffset;
            _Flags = pFlags;
            _HandlerLength = pHandlerLength;
            _HandlerOffset = pHandlerOffset;
            _TryLength = pTryLength;
            _TryOffset = pTryOffset;
        }

        public Type CatchType { get { return this._CatchType; } }
        public int FilterOffset { get { return this._FilterOffset; } }
        public ExceptionHandlingClauseOptions Flags { get { return this._Flags; } }
        public int HandlerLength { get { return this._HandlerLength; } }
        public int HandlerOffset { get { return this._HandlerOffset; } }
        public int TryLength { get { return this._TryLength; } }
        public int TryOffset { get { return this._TryOffset; } }

        public static ExceptionBlock FromReflection(ExceptionHandlingClause clause)
        {
            int filter = 0;
            Type t = null;

            if ((clause.Flags & ExceptionHandlingClauseOptions.Filter) != 0) filter = clause.FilterOffset;

            if(clause.Flags == ExceptionHandlingClauseOptions.Clause) t = clause.CatchType;

            return new ExceptionBlock(
                clause.Flags,
                clause.TryOffset, clause.TryLength,
                t, clause.HandlerOffset, clause.HandlerLength,
                filter
                );
        }
    }
}
