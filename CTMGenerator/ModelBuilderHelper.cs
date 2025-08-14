
using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CSharp;
using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using NMF.Models.Meta;
using NMF.Models.Repository;
using NMF.Transformations.Core;
using NMF.Utilities;
using System;
using System.CodeDom;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using static NMF.Models.Meta.Meta2ClassesTransformation;
using Attribute = NMF.Models.Meta.Attribute;
using IOperation = NMF.Models.Meta.IOperation;
using Parameter = NMF.Models.Meta.Parameter;
using SystemAttribute = System.Attribute;

namespace CTMGenerator {

    internal class ModelBuilderHelper {

        public const string DefaultUri = "http://GENERATED.com";
        public const string DefaultFilename = "GENERATED.FORGOT.ASSEMBLY.INFO.nmeta";



        /// #############################
        /// ###   Member Conversion   ###
        /// #############################



        public static (List<IPropertySymbol> properties, List<IMethodSymbol> methodes) GetClassMembers(ImmutableArray<ISymbol> members) {
            List<IPropertySymbol> properties = [];
            List<IMethodSymbol> methodes = [];

            foreach (var member in members) {
                switch (member) {
                    case IPropertySymbol property:
                        properties.Add(property);
                        break;

                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                        methodes.Add(method);
                        break;

                    // Events are currently not handeled
                    case IEventSymbol eventMember:
                        break;

                    // Skip accessors (get/set/add/remove)
                    default:
                        continue;
                }
            }

            return (properties, methodes);
        }

        public static (List<IReference> references, List<IAttribute> attributes, IAttribute? idAttribute)
            ConvertProperties(List<IPropertySymbol> properties, out List<TypeHelper> refTypeInfos) {

            List<IReference> references = [];
            List<IAttribute> attributes = [];
            IAttribute? idAttribute = null;
            refTypeInfos = [];
            Dictionary<string, IReference> opposites = [];
            foreach (IPropertySymbol property in properties) {
                INamedTypeSymbol type = (INamedTypeSymbol)property.Type;
                bool isNullableType = type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

                if (!IsXExpression(property) && type.IsGenericType && !isNullableType) {
                    // (type.BaseType != null && type.BaseType.Name.Equals(nameof(ICollectionExpression)))
                    continue;
                }

                // TODO Kommen Listen von Listen durch? Wenn ja dafür sorgen, dass übersprungen wird

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
                        Remarks = GetDocElementText(property, Utilities.REMARKS),
                        Summary = GetDocElementText(property, Utilities.SUMMARY),
                        Refines = null // TODO
                    };

                    if (Utilities.GetAttributeByName(propertyAttributes, nameof(IdAttribute)) != null) {
                        idAttribute = attribute;
                    } 

                    attributes.Add(attribute);
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
                        Remarks = GetDocElementText(property, Utilities.REMARKS),
                        Summary = GetDocElementText(property, Utilities.SUMMARY),
                        Refines = null // TODO
                    };

                    // TODO Assumes the ref is a model interface - What if just a normal Object?
                    string refName = (isCollection ? typeArgument : type).Name;
                    refTypeInfos.Add(new TypeHelper(reference, refName.StartsWith("I") ? refName.Substring(1) : refName));

                    references.Add(reference);

