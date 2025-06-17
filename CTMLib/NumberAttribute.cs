namespace CTMLib {

    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    internal class NumberAttribute : Attribute {

        private readonly int Value = 0;

        public NumberAttribute(int value) {
            Value = value;
        }

        public int getValue() => Value;
    }
}
