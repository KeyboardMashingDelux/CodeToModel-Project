namespace CTMLib {

    /// <summary>
    /// Attribute to set the refines target type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class Refines : Attribute {

        /// <summary>
        /// The name of the refines target type.
        /// </summary>
        public string Target {  get; }

        /// <summary>
        /// Sets the name of the refines target type.
        /// </summary>
        /// <param name="target">Name of the refines target type.</param>
        public Refines(string target) {
            Target = target;
        }
    }
}
