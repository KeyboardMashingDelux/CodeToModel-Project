namespace CTMLib {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UpperBound : Attribute {

        private readonly int Upper = -1;

        public UpperBound(int upper) {
            Upper = upper;
        }

        public int getUpper() => Upper;
    }
}
