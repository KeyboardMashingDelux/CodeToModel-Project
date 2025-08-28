using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Models;
using NMF.Models.Meta;
using System.Collections.Immutable;
using Attribute = NMF.Models.Meta.Attribute;

namespace CTMGenerator {

    public class PropertyConversionHelper : SymbolConversionHelper {

        public List<IReference> References { get; private set; }
        public List<IAttribute> Attributes { get; private set; }
        public IAttribute? IdAttribute { get; private set; }
        public List<TypeHelper> RefTypeInfos { get; private set; }



        public PropertyConversionHelper() {
            References = [];
            Attributes = [];
            IdAttribute = null;
            RefTypeInfos = [];
        }

        public void Reset() {
            References.Clear();
            Attributes.Clear();
            IdAttribute = null;
            RefTypeInfos.Clear();
        }

        public void CleanConvert(List<IPropertySymbol> properties) { 
            Reset();
            Convert(properties);
        }

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
                        Remarks = ModelBuilderHelper.GetElementRemarks(property),
                        Summary = ModelBuilderHelper.GetElementSummary(property),
                    };

                    if (Utilities.GetAttributeByName(propertyAttributes, nameof(IdAttribute)) != null) {
                        IdAttribute = attribute;
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

                    string refName = (isCollection ? typeArgument : type).Name;
                    RefTypeInfos.Add(
                        new TypeHelper(reference, 
                                       refName,
                                       ModelBuilderHelper.GetRefinesTarget(propertyAttributes),
                                       Utilities.GetSecondString(propertyAttributes, nameof(OppositeAttribute)) ?? ""));

                    References.Add(reference);

                    if (IdAttribute == null && Utilities.GetAttributeByName(propertyAttributes, nameof(IdAttribute)) != null) {
                        IdAttribute = new Attribute() { Name = Utilities.REFIDATTRIBUTE + reference.Name };
                    }
                }
            }
        }
    }
}
