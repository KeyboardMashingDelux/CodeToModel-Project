using Attribute = System.Attribute;

namespace CTMLib {

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class InstanceOfAttribute : Attribute {

        public string Type { get; }

        public InstanceOfAttribute(string type) {
            Type = type;
        }
    }
}
