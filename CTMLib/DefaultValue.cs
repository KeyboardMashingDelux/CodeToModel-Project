using Attribute = System.Attribute;

namespace CTMLib {

    /// <summary>
    /// Attribute to set the InstanceOf class name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DefaultValue : Attribute {

        /// <summary>
        /// The name of the InstanceOf type.
        /// </summary>
        public string Value { get; }


        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <param name="defaultValue">Default value.</param>
        public DefaultValue(string defaultValue) {
            Value = defaultValue;
        }
    }
}
