using System;
using System.Collections.Generic;
using System.Text;

namespace CTMLib {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UpperBound : Attribute {

        private int Upper = -1;

        public UpperBound(int upper) {
            Upper = upper;
        }

        public int getUpper() => Upper;
    }
}