                    string? oppositeName = GetSecondString(propertyAttributes, nameof(OppositeAttribute));
                    if (oppositeName != null) {
                        opposites.Add(oppositeName, reference);
                    }
                }
            }

            // TODO Kann opposite in anderem Interface sein? -> Ja
            // Wie refernzen später machen
            //foreach (var opposite in opposites) {
            //    string oppositeName = opposite.Key;
            //    IReference thisRef = opposite.Value;

            //    if (opposites.ContainsKey(thisRef.Name)) {
            //        IReference oppositeRef = opposites[thisRef.Name];
            //        thisRef.Opposite = oppositeRef;
            //    }
            //}

            return (references, attributes, idAttribute);
        }

        public static List<Operation> ConvertMethods(List<IMethodSymbol> methods, out List<TypeHelper> refTypeInfos) {
            List<Operation> operations = [];
            refTypeInfos = [];

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
                    LowerBound = GetLowerBound(methodAttributes, IsNullable(returnType)),
                    UpperBound = GetUpperBound(methodAttributes, isCollection),
                    Refines = null, // TODO
                    Remarks = GetDocElementText(method, Utilities.REMARKS),
                    Summary = GetDocElementText(method, Utilities.SUMMARY)
                };

                if (specialType != SpecialType.System_Void) {
                    if (IsPrimitive(specialType)) {
                        operation.Type = GetPrimitiveType(specialType);
                    }
                    else {
                        refTypeInfos.Add(new TypeHelper(operation, checkType.Name));
                    }
                }

                ConvertParameters(method.Parameters);

                operations.Add(operation);
            }

            return operations;
        }

        public static List<Parameter> ConvertParameters(ImmutableArray<IParameterSymbol> parameterSymbols) {
            List<Parameter> parameters = [];



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
                    Remarks = GetDocElementText(parameterSymbol, Utilities.REMARKS), // TODO Testen ob ignoriert wird
                    Summary = GetDocElementText(parameterSymbol, Utilities.SUMMARY) // oder Probleme bereitet
                };

                if (IsPrimitive(checkType.SpecialType)) {
                    parameter.Type = GetPrimitiveType(checkType.SpecialType);
                }

                parameters.Add(parameter);
            }

            return parameters;
        }

        public static List<ILiteral> ConvertLiterals(List<IFieldSymbol> literalSymbols) {
            List<ILiteral> literals = [];

            foreach (IFieldSymbol literalSymbol in literalSymbols) {
                Literal literal = new() {
                    Name = literalSymbol.Name,
                    Value = (int?)literalSymbol.ConstantValue,
                    Remarks = GetDocElementText(literalSymbol, Utilities.REMARKS),
                    Summary = GetDocElementText(literalSymbol, Utilities.SUMMARY)
                };

                literals.Add(literal);
            }

            return literals;
        }





        /// #############################
        /// ###   Primitive Creation  ###
        /// #############################



        /// <summary>
        /// Gets the primitive type by the special type.
        /// </summary>
        public static IType? GetPrimitiveType(SpecialType specialType) {
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
        private static readonly Dictionary<System.Type, string> Aliases = new() {
            { typeof(short), "Short" },
            { typeof(int), "Integer" },
            { typeof(long), "Long" },
            { typeof(float), "Float" },
        };

        /// <summary>
        /// Resolves the primitive type from the <see cref="MetaRepository"/>. 
        /// </summary>
        /// <typeparam name="T">The primitve type to resolve.</typeparam>
        public static IPrimitiveType ResolvePrimitve<T>() {
            Aliases.TryGetValue(typeof(T), out var primitiveName);
            primitiveName ??= typeof(T).Name;

            return ((IPrimitiveType)(MetaRepository.Instance.Resolve($"http://nmf.codeplex.com/nmeta/#//{primitiveName}")));
        }





        /// #############################
        /// ###   Conversion Helper   ###
        /// #############################



        /// <summary>
        /// Retrives the first type argument of the given type.
        /// </summary>
        public static ITypeSymbol? GetTypeArgument(ITypeSymbol type) {
            if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType) {
                return namedTypeSymbol.TypeArguments.FirstOrDefault();
            }

            return null;
        }

        /// <returns>True for <see cref="IListExpression{T}"/>, <see cref="ISetExpression{T}"/> or 
        /// <see cref="IOrderedSetExpression{T}"/></returns>
        public static bool IsXExpression(ISymbol type) {
            string typeName = type.Name;
            return typeName.Equals(nameof(IListExpression<int>))
                || typeName.Equals(nameof(ISetExpression<int>))
                || typeName.Equals(nameof(IOrderedSetExpression<int>));
        }

        /// <summary>
        /// Gets the parameter direction.
        /// </summary>
        public static Direction GetDirection(RefKind refKind) {
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

        /// <summary>
        /// Determines if the given special type is primitive.
        /// </summary>
        public static bool IsPrimitive(SpecialType specialType) {
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
        /// Determines if the given type is ordered by NMF standards.
        /// </summary>
        public static bool IsOrdered(ITypeSymbol type) {
            return type.Name.Equals(nameof(IListExpression<int>)) || type.Name.Equals(nameof(IOrderedSetExpression<int>));
        }

        /// <summary>
        /// Determines if the given type is unique by NMF standards.
        /// </summary>
        public static bool IsUnique(ITypeSymbol type) {
            return type.Name.Equals(nameof(ISetExpression<int>)) || type.Name.Equals(nameof(IOrderedSetExpression<int>));
        }

        /// <summary>
        /// Checks if the given type is nullable.
        /// </summary>
        public static bool IsNullable(ITypeSymbol type) {
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
        /// Retrieves the value of the <see cref="LowerBoundAttribute"/>.
        /// </summary>
        /// <param name="isNullable">Whether or not the type the attributes belong to is nullable.</param>
        public static int GetLowerBound(ImmutableArray<AttributeData> attributes, bool isNullable) {
            var attribute = Utilities.GetAttributeByName(attributes, nameof(LowerBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? (isNullable ? 0 : 1));
        }

        /// <summary>
        /// Retrieves the value of the <see cref="UpperBoundAttribute"/>.
        /// </summary>
        /// <param name="isCollection">Whether or not the type the attributes belong to is a collection.</param>
        public static int GetUpperBound(ImmutableArray<AttributeData> attributes, bool isCollection) {
            var attribute = Utilities.GetAttributeByName(attributes, nameof(UpperBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? (isCollection ? -1 : 1));
        }

        /// <summary>
        /// Retrievs the attribute from the given list by name.
        /// </summary>
        /// <param name="index">Represents the strings constructor index</param>
        public static string? GetAttributeString(ImmutableArray<AttributeData> attributes, string attributeName, int index) {
            var attribute = Utilities.GetAttributeByName(attributes, attributeName);
            var ca = attribute?.ConstructorArguments;
            return ca?[index].Value?.ToString();
        }

        /// <summary>
        /// Helper method, calls <see cref="GetAttributeString"/> with index 0.
        /// </summary>
        public static string? GetFirstString(ImmutableArray<AttributeData> attributes, string attributeName) {
            return GetAttributeString(attributes, attributeName, 0);
        }

        /// <summary>
        /// Helper method, calls <see cref="GetAttributeString"/> with index 1.
        /// </summary>
        public static string? GetSecondString(ImmutableArray<AttributeData> attributes, string attributeName) {
            return GetAttributeString(attributes, attributeName, 1);
        }

        /// <summary>
        /// Retrieves the <see cref="IdentifierScope"/> of the <see cref="IdentifierScopeAttribute"/>.
        /// </summary>
        /// <returns><see cref="IdentifierScope"/> or <see langword="null"/> if this attribute does not exist</returns>
        public static IdentifierScope GetIdentifierScope(ImmutableArray<AttributeData> attributes) {
            var attribute = Utilities.GetAttributeByName(attributes, nameof(IdentifierScopeAttribute));
            var ca = attribute?.ConstructorArguments;
            return (IdentifierScope?)ca?[0].Value ?? IdentifierScope.Inherit;
        }





        /// #############################
        /// ###         MISC          ###
        /// #############################



        /// <summary>
        /// Gets the text from the doc comment of the given <see cref="ISymbol"/> from the given name.
        /// </summary>
        /// <returns><see langword="null"/> if no text could be extracted.</returns>
        public static string? GetDocElementText(ISymbol element, string docElementName) {
            if (string.IsNullOrWhiteSpace(docElementName)) {
                return null;
            }

            string? xml = element.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml)) {
                return null;
            }

            XDocument doc = XDocument.Parse(xml);
            return doc.Root.Element(docElementName)?.Value.Trim();
        }

        /// <summary>
        /// Extracts the Ambient Namespace of a Namespace. 
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <returns>The ambient namespace or <code>null</code> if this process failed.</returns>
        public static string? GetAmbientNamespaceName(string? namespaceName) {
            if (!string.IsNullOrEmpty(namespaceName))
                return "CodeToModel";

            if (namespaceName == null) {
                return namespaceName;
            }

            int lastDot = namespaceName.LastIndexOf('.');
            if (lastDot == -1) {
                return null;
            }

            return namespaceName.Substring(0, lastDot);
        }

        /// <summary>
        /// Extracts name, prefix and suffix from filename. It is assumed the filename has the following syntax: name.prefix.suffix.
        /// If the wrong filename syntax is used, uses the default filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static (string fullName, string name, string ambientName, string prefix, string suffix) GetFilenameInfo(string? filename) {
            if (filename == null) {
                return GetFilenameInfo(DefaultFilename);
            }

            string[] parts = filename.Split('.');

            if (parts.Length < 3) {
                return GetFilenameInfo(DefaultFilename);
            }

            string fullName = string.Join(".", parts[0..^2]);
            string name = parts[^3];
            string ambientName = string.Join(".", parts[0..^3]);
            string prefix = parts[^2];
            string suffix = parts[^1];

            return (fullName, name, ambientName, prefix, suffix);
        }

        /// <summary>
        /// Tries to get the path based on the given ITypeSymbol. This will result in the path of the file. 
        /// This can be the project root or another folder inside the project.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>Directory path which contains the file.</returns>
        public static string? GetSavePath(ITypeSymbol symbol) {
            string? path = null;
            var locations = symbol.Locations;
            foreach (var location in locations) {
                if (string.IsNullOrWhiteSpace(path)) {
                    path = location.SourceTree?.FilePath;
                }
                else {
                    break;
                }
            }

            return Path.GetDirectoryName(path);
        }

        public static string GetAccessibility(Accessibility accessibility) {
            return accessibility == Accessibility.NotApplicable ? "" : accessibility.ToString().ToLower();
        }
    }
}
