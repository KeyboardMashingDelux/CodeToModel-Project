using NMF.Expressions;
using NMF.Models.Meta;
using Attribute = NMF.Models.Meta.Attribute;

namespace CTMGenerator {

    /// <summary>
    /// Helper class for adding type references.
    /// </summary>
    public class TypeHelper {

        public IReference? Reference { get; private set; }

        public IOperation? Operation { get; private set; }

        public IParameter? Parameter { get; private set; }

        private readonly string TypeName;



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

        /// <param name="parameter">Parameter which is missing a type.</param>
        /// <param name="typeName">Name of the missing type reference. Name and System type should match.</param>
        public TypeHelper(IParameter parameter, string typeName) {
            Parameter = parameter;
            TypeName = typeName;
        }

        public bool SetType(ICollectionExpression<IType> types) {
            if (Reference != null) {
                return SetReferenceType(types);
            } 
            else if (Operation != null) {
                return SetOperationType(types);
            }
            else if (Parameter != null) {
                return SetParameterType(types);
            }

            return false;
        }

        public bool SetOperationType(ICollectionExpression<IType> types) {
            if (Operation == null) {
                throw new InvalidOperationException($"Can't set Operation type for null! (null -> {TypeName})");
            }

            Operation.Type = GetRefType(types) ?? GetPrimitiveType();

            return true;
        }

        public bool SetParameterType(ICollectionExpression<IType> types) {
            if (Parameter == null) {
                throw new InvalidOperationException($"Can't set Operation type for null! (null -> {TypeName})");
            }

            Parameter.Type = GetRefType(types) ?? GetPrimitiveType();

            return true;
        }

        public bool SetReferenceType(ICollectionExpression<IType> types) {
            if (Reference == null) {
                throw new InvalidOperationException($"Can't set reference for null! (null -> {TypeName})");
            }

            if (GetRefType(types) is not null and IReferenceType refTypeAsIReference) {
                Reference.ReferenceType = refTypeAsIReference;
                return true;
            }

            return false;
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

        /// <summary>
        /// Converts the given <see cref="IReference"/> to a <see cref="IAttribute"/> and sets its <see cref="IType"/>. 
        /// </summary>
        public IAttribute ConvertToAttribute(IReference reference, ICollectionExpression<IType> types) {
            return new Attribute() {
                Name = reference.Name,
                IsUnique = reference.IsUnique,
                IsOrdered = reference.IsOrdered,
                LowerBound = reference.LowerBound,
                UpperBound = reference.UpperBound,
                Type = GetRefType(types) ?? GetPrimitiveType(),
                Remarks = reference.Remarks,
                Summary = reference.Summary,
                Refines = null // TODO
            };
        }
    }
}
