
using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CSharp;
using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using NMF.Models.Meta;
using NMF.Models.Repository;
using NMF.Transformations.Core;
using System;
using System.CodeDom;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using static NMF.Models.Meta.Meta2ClassesTransformation;
using Attribute = NMF.Models.Meta.Attribute;
using SystemAttribute = System.Attribute;

namespace CTMGenerator {
    public class ModelBuilderHelper {

        public const string DefaultUri = "http://GENERATED.com";
        public const string DefaultFilename = "GENERATED.FORGOT.ASSEMBLY.INFO.nmeta";



        public static (List<IPropertySymbol> properties, List<IMethodSymbol> methodes, List<IEventSymbol> events) GetClassMembers(ImmutableArray<ISymbol> members) {
            List<IPropertySymbol> properties = [];
            List<IMethodSymbol> methodes = [];
            List<IEventSymbol> events = [];

            foreach (var member in members) {
                switch (member) {
                    case IPropertySymbol property:
                        properties.Add(property);
                        break;

                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                        methodes.Add(method);
                        break;

                    case IEventSymbol eventMember:
                        events.Add(eventMember);
                        break;

                    // Skip accessors (get/set/add/remove)
                    default:
                        continue;
                }
            }

            return (properties, methodes, events);
        }

        public static string GetAccessibility(Accessibility accessibility) {
            return accessibility == Accessibility.NotApplicable ? "" : accessibility.ToString().ToLower();
        }

        public static (List<IReference> references, List<IAttribute> attributes, Dictionary<IReference, string> refTypeInfos) ConvertProperties(List<IPropertySymbol> properties, Compilation compilation) {

            //Debugger.Launch();

            List<IReference> references = [];
            List<IAttribute> attributes = [];
            Dictionary<string, IReference> opposites = [];
            Dictionary<IReference, string> refTypeInfos = [];
            foreach (IPropertySymbol property in properties) {
                INamedTypeSymbol type = (INamedTypeSymbol) property.Type;
                bool isNullableType = type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;


                // TODO CreateAttribute & CreateReference methoden erstellen
                // Bei Nullable das typeArgument nutzen?


                // TODO genrisch und collectionexpression überspringen
                // -> Funktionier weil IListExpression nicht generisch und das ist gewünscht anstatt IList?
                if (//(type.IsGenericType && !isNullableType) ||
                    (type.BaseType != null && type.BaseType.Name.Equals(nameof(ICollectionExpression)))) {
                        continue;
                }

                // TODO Was wenn meherere TypeArguments z.B. eigene Dictionary implementation
                ITypeSymbol typeArgument = GetTypeArgument(type) ?? type;
                bool isCollection = !SymbolEqualityComparer.Default.Equals(type, typeArgument) && !isNullableType;
                SpecialType typeArgumentSpecialType = typeArgument.SpecialType;
                ImmutableArray<AttributeData> propertyAttributes = property.GetAttributes();
                ITypeSymbol checkType = isNullableType ? typeArgument : type;
                SpecialType specialType = checkType.SpecialType;

                int lowerBound = GetLowerBound(propertyAttributes, IsNullable(type));
                int upperBound = GetUpperBound(propertyAttributes, isCollection);

                // TODO Bestimmen ob es nur Get / Set gibt?

                // Attribut
                if (isCollection ? IsPrimitive(typeArgumentSpecialType) : IsPrimitive(specialType)) {
                    Attribute attribute = new() {
                        Name = property.Name,
                        IsUnique = isCollection && IsUnique(checkType),
                        IsOrdered = isCollection && IsOrdered(checkType),
                        LowerBound = GetLowerBound(propertyAttributes, IsNullable(type)),
                        UpperBound = GetUpperBound(propertyAttributes, isCollection),
                        Type = GetPrimitiveType(isCollection ? typeArgumentSpecialType : specialType),
                    };

                    attributes.Add(attribute);
                }
                // Reference
                else {
                    bool isUnique = IsUnique(checkType);
                    bool isOrdered = IsOrdered(checkType);

                    // TODO Wenn generisch nach Expression gucken, wenn nicht generisch dann Fehler
                    //if ((isUnique || isOrdered) && !isNullableType && checkType is INamedTypeSymbol namedType && namedType.IsGenericType) {
                    //    continue;
                    //}

                    Reference reference = new() {
                        Name = property.Name,
                        IsUnique = isUnique,
                        IsOrdered = isOrdered,
                        LowerBound = lowerBound,
                        UpperBound = upperBound,
                        IsContainment = Utilities.GetAttributeByName(propertyAttributes, nameof(ContainmentAttribute)) != null
                    };

                    // TODO Assumes the ref is a interface - What if just a normal Object?
                    refTypeInfos.Add(reference, (isCollection ? typeArgument : type).Name.Substring(1));

                    references.Add(reference);

                    string? oppositeName = GetSecondString(propertyAttributes, nameof(OppositeAttribute));
                    if (oppositeName != null) {
                        opposites.Add(oppositeName, reference);
                    }
                }


                // TODO This is the way Für Enumerationen
                //PrimitiveType ptype = new PrimitiveType() {
                //    Name = typeof(bool).Name,
                //    SystemType = typeof(bool).FullName
                //};
            }

            // TODO Kann opposite in anderem Interface sein?
            foreach (var opposite in opposites) {
                string oppositeName = opposite.Key;
                IReference thisRef = opposite.Value;

                if (opposites.ContainsKey(thisRef.Name)) {
                    IReference oppositeRef = opposites[thisRef.Name];
                    thisRef.Opposite = oppositeRef;
                }
            }

            return (references, attributes, refTypeInfos);
        }

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


