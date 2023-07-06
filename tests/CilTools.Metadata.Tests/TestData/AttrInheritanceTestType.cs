/* CilTools.Metadata tests
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using CilTools.Tests.Common;

namespace CilTools.Metadata.Tests.TestData
{
    [My(444)]
    public class CustomAttrsTestType { }

    [Category("Unicorns")]
    public class AttrInheritanceTestType : CustomAttrsTestType { }
}
