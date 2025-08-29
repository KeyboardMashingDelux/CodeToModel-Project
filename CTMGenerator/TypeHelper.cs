using Microsoft.CodeAnalysis;
using NMF.Expressions;
using NMF.Models;
using NMF.Models.Meta;
using IOperation = NMF.Models.Meta.IOperation;
using Attribute = NMF.Models.Meta.Attribute;

namespace CTMGenerator {

    /// <summary>
    /// Helper class for adding type references.
    /// </summary>
    public class TypeHelper {

        public IAttribute? AttributeType {  get; private set; }

        public IReference? Reference { get; private set; }

        public IOperation? Operation { get; private set; }

        public IParameter? Parameter { get; private set; }

        public readonly ITypeSymbol? TypeSymbol;

        private readonly string TypeName;

        private readonly string OppositeName;

        private readonly string RefinesName;



        /// <param name="attribute">Attribute which is missing a type.</param>
        /// <param name="typeSymbol">Type symbol of the missing type reference.</param>
        /// <param name="refinesName">Name of the type which refines this type.</param>
        /// <param name="oppositeName">Name of the opposite type of this type.</param>
        public TypeHelper(IAttribute attribute, ITypeSymbol? typeSymbol = null, string refinesName = "", string oppositeName = "") {
            if (typeSymbol != null) {
                TypeSymbol = typeSymbol;
                TypeName = typeSymbol.Name;
            }
            else {
                TypeName = "";
            }
            AttributeType = attribute;
            OppositeName = oppositeName;
            RefinesName = refinesName;
        }

        /// <param name="reference">Reference which is missing a type.</param>
        /// <param name="typeSymbol">Type symbol of the missing type reference.</param>
        /// <param name="refinesName">Name of the type which refines this type.</param>
        /// <param name="oppositeName">Name of the opposite type of this type.</param>
        public TypeHelper(IReference reference, ITypeSymbol? typeSymbol = null, string refinesName = "", string oppositeName = "") {
            if (typeSymbol != null) {
                TypeSymbol = typeSymbol;
                TypeName = typeSymbol.Name;
            }
            else {
                TypeName = "";
            }
            Reference = reference;
            OppositeName = oppositeName;
            RefinesName = refinesName;
        }

        /// <param name="operation">Operation which is missing a type.</param>
        /// <param name="typeSymbol">Type symbol of the missing type reference.</param>
        /// <param name="refinesName">Name of the type which refines this type.</param>
        /// <param name="oppositeName">Name of the opposite type of this type.</param>
        public TypeHelper(IOperation operation, ITypeSymbol? typeSymbol = null, string refinesName = "", string oppositeName = "") {
            if (typeSymbol != null) {
                TypeSymbol = typeSymbol;
                TypeName = typeSymbol.Name;
            }
            else {
                TypeName = "";
            }
            Operation = operation;
            OppositeName = oppositeName;
            RefinesName = refinesName;
        }

        /// <param name="parameter">Parameter which is missing a type.</param>
        /// <param name="typeSymbol">Type symbol of the missing type reference.</param>
        /// <param name="refinesName">Name of the type which refines this type.</param>
        /// <param name="oppositeName">Name of the opposite type of this type.</param>
        public TypeHelper(IParameter parameter, ITypeSymbol? typeSymbol = null, string refinesName = "", string oppositeName = "") {
            if (typeSymbol != null) {
                TypeSymbol = typeSymbol;
                TypeName = typeSymbol.Name;
            }
            else {
                TypeName = "";
            }
            Parameter = parameter;
            OppositeName = oppositeName;
            RefinesName = refinesName;
        }

        

        public bool SetRefines(ICollectionExpression<IType> types) {
            if (!string.IsNullOrWhiteSpace(RefinesName)) {
                if (AttributeType != null) {
                    return SetAttributeRefines(types);
                }
                else if (Reference != null) {
                    return SetReferenceRefines(types);
                }
                else if (Operation != null) {
                    return SetOperationRefines(types);
                }
            }

            return false;
        }

        public bool SetAttributeRefines(ICollectionExpression<IType> types) {
            if (AttributeType == null) {
                throw new InvalidOperationException($"Can't set Attribute Refines for null! (null -> {RefinesName})");
            }

            IAttribute? refinesAttribute = GetAttribute(types);
            if (refinesAttribute != null) {
                if ((IsCollectionExpression(refinesAttribute)
                    || (!refinesAttribute.IsUnique && AttributeType.IsOrdered == refinesAttribute.IsOrdered))
                    && AttributeType.Type == refinesAttribute.Type
                    && IsBaseTypeOf(AttributeType.Parent, refinesAttribute.Parent)) {
                    AttributeType.Refines = refinesAttribute;
                    return true;
                }
            }

            return false;
        }

        public bool SetReferenceRefines(ICollectionExpression<IType> types) {
            if (Reference == null) {
                throw new InvalidOperationException($"Can't set Reference Refines for null! (null -> {RefinesName})");
            }

            IReference? refinesReference = GetReference(types, RefinesName);
            if (refinesReference != null) {
                if ((IsCollectionExpression(refinesReference)
                    || (!refinesReference.IsUnique && Reference.IsOrdered == refinesReference.IsOrdered)) 
                    && IsBaseTypeOf(Reference.ReferenceType, refinesReference.ReferenceType) 
                    && IsBaseTypeOf(Reference.Parent, refinesReference.Parent)) {
                    Reference.Refines = refinesReference;
                    return true;
                }
            }

            return false;
        }

