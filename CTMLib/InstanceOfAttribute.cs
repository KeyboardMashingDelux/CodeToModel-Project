using Attribute = System.Attribute;

namespace CTMLib {

    /// <summary>
    /// Attribute to set the InstanceOf class name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class InstanceOfAttribute : Attribute {

        /// <summary>
        /// The name of the InstanceOf type.
        /// </summary>
        public string Type { get; }


        /// <summary>
        /// Sets the type name.
        /// </summary>
        /// <param name="type">Name of the type.</param>
        public InstanceOfAttribute(string type) {
            Type = type;
        }
    }
}
