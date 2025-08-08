using NMF.Expressions;
using NMF.Models.Meta;

namespace CTMGenerator {

    /// <summary>
    /// Helper class for adding type references.
    /// </summary>
    public class TypeHelper {

        private IReference? Reference;

        private IOperation? Operation;

        private string TypeName;

        /// <param name="reference">Reference which is missing a type.</param>
        /// <param name="typeName">Name of the missing type reference. Name and System type should match.</param>
        public TypeHelper(IReference reference, string typeName) {
            Reference = reference;
            TypeName = typeName;
        }

        /// <param name="operation">Operation which is missing a type.</param>
        /// <param name="typeName">Name of the missing type reference. Name and System type should match.</param>
        public TypeHelper(IOperation operation, string typeName) {
            Operation = operation;
            TypeName = typeName;
        }

        public void SetType(ICollectionExpression<IType> types) {
            if (Reference != null) {
                SetReferenceType(types);
            } 
            else if (Operation != null) {
                SetOperationType(types);
            }
        }

        public void SetOperationType(ICollectionExpression<IType> types) {
            if (Operation == null) {
                throw new InvalidOperationException($"Can't set Operation type for null! (null -> {TypeName})");
            }

            IType? refType = GetRefType(types);
            if (refType is not null) {
                Operation.Type = refType;
                return;
            }

            Operation.Type = GetPrimitiveType();
        }

        public void SetReferenceType(ICollectionExpression<IType> types) {
            if (Reference == null) {
                throw new InvalidOperationException($"Can't set reference for null! (null -> {TypeName})");
            }

            IType? refType = GetRefType(types);
            // TODO Brauche Enum Referenz -> Enumeration aber keine Referenez SÄDILÖHDFÖLACJ
            if (refType is not null) {
                Reference.ReferenceType = (IReferenceType)refType;
                return;
            }

            // TODO Not an option
            // Bedeutet sollte keine Referenz sein?
            // Aber bei erzeugung kann ja nicht bekannt sein ob Teil des Modells?
            // Im nachhinein Referenz in Attribut umwandeln?
            Reference.ReferenceType = (IReferenceType)GetPrimitiveType();
        }

        /// <summary>
        /// Tries to get the type by name from the given types collection.
        /// </summary>
        private IType? GetRefType(ICollectionExpression<IType> types) {
            IEnumerable<IType>? possibleRefType = types.Where((type) => type.Name.Equals(TypeName));
            if (possibleRefType != null && possibleRefType.Count() == 1) {
                return possibleRefType.First();
            }

            return null;
        }

        /// <summary>
        /// Creates the primitive type by name.
        /// </summary>
        private IPrimitiveType GetPrimitiveType() {
            return new PrimitiveType() {
                Name = TypeName,
                SystemType = TypeName
            };
        }
    }
}
