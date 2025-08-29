using NMF.Models.Meta;
using Attribute = System.Attribute;

namespace CTMLib {

    /// <summary>
    /// Attribute to set the <see cref="IdentifierScope"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class IdentifierScopeAttribute : Attribute {

        /// <summary>
        /// The <see cref="IdentifierScope"/>.
        /// </summary>
        public IdentifierScope Scope { get; }

        /// <summary>
        /// Sets the <see cref="IdentifierScope"/>.
        /// </summary>
        /// <param name="scope"><see cref="IdentifierScope"/> of the type.</param>
        public IdentifierScopeAttribute(IdentifierScope scope) {
            Scope = scope;
        }
    }
}
