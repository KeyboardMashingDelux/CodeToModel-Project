using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Models.Meta;
using System.Collections.Immutable;
using System.Xml.Linq;
using Parameter = NMF.Models.Meta.Parameter;

namespace CTMGenerator {

    public class ParameterConversionHelper : SymbolConversionHelper {

        public List<Parameter> Parameters { get; private set; }

        public List<TypeHelper> RefTypeInfos { get; private set; }



        public ParameterConversionHelper() {
            Reset();
        }

        public void Reset() {
            Parameters = [];
            RefTypeInfos = [];
        }

        public void CleanConvert(ImmutableArray<IParameterSymbol> parameterSymbols) {
            Reset();
            Convert(parameterSymbols);
        }

        public void Convert(ImmutableArray<IParameterSymbol> parameterSymbols) {
            foreach (IParameterSymbol parameterSymbol in parameterSymbols) {
                if (parameterSymbol.IsThis) {
                    continue;
                }

                ITypeSymbol type = parameterSymbol.Type;
                ImmutableArray<AttributeData> parameterAttributes = parameterSymbol.GetAttributes();
                bool isNullableType = type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

                ITypeSymbol typeArgument = GetTypeArgument(type) ?? type;
                bool isCollection = !isNullableType && !SymbolEqualityComparer.Default.Equals(type, typeArgument);
                ITypeSymbol checkType = isNullableType ? typeArgument : type;

                Parameter parameter = new() {
                    Name = parameterSymbol.Name,
                    Direction = GetDirection(parameterSymbol.RefKind),
                    IsUnique = IsUnique(type),
                    IsOrdered = IsOrdered(type),
                    LowerBound = GetLowerBound(parameterAttributes, IsNullable(type)),
                    UpperBound = GetUpperBound(parameterAttributes, isCollection),
                    Remarks = ModelBuilderHelper.GetElementRemarks(parameterSymbol), // TODO Testen ob ignoriert wird
                    Summary = ModelBuilderHelper.GetElementSummary(parameterSymbol) // oder Probleme bereitet
                };

                if (IsPrimitive(checkType.SpecialType)) {
                    parameter.Type = GetPrimitiveType(checkType.SpecialType);
                }
                else {
                    string refName = (isCollection ? typeArgument : type).Name;
                    RefTypeInfos.Add(new TypeHelper(parameter, refName.StartsWith("I") ? refName.Substring(1) : refName));
                }

                    Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// Gets the parameter direction.
        /// </summary>
        public Direction GetDirection(RefKind refKind) {
            switch (refKind) {
                case RefKind.In:
                case RefKind.RefReadOnlyParameter:
                case RefKind.None:
                    return Direction.In;
                case RefKind.Out:
                    return Direction.Out;
                case RefKind.Ref:
                    return Direction.InOut;
                default:
                    return Direction.In;
            }
        }
    }
}
