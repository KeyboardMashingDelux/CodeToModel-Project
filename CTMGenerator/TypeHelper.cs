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

        /// <summary>
        /// The <see cref="IAttribute"/> used by this instance. <see langword="null"/> if unused.
        /// </summary>
        public IAttribute? AttributeType {  get; private set; }

        /// <summary>
        /// The <see cref="IReference"/> used by this instance. <see langword="null"/> if unused.
        /// </summary>
        public IReference? Reference { get; private set; }

        /// <summary>
        /// The <see cref="IOperation"/> used by this instance. <see langword="null"/> if unused.
        /// </summary>
        public IOperation? Operation { get; private set; }

        /// <summary>
        /// The <see cref="IParameter"/> used by this instance. <see langword="null"/> if unused.
        /// </summary>
        public IParameter? Parameter { get; private set; }

        /// <summary>
        /// <see cref="ITypeSymbol"/> which from which type infos are derived from. <see langword="null"/> if unused.
        /// </summary>
        public readonly ITypeSymbol? TypeSymbol;

        /// <summary>
        /// The name of the type reference.
        /// </summary>
        private readonly string TypeName;

        /// <summary>
        /// The name of the opposite type reference.
        /// </summary>
        private readonly string OppositeName;

        /// <summary>
        /// The name of the refines type reference.
        /// </summary>
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


        /// <summary>
        /// Sets the refines element of the available 
        /// <see cref="IAttribute"/>, <see cref="IReference"/> or <see cref="IOperation"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
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

        /// <summary>
        /// Sets the refines element of the available <see cref="IAttribute"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        public bool SetAttributeRefines(ICollectionExpression<IType> types) {
            if (AttributeType == null) {
                return false;
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

        /// <summary>
        /// Sets the refines element of the available <see cref="IReference"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        public bool SetReferenceRefines(ICollectionExpression<IType> types) {
            if (Reference == null) {
                return false;
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

        /// <summary>
        /// Sets the refines element of the available <see cref="IOperation"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        public bool SetOperationRefines(ICollectionExpression<IType> types) {
            if (Operation == null) {
                return false;
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

        /// <summary>
        /// Sets the opposite <see cref="IReference"/> of the available <see cref="IReference"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
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

        /// <summary>
        /// Sets the type element of the available 
        /// <see cref="IAttribute"/>, <see cref="IReference"/> or <see cref="IOperation"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
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

        /// <summary>
        /// Sets the type element of the available <see cref="IOperation"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        public bool SetOperationType(ICollectionExpression<IType> types) {
            if (Operation == null) {
                return false;
            }

            Operation.Type = GetReferenceType(types) ?? GetPrimitiveType();

            return true;
        }

        /// <summary>
        /// Sets the type element of the available <see cref="IParameter"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        public bool SetParameterType(ICollectionExpression<IType> types) {
            if (Parameter == null) {
                return false;
            }

            Parameter.Type = GetReferenceType(types) ?? GetPrimitiveType();

            return true;
        }

        /// <summary>
        /// Sets the type element of the available <see cref="IReference"/>.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/>.</returns>
        public bool SetReferenceType(ICollectionExpression<IType> types) {
            if (Reference == null) {
                return false;
            }

            if (GetReferenceType(types) is not null and IReferenceType refTypeAsIReference) {
                Reference.ReferenceType = refTypeAsIReference;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the <see cref="IType"/> by name.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns>The found <see cref="IType"/> or <see langword="null"/> if none was found.</returns>
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
        /// <remarks>
        /// Uses the previously set <see cref="TypeSymbol"/>.
        /// </remarks>
        /// <returns>An <see cref="IPrimitiveType"/>.</returns>
        private IPrimitiveType GetPrimitiveType() {
            return new PrimitiveType() {
                Name = TypeName,
                SystemType = TypeSymbol?.ToDisplayString() ?? TypeName
            };
        }

        /// <summary>
        /// Tries to get the <see cref="IAttribute"/> by <see cref="RefinesName"/> from the given types collection.
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns>The found <see cref="IAttribute"/> or <see langword="null"/> if none was found.</returns>
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
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <param name="compareName">Name of the reference wanted.</param>
        /// <returns>The found <see cref="IReference"/> or <see langword="null"/> if none was found.</returns>
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
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns>The found <see cref="IOperation"/> or <see langword="null"/> if none was found.</returns>
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

        /// <summary>
        /// Determines if a class is a base class of another.
        /// </summary>
        /// <param name="element">The current class.</param>
        /// <param name="baseElement">The supposed base class.</param>
        /// <returns><see langword="true"/> if <paramref name="element"/> is a base class 
        /// of <paramref name="baseElement"/>, otherwise <see langword="false"/>..</returns>
        private bool IsBaseTypeOf(IModelElement element, IModelElement baseElement) {
            if (element is IClass typeClass && baseElement is IClass baseTypeClass) {
                return baseTypeClass.IsAssignableFrom(typeClass);
            }

            return false;
        }

        /// <summary>
        /// Determines if an <see cref="ITypedElement"/> is a <see cref="ICollectionExpression"/>.
        /// </summary>
        /// <param name="element">The <see cref="ITypedElement"/> to check.</param>
        /// <returns><see langword="true"/> if <paramref name="element"/> is a <see cref="ICollectionExpression"/>
        /// , otherwise <see langword="false"/>.</returns>
        private bool IsCollectionExpression(ITypedElement element) {
            if ((element.UpperBound > 1 || element.UpperBound == -1) && !element.IsUnique && !element.IsOrdered) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts this <see cref="IReference"/> to a <see cref="IAttribute"/> and sets its <see cref="IType"/>. 
        /// </summary>
        /// <param name="types">List of available <see cref="IType"/>s.</param>
        /// <returns>The newly created <see cref="IAttribute"/>.</returns>
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