        private static readonly Dictionary<System.Type, string> Aliases = new() {
            { typeof(short), "Short" },
            { typeof(int), "Integer" },
            { typeof(long), "Long" },
            { typeof(float), "Float" },
        };

        public static IPrimitiveType ResolvePrimitve<T>() {
            Aliases.TryGetValue(typeof(T), out var primitiveName);
            primitiveName ??= typeof(T).Name;

            return ((IPrimitiveType)(MetaRepository.Instance.Resolve($"http://nmf.codeplex.com/nmeta/#//{primitiveName}")));
        }

        public static ITypeSymbol? GetTypeArgument(ITypeSymbol type) {
            if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType) {
                return namedTypeSymbol.TypeArguments.FirstOrDefault();
            }

            return null;
        }

        public static bool IsOrdered(ITypeSymbol type) {
            return type.Name.Equals(nameof(IListExpression<int>)) || type.Name.Equals(nameof(IOrderedSetExpression<int>));
        }

        public static bool IsUnique(ITypeSymbol type) {
            return type.Name.Equals(nameof(ISetExpression<int>)) || type.Name.Equals(nameof(IOrderedSetExpression<int>));
        }

        public static (List<IReference> references, List<IAttribute> attributes) ConvertMethods(List<IMethodSymbol> methods) {
            List<IReference> references = [];
            List<IAttribute> attributes = [];
            foreach (var method in methods) {

            }
            return (references, attributes);
        }

        public static (List<IReference> references, List<IAttribute> attributes) ConvertEvents(List<IEventSymbol> events) {
            List<IReference> references = [];
            List<IAttribute> attributes = [];
            foreach (var eventSymbol in events) {

            }
            return (references, attributes);
        }

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

        public static int GetLowerBound(ImmutableArray<AttributeData> propertyAttributes, bool isNullable) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(LowerBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? (isNullable ? 0 : 1));
        }

        public static int GetUpperBound(ImmutableArray<AttributeData> propertyAttributes, bool isCollection) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(UpperBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? (isCollection ? -1 : 1));
        }

        public static string? GetFirstString(ImmutableArray<AttributeData> propertyAttributes, string attributeName) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, attributeName);
            var ca = attribute?.ConstructorArguments;
            return ca?[0].Value?.ToString();
        }

        public static string? GetSecondString(ImmutableArray<AttributeData> propertyAttributes, string attributeName) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, attributeName);
            var ca = attribute?.ConstructorArguments;
            return ca?[1].Value?.ToString();
        }

        public static IdentifierScope? GetIdentifierScope(ImmutableArray<AttributeData> propertyAttributes) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(IdentifierScopeAttribute));
            var ca = attribute?.ConstructorArguments;
            return (IdentifierScope?)ca?[0].Value;
        }





        /// #############################
        /// ###         MISC          ###
        /// #############################



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
    }
}
