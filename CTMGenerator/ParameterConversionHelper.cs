using Microsoft.CodeAnalysis;
using NMF.Models.Meta;
using System.Collections.Immutable;
using Parameter = NMF.Models.Meta.Parameter;

namespace CTMGenerator {

    /// <summary>
    /// <see cref="SymbolConversionHelper"/> for <see cref="IParameterSymbol"/>s.
    /// </summary>
    public class ParameterConversionHelper : SymbolConversionHelper {

        /// <summary>
        /// <see cref="List{T}"/> of converted <see cref="IParameter"/>s.
        /// </summary>
        public List<IParameter> Parameters { get; private set; }

        /// <summary>
        /// <see cref="List{T}"/> of <see cref="TypeHelper"/> for the converted symbols.
        /// </summary>
        public List<TypeHelper> RefTypeInfos { get; private set; }



        /// <summary>
        /// Creates an empty <see cref="ParameterConversionHelper"/>.
        /// </summary>
        public ParameterConversionHelper() {
            Parameters = [];
            RefTypeInfos = [];
        }

        /// <summary>
        /// Resets an <see cref="ParameterConversionHelper"/>.
        /// </summary>
        public void Reset() {
            Parameters.Clear();
            RefTypeInfos.Clear();
        }

        /// <summary>
        /// Resets an <see cref="ParameterConversionHelper"/> and then starts the conversion process.
        /// </summary>
        /// <param name="parameterSymbols">The parameters to convert.</param>
        public void CleanConvert(ImmutableArray<IParameterSymbol> parameterSymbols) {
            Reset();
            Convert(parameterSymbols);
        }

        /// <summary>
        /// Converts the given <see cref="List{T}"/> of <see cref="IParameterSymbol"/>s.
        /// </summary>
        /// <remarks>
        /// Parameters which become <see cref="IParameter"/>s will be stored in <see cref="Parameters"/>. <br/>
        /// Type infos will be stored in <see cref="RefTypeInfos"/>. <br/>
        /// </remarks>
        /// <param name="parameterSymbols">The parameters to convert.</param>
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
                    ITypeSymbol refType = isCollection ? typeArgument : type;
                    RefTypeInfos.Add(new TypeHelper(parameter, refType));
                }

                    Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// Gets the parameter direction.
        /// </summary>
        /// <param name="refKind">The kind of a <see cref="IParameterSymbol"/>.</param>
        /// <returns>A parameters directions.</returns>
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
