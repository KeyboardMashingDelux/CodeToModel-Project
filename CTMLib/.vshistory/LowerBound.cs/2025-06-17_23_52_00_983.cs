namespace CTMLib {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LowerBound : Attribute {

        private readonly int Lower = -1;

        public LowerBound(int lower) {
            Lower = lower;
        }

        public int getUpper() => Lower;
    }
}