        public bool SetOperationRefines(ICollectionExpression<IType> types) {
            if (Operation == null) {
                throw new InvalidOperationException($"Can't set Operation Refines for null! (null -> {RefinesName})");
            }

            IOperation? refinesOperation = GetOperation(types);
            if (refinesOperation != null) {
                if ((IsCollectionExpression(refinesOperation)
                    || (!refinesOperation.IsUnique && Operation.IsOrdered == refinesOperation.IsOrdered))
                    && IsBaseTypeOf(Operation.Type, refinesOperation.Type)
                    && IsBaseTypeOf(Operation.Parent, refinesOperation.Parent)) {
                    Operation.Refines = refinesOperation;
                    return true;
                }
            }

            return false;
        }

        public bool SetOpposite(ICollectionExpression<IType> types) {
            if (!string.IsNullOrWhiteSpace(OppositeName) && Reference != null) {
                IReference? oppositeReference = GetReference(types, OppositeName);
                if (oppositeReference is not null) {
                    Reference.Opposite = oppositeReference;
                    return true;
                }
            }

            return false;
        }

        public bool SetType(ICollectionExpression<IType> types) {

            if (!string.IsNullOrWhiteSpace(TypeName)) {
                if (Reference != null) {
                    return SetReferenceType(types);
                }
                else if (Operation != null) {
                    return SetOperationType(types);
                }
                else if (Parameter != null) {
                    return SetParameterType(types);
                }
            }

            return false;
        }

        public bool SetOperationType(ICollectionExpression<IType> types) {
            if (Operation == null) {
                throw new InvalidOperationException($"Can't set Operation type for null! (null -> {TypeName})");
            }

            Operation.Type = GetReferenceType(types) ?? GetPrimitiveType();

            return true;
        }

        public bool SetParameterType(ICollectionExpression<IType> types) {
            if (Parameter == null) {
                throw new InvalidOperationException($"Can't set Operation type for null! (null -> {TypeName})");
            }

            Parameter.Type = GetReferenceType(types) ?? GetPrimitiveType();

            return true;
        }

        public bool SetReferenceType(ICollectionExpression<IType> types) {
            if (Reference == null) {
                throw new InvalidOperationException($"Can't set reference for null! (null -> {TypeName})");
            }

            if (GetReferenceType(types) is not null and IReferenceType refTypeAsIReference) {
                Reference.ReferenceType = refTypeAsIReference;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the <see cref="IType"/> by name from the given types collection.
        /// </summary>
        private IType? GetReferenceType(ICollectionExpression<IType> types) {
            IEnumerable<IType>? possibleRefType = 
                types.Where((type) => type.Name.Equals(TypeName) || type.Name.Equals(TypeName.Substring(1)));
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
                SystemType = TypeSymbol?.ToDisplayString() ?? TypeName
            };
        }

        /// <summary>
        /// Tries to get the <see cref="IAttribute"/> by OppositeName from the given types collection.
        /// </summary>
        private IAttribute? GetAttribute(ICollectionExpression<IType> types) {
            foreach (IType type in types) {
                if (type is IClass attributeParent) {
                    foreach (IAttribute attribute in attributeParent.Attributes) {
                        if (attribute.Name.Equals(RefinesName)) {
                            return attribute;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to get the <see cref="IReference"/> by the given name from the given types collection.
        /// </summary>
        private IReference? GetReference(ICollectionExpression<IType> types, string compareName) {
            foreach (IType type in types) {
                if (type is IClass referenceParent) {
                    foreach (IReference reference in referenceParent.References) {
                        if (reference.Name.Equals(compareName)) {
                            return reference;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to get the <see cref="IOperation"/> by RefinesName from the given types collection.
        /// </summary>
        private IOperation? GetOperation(ICollectionExpression<IType> types) {
            foreach (IType type in types) {
                if (type is IClass operationParent) {
                    foreach (IOperation operation in operationParent.Operations) {
                        if (operation.Name.Equals(RefinesName)) {
                            return operation;
                        }
                    }
                }
            }

            return null;
        }

        private bool IsBaseTypeOf(IModelElement element, IModelElement baseElement) {
            if (element is IClass typeClass && baseElement is IClass baseTypeClass) {
                return baseTypeClass.IsAssignableFrom(typeClass);
            }

            return false;
        }

        private bool IsCollectionExpression(ITypedElement element) {
            if ((element.UpperBound > 1 || element.UpperBound == -1) && !element.IsUnique && !element.IsOrdered) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts this <see cref="IReference"/> to a <see cref="IAttribute"/> and sets its <see cref="IType"/>. 
        /// </summary>
        public IAttribute ReferenceToAttribute(ICollectionExpression<IType> types) {
            if (Reference == null) {
                throw new InvalidOperationException($"Reference was null! Can't convert null to Attribute!");
            }

            IAttribute convertedAttribute = new Attribute() {
                Name = Reference.Name,
                IsUnique = Reference.IsUnique,
                IsOrdered = Reference.IsOrdered,
                LowerBound = Reference.LowerBound,
                UpperBound = Reference.UpperBound,
                Type = GetReferenceType(types) ?? GetPrimitiveType(),
                Remarks = Reference.Remarks,
                Summary = Reference.Summary
            };

            Reference = null;
            AttributeType = convertedAttribute;

            return convertedAttribute;
        }
    }
}
