/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core.Reflection
{
    public class MethodExecutionResults
    {
        public object ReturnValue { get; set; }

        public Type ReturnValueType { get; set; }

        public Exception ExceptionObject { get; set; }

        public bool IsTimedOut { get; set; }

        public TimeSpan Duration { get; set; }

        public string GetText()
        {
            StringBuilder sb = new StringBuilder();

            if (this.IsTimedOut)
            {
                sb.AppendLine("Method execution did not completed within the specified timeout interval");
            }
            else if (this.ExceptionObject != null)
            {
                sb.AppendLine("Executing method resulted in exception:");
                sb.AppendLine(this.ExceptionObject.ToString());
            }
            else if (this.ReturnValueType.Equals(typeof(void)))
            {
                sb.AppendLine("Method does not have a return value");
            }
            else 
            {
                if (this.ReturnValue != null)
                {
                    sb.AppendLine("Return value: ");
                    sb.AppendLine(this.ReturnValue.ToString());
                }
                else
                {
                    sb.AppendLine("Method returned null");
                }

                sb.AppendLine("Return value type: " + this.ReturnValueType.ToString());
            }

            sb.AppendLine();
            sb.AppendLine("Duration: " + this.Duration.TotalMilliseconds.ToString() + " ms");

            return sb.ToString();
        }
    }
}
