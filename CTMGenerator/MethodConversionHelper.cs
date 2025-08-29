using Microsoft.CodeAnalysis;
using NMF.Utilities;
using System.Collections.Immutable;
using Operation = NMF.Models.Meta.Operation;

namespace CTMGenerator {

    /// <summary>
    /// <see cref="SymbolConversionHelper"/> for <see cref="IMethodSymbol"/>s.
    /// </summary>
    public class MethodConversionHelper : SymbolConversionHelper {

        /// <summary>
        /// <see cref="List{T}"/> of converted <see cref="Operation"/>s.
        /// </summary>
        public List<Operation> Operations { get; private set; }

        /// <summary>
        /// <see cref="List{T}"/> of <see cref="TypeHelper"/> for the converted symbols.
        /// </summary>
        public List<TypeHelper> RefTypeInfos { get; private set; }

        private readonly ParameterConversionHelper ParameterConverter;



        /// <summary>
        /// Creates an empty <see cref="MethodConversionHelper"/>.
        /// </summary>
        public MethodConversionHelper() {
            ParameterConverter = new();
            Operations = [];
            RefTypeInfos = [];
        }

        /// <summary>
        /// Resets an <see cref="MethodConversionHelper"/>.
        /// </summary>
        public void Reset() {
            Operations.Clear();
            RefTypeInfos.Clear();
        }

        /// <summary>
        /// Resets an <see cref="MethodConversionHelper"/> and then starts the conversion process.
        /// </summary>
        /// <param name="methods">The methods to convert.</param>
        public void CleanConvert(List<IMethodSymbol> methods) {
            Reset();
            Convert(methods);
        }

        /// <summary>
        /// Converts the given <see cref="List{T}"/> of <see cref="IMethodSymbol"/>s.
        /// </summary>
        /// <remarks>
        /// Methods which become <see cref="Operation"/>s will be stored in <see cref="Operations"/>. <br/>
        /// Type infos will be stored in <see cref="RefTypeInfos"/>. <br/>
        /// </remarks>
        /// <param name="methods">The methods to convert.</param>
        public void Convert(List<IMethodSymbol> methods) {
            foreach (IMethodSymbol method in methods) {
                ITypeSymbol returnType = method.ReturnType;
                bool isNullableType = returnType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                ImmutableArray<AttributeData> methodAttributes = method.GetAttributes();

                ITypeSymbol typeArgument = GetTypeArgument(returnType) ?? returnType;
                bool isCollection = !SymbolEqualityComparer.Default.Equals(returnType, typeArgument) && !isNullableType;
                SpecialType typeArgumentSpecialType = typeArgument.SpecialType;
                ITypeSymbol checkType = isNullableType ? typeArgument : returnType;
                SpecialType specialType = checkType.SpecialType;

                Operation operation = new() {
                    Name = method.Name,
                    IsUnique = IsUnique(returnType),
                    IsOrdered = IsOrdered(returnType),
                    LowerBound = GetLowerBound(methodAttributes, IsNullable(returnType) ||returnType.SpecialType == SpecialType.System_Void),
                    UpperBound = GetUpperBound(methodAttributes, isCollection),
                    Remarks = ModelBuilderHelper.GetElementRemarks(method),
                    Summary = ModelBuilderHelper.GetElementSummary(method)
                };

                if (specialType != SpecialType.System_Void) {
                    if (IsPrimitive(specialType)) {
                        operation.Type = GetPrimitiveType(specialType);
                    }
                    else {
                        ITypeSymbol refType = isCollection ? typeArgument : returnType;
                        RefTypeInfos.Add(
                            new TypeHelper(operation,
                                           refType,
                                           ModelBuilderHelper.GetRefinesTarget(methodAttributes)));
                    }
                }

                ParameterConverter.CleanConvert(method.Parameters);
                operation.Parameters.AddRange(ParameterConverter.Parameters);
                RefTypeInfos.AddRange(ParameterConverter.RefTypeInfos);

                Operations.Add(operation);
            }
        }
    }
}
