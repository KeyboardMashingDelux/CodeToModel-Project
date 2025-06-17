using System;
using System.Collections.Generic;
using System.Text;

namespace CTMLib {

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    internal class IsAbstract : Attribute {
    }
}
