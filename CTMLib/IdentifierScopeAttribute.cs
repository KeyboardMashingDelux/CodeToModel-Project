using NMF.Models.Meta;
using Attribute = System.Attribute;

namespace CTMLib {

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class IdentifierScopeAttribute : Attribute {

        public IdentifierScope Scope { get; }

        public IdentifierScopeAttribute(IdentifierScope scope) {
            Scope = scope;
        }
    }
}
