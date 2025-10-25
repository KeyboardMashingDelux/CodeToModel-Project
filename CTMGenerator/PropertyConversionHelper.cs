using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Models;
using NMF.Models.Meta;
using System.Collections.Immutable;
using Attribute = NMF.Models.Meta.Attribute;

namespace CTMGenerator {

    /// <summary>
    /// <see cref="SymbolConversionHelper"/> for <see cref="IPropertySymbol"/>s.
    /// </summary>
    public class PropertyConversionHelper : SymbolConversionHelper {

        /// <summary>
        /// <see cref="List{T}"/> of converted <see cref="IReference"/>s.
        /// </summary>
        public List<IReference> References { get; private set; }

        /// <summary>
        /// <see cref="List{T}"/> of converted <see cref="IAttribute"/>s.
        /// </summary>
        public List<IAttribute> Attributes { get; private set; }

        /// <summary>
        /// <see cref="IAttribute"/> which is marked as id by an <see cref="IdAttribute"/>.
        /// </summary>
        public IAttribute? IdIAttribute { get; private set; }

        /// <summary>
        /// <see cref="List{T}"/> of <see cref="TypeHelper"/> for the converted symbols.
        /// </summary>
        public List<TypeHelper> RefTypeInfos { get; private set; }



        /// <summary>
        /// Creates an empty <see cref="PropertyConversionHelper"/>.
        /// </summary>
        public PropertyConversionHelper() {
            References = [];
            Attributes = [];
            IdIAttribute = null;
            RefTypeInfos = [];
        }

        /// <summary>
        /// Resets an <see cref="PropertyConversionHelper"/>.
        /// </summary>
        public void Reset() {
            References.Clear();
            Attributes.Clear();
            IdIAttribute = null;
            RefTypeInfos.Clear();
        }

        /// <summary>
        /// Resets an <see cref="PropertyConversionHelper"/> and then starts the conversion process.
        /// </summary>
        /// <param name="properties">The properties to convert.</param>
        public void CleanConvert(List<IPropertySymbol> properties) { 
            Reset();
            Convert(properties);
        }

        /// <summary>
        /// Converts the given <see cref="List{T}"/> of <see cref="IPropertySymbol"/>s.
        /// </summary>
        /// <remarks>
        /// Properies which become <see cref="IAttribute"/>s will be stored in <see cref="Attributes"/>. <br/>
        /// Properies which become <see cref="IReference"/>s will be stored in <see cref="References"/>. <br/>
        /// Type infos will be stored in <see cref="RefTypeInfos"/>. <br/>
        /// </remarks>
        /// <param name="properties">The properties to convert.</param>
        public void Convert(List<IPropertySymbol> properties) {
            foreach (IPropertySymbol property in properties) {
                INamedTypeSymbol type = (INamedTypeSymbol)property.Type;
                bool isNullableType = type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

                if (!IsXExpression(property.Type) && type.IsGenericType && !isNullableType) {
                    continue;
                }

                ITypeSymbol typeArgument = GetTypeArgument(type) ?? type;
                bool isCollection = !SymbolEqualityComparer.Default.Equals(type, typeArgument) && !isNullableType;
                SpecialType typeArgumentSpecialType = typeArgument.SpecialType;
                ImmutableArray<AttributeData> propertyAttributes = property.GetAttributes();
                ITypeSymbol checkType = isNullableType ? typeArgument : type;
                SpecialType specialType = checkType.SpecialType;

                // Attribut
                if (isCollection ? IsPrimitive(typeArgumentSpecialType) : IsPrimitive(specialType)) {
                    Attribute attribute = new() {
                        Name = property.Name,
                        IsUnique = isCollection && IsUnique(checkType),
                        IsOrdered = isCollection && IsOrdered(checkType),
                        LowerBound = GetLowerBound(propertyAttributes, IsNullable(type)),
                        UpperBound = GetUpperBound(propertyAttributes, isCollection),
                        Type = GetPrimitiveType(isCollection ? typeArgumentSpecialType : specialType),
                        DefaultValue = ModelBuilderHelper.GetDefaultValue(propertyAttributes),
                        Remarks = ModelBuilderHelper.GetElementRemarks(property),
                        Summary = ModelBuilderHelper.GetElementSummary(property),
                    };

                    if (Utilities.GetAttributeByName(propertyAttributes, nameof(IdAttribute)) != null) {
                        IdIAttribute = attribute;
                    }

                    RefTypeInfos.Add(new TypeHelper(attribute, refinesName: ModelBuilderHelper.GetRefinesTarget(propertyAttributes)));

                    Attributes.Add(attribute);
                }
                // Reference
                else {
                    bool isUnique = IsUnique(checkType);
                    bool isOrdered = IsOrdered(checkType);

                    Reference reference = new() {
                        Name = property.Name,
                        IsUnique = isUnique,
                        IsOrdered = isOrdered,
                        LowerBound = GetLowerBound(propertyAttributes, IsNullable(type)),
                        UpperBound = GetUpperBound(propertyAttributes, isCollection),
                        IsContainment = Utilities.GetAttributeByName(propertyAttributes, nameof(ContainmentAttribute)) != null,
                        Remarks = ModelBuilderHelper.GetElementRemarks(property),
                        Summary = ModelBuilderHelper.GetElementSummary(property)
                    };



                    ITypeSymbol refType = isCollection ? typeArgument : type;
                    RefTypeInfos.Add(
                        new TypeHelper(reference, 
                                       refType,
                                       ModelBuilderHelper.GetRefinesTarget(propertyAttributes),
                                       Utilities.GetSecondString(propertyAttributes, nameof(OppositeAttribute)) ?? "",
                                       ModelBuilderHelper.GetDefaultValue(propertyAttributes)));

                    References.Add(reference);

                    if (IdIAttribute == null && Utilities.GetAttributeByName(propertyAttributes, nameof(IdAttribute)) != null) {
                        IdIAttribute = new Attribute() { Name = Utilities.REFIDATTRIBUTE + reference.Name };
                    }
                }
            }
        }
    }
}
