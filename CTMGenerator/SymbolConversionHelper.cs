using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.Collections.Immutable;

namespace CTMGenerator {

    public class SymbolConversionHelper {

        private const string IListExpressionName = nameof(IListExpression<int>);
        private const string ISetExpressionName = nameof(ISetExpression<int>);
        private const string IOrderedSetExpressionName = nameof(IOrderedSetExpression<int>);



        /// <returns>True for <see cref="IListExpression{T}"/>, <see cref="ISetExpression{T}"/> or 
        /// <see cref="IOrderedSetExpression{T}"/></returns>
        public bool IsXExpression(ITypeSymbol type) {
            string typeName = type.Name;
            return typeName.Equals(IListExpressionName)
                || typeName.Equals(ISetExpressionName)
                || typeName.Equals(IOrderedSetExpressionName);
        }

        /// <summary>
        /// Determines if the given type is ordered by NMF standards.
        /// </summary>
        public bool IsOrdered(ITypeSymbol type) {
            return type.Name.Equals(IListExpressionName) || type.Name.Equals(IOrderedSetExpressionName);
        }

        /// <summary>
        /// Determines if the given type is unique by NMF standards.
        /// </summary>
        public bool IsUnique(ITypeSymbol type) {
            return type.Name.Equals(ISetExpressionName) || type.Name.Equals(IOrderedSetExpressionName);
        }

        /// <summary>
        /// Checks if the given type is nullable.
        /// </summary>
        public bool IsNullable(ITypeSymbol type) {
            if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType) {
                if (namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
                    return true;
                }

                foreach (ITypeSymbol typeArgument in namedTypeSymbol.TypeArguments) {
                    if (!(typeArgument.NullableAnnotation == NullableAnnotation.Annotated)) {
                        return false;
                    }
                }
                return true;
            }
            else {
                return type.NullableAnnotation == NullableAnnotation.Annotated;
            }
        }

        /// <summary>
        /// Determines if the given special type is primitive.
        /// </summary>
        public bool IsPrimitive(SpecialType specialType) {
            switch (specialType) {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Char:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_Object: // Don't use?
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Retrives the first type argument of the given type.
        /// </summary>
        public ITypeSymbol? GetTypeArgument(ITypeSymbol type) {
            if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType) {
                return namedTypeSymbol.TypeArguments.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Retrieves the value of the <see cref="LowerBoundAttribute"/>.
        /// </summary>
        /// <param name="isNullable">Whether or not the type the attributes belong to is nullable.</param>
        public int GetLowerBound(ImmutableArray<AttributeData> attributes, bool isNullable) {
            var attribute = Utilities.GetAttributeByName(attributes, nameof(LowerBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? (isNullable ? 0 : 1));
        }

        /// <summary>
        /// Retrieves the value of the <see cref="UpperBoundAttribute"/>.
        /// </summary>
        /// <param name="isCollection">Whether or not the type the attributes belong to is a collection.</param>
        public int GetUpperBound(ImmutableArray<AttributeData> attributes, bool isCollection) {
            var attribute = Utilities.GetAttributeByName(attributes, nameof(UpperBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? (isCollection ? -1 : 1));
        }





        /// #############################
        /// ###   Primitive Creation  ###
        /// #############################



        /// <summary>
        /// Gets the primitive type by the special type.
        /// </summary>
        public IType? GetPrimitiveType(SpecialType specialType) {
            switch (specialType) {
                case SpecialType.System_Boolean:
                    return ResolvePrimitve<bool>();
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    return ResolvePrimitve<byte>();
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    return ResolvePrimitve<short>();
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                    return ResolvePrimitve<int>();
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return ResolvePrimitve<long>();
                case SpecialType.System_Char:
                    return ResolvePrimitve<char>();
                case SpecialType.System_Double:
                    return ResolvePrimitve<double>();
                case SpecialType.System_Single:
                    return ResolvePrimitve<float>();
                case SpecialType.System_String:
                    return ResolvePrimitve<string>();
                case SpecialType.System_Object:
                    return ResolvePrimitve<object>();
                case SpecialType.System_DateTime:
                    return ResolvePrimitve<DateTime>();
                case SpecialType.System_Decimal:
                    return ResolvePrimitve<decimal>();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Contains the NMF friendly aliases of some primitive types.
        /// </summary>
        private readonly Dictionary<System.Type, string> Aliases = new() {
            { typeof(short), "Short" },
            { typeof(int), "Integer" },
            { typeof(long), "Long" },
            { typeof(float), "Float" },
        };

        /// <summary>
        /// Resolves the primitive type from the <see cref="MetaRepository"/>. 
        /// </summary>
        /// <typeparam name="T">The primitve type to resolve.</typeparam>
        public IPrimitiveType ResolvePrimitve<T>() {
            Aliases.TryGetValue(typeof(T), out var primitiveName);
            primitiveName ??= typeof(T).Name;

            return ((IPrimitiveType)(MetaRepository.Instance.Resolve($"http://nmf.codeplex.com/nmeta/#//{primitiveName}")));
        }
    } 
}
