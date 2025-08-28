namespace CTMLib {

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class Refines : Attribute {

        public string Target {  get; }

        public Refines(string target) {
            Target = target;
        }
    }
}
