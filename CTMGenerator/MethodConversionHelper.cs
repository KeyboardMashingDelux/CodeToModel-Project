using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Models.Meta;
using NMF.Utilities;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace CTMGenerator {

    public class MethodConversionHelper : SymbolConversionHelper {

        public List<Operation> Operations { get; private set; }
        public List<TypeHelper> RefTypeInfos { get; private set; }

        private readonly ParameterConversionHelper ParameterConverter;

        public MethodConversionHelper() {
            ParameterConverter = new();
            Reset();
        }

        public void Reset() {
            Operations = [];
            RefTypeInfos = [];
        }

        public void CleanConvert(List<IMethodSymbol> methods) {
            Reset();
            Convert(methods);
        }

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
                    Refines = null, // TODO
                    Remarks = ModelBuilderHelper.GetElementRemarks(method),
                    Summary = ModelBuilderHelper.GetElementSummary(method)
                };

                if (specialType != SpecialType.System_Void) {
                    if (IsPrimitive(specialType)) {
                        operation.Type = GetPrimitiveType(specialType);
                    }
                    else {
                        // TODO
                        string refName = (isCollection ? typeArgument : returnType).Name;
                        RefTypeInfos.Add(new TypeHelper(operation, refName.StartsWith("I") ? refName.Substring(1) : refName));
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
