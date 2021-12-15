/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection.Methods
{
    internal class MethodBodyData
    {
        ITokenResolver resolver;
        int _stacksize;
        bool _hasstacksize;
        byte[] _code;
        byte[] _localssig;
        ExceptionBlock[] _blocks;
        bool _localsinit;
        bool _haslocalsinit;

        public MethodBodyData(MethodBase srcmethod)
        {
            this.resolver = CustomMethod.CreateResolver(srcmethod);

            if (Types.IsDynamicMethod(srcmethod))
            {
                FieldInfo field;
                this._code = GetDynamicMethodBytecode(srcmethod);

                FieldInfo fieldResolver = srcmethod.GetType().GetField("m_resolver",
                    ReflectionUtils.InstanceMembers);
                object valueResolver = fieldResolver.GetValue(srcmethod);

                if (valueResolver != null)
                {
                    field = valueResolver.GetType().GetField("m_localSignature",
                        ReflectionUtils.InstanceMembers
                        );
                    byte[] sigbytes = (byte[])field.GetValue(valueResolver);
                    this._localssig = sigbytes;

                    field = valueResolver.GetType().GetField("m_stackSize",
                        ReflectionUtils.InstanceMembers
                        );

                    this._stacksize = (int)field.GetValue(valueResolver);
                    this._hasstacksize = true;
                    this._blocks = GetDynamicMethodExceptions(valueResolver, this._code.Length);
                }
                else
                {
                    this._localssig = new byte[0];
                    this._hasstacksize = false;
                    this._haslocalsinit = false;
                }
            }
            else //regular method
            {
                MethodBody body = srcmethod.GetMethodBody();

                if (body != null)
                {
                    this._code = body.GetILAsByteArray();
                    int token = body.LocalSignatureMetadataToken;

                    if (token == 0) this._localssig = new byte[0];
                    else this._localssig = this.resolver.ResolveSignature(token);

                    this._stacksize = body.MaxStackSize;
                    this._hasstacksize = true;

                    this._blocks = new ExceptionBlock[body.ExceptionHandlingClauses.Count];

                    for (int i = 0; i < body.ExceptionHandlingClauses.Count; i++)
                    {
                        this._blocks[i] = ExceptionBlock.FromReflection(body.ExceptionHandlingClauses[i]);
                    }

                    this._localsinit = body.InitLocals;
                    this._haslocalsinit = true;
                }
                else
                {
                    this._code = new byte[0];
                    this._localssig = new byte[0];
                    this._blocks = new ExceptionBlock[0];
                    this._hasstacksize = false;
                    this._haslocalsinit = false;
                }
            }//endif
        }

        public ITokenResolver TokenResolver
        {
            get
            {
                return this.resolver;
            }
        }

        public byte[] GetBytecode()
        {
            return this._code;
        }

        public byte[] GetLocalVarSignature()
        {
            return this._localssig;
        }

        public int MaxStackSize
        {
            get { return this._stacksize; }
        }

        public bool MaxStackSizeSpecified
        {
            get { return this._hasstacksize; }
        }

        public ExceptionBlock[] GetExceptionBlocks()
        {
            return this._blocks;
        }

        public bool InitLocals
        {
            get { return this._localsinit; }
        }

        public bool InitLocalsSpecified
        {
            get { return this._haslocalsinit; }
        }

        static byte[] GetDynamicMethodBytecode(MethodBase srcmethod)
        {
            FieldInfo fieldGenerator = srcmethod.GetType().GetField("m_ilGenerator",
                    ReflectionUtils.InstanceMembers);
            object valueGenerator = fieldGenerator.GetValue(srcmethod);
            FieldInfo field;

            if (valueGenerator != null)
            {
                field = Types.ILGeneratorType.GetField("m_ILStream",
                    ReflectionUtils.InstanceMembers
                    );
                byte[] ilbytes = (byte[])field.GetValue(valueGenerator);

                field = Types.ILGeneratorType.GetField("m_length",
                    ReflectionUtils.InstanceMembers
                    );
                int len = (int)field.GetValue(valueGenerator);

                field = valueGenerator.GetType().GetField("m_methodBuilder",
                    ReflectionUtils.InstanceMembers
                    );
                MethodBase val = (MethodBase)field.GetValue(valueGenerator);

                byte[] il = new byte[len];
                Array.Copy(ilbytes, il, len);
                return il;
            }
            else
            {
                return new byte[0];
            }
        }

        static ExceptionBlock[] GetDynamicMethodExceptions(object valueResolver, int bytecodeLength)
        {
            FieldInfo field = valueResolver.GetType().GetField("m_exceptions",
                        ReflectionUtils.InstanceMembers);

            object[] arrExceptions = (object[])field.GetValue(valueResolver);

            if (arrExceptions == null) arrExceptions = new object[0];

            List<ExceptionBlock> blocks = new List<ExceptionBlock>(arrExceptions.Length * 2);

            Type t = arrExceptions.GetType().GetElementType();

            FieldInfo fieldStartAddr = t.GetField("m_startAddr", ReflectionUtils.InstanceMembers);
            FieldInfo fieldEndAddr = t.GetField("m_endAddr", ReflectionUtils.InstanceMembers);
            FieldInfo fieldCatchAddr = t.GetField("m_catchAddr", ReflectionUtils.InstanceMembers);
            FieldInfo fieldCatchClass = t.GetField("m_catchClass", ReflectionUtils.InstanceMembers);
            FieldInfo fieldCatchEndAddr = t.GetField("m_catchEndAddr", ReflectionUtils.InstanceMembers);
            FieldInfo fieldFilterAddr = t.GetField("m_filterAddr", ReflectionUtils.InstanceMembers);
            FieldInfo fieldType = t.GetField("m_type", ReflectionUtils.InstanceMembers);

            for (int i = 0; i < arrExceptions.Length; i++)
            {
                int startaddr = (int)fieldStartAddr.GetValue(arrExceptions[i]);
                int endAddr = (int)fieldEndAddr.GetValue(arrExceptions[i]);
                int[] catchAddr = (int[])fieldCatchAddr.GetValue(arrExceptions[i]);
                int[] catchEndAddr = (int[])fieldCatchEndAddr.GetValue(arrExceptions[i]);
                Type[] catchClass = (Type[])fieldCatchClass.GetValue(arrExceptions[i]);
                int[] filterAddr = (int[])fieldFilterAddr.GetValue(arrExceptions[i]);
                int[] type = (int[])fieldType.GetValue(arrExceptions[i]);

                for (int j = 0; j < type.Length; j++)
                {
                    int length = endAddr - startaddr;
                    int handler_length = catchEndAddr[j] - catchAddr[j];
                    int filter_offset = filterAddr[j];

                    if (handler_length <= 0) continue;

                    if (filter_offset < 0 || filter_offset > bytecodeLength) filter_offset = 0;

                    blocks.Add(new ExceptionBlock(
                                (ExceptionHandlingClauseOptions)type[j], startaddr, length,
                                catchClass[j], catchAddr[j],
                                handler_length, filter_offset
                                ));
                }
            }

            return blocks.ToArray();
        }
    }
}
