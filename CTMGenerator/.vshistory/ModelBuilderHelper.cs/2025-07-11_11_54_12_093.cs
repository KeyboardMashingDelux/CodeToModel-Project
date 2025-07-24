
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
            List<IReference> references = [];
            List<IAttribute> attributes = [];
            Dictionary<string, IReference> opposites = [];
            Dictionary<IReference, string> refTypeInfos = [];
            foreach (var property in properties) {
                var specialType = property.Type.SpecialType;

                // TODO genrisch und collectionexpression überspringen
                //if (type.isGeneric && type.IsCollectionExpression) {
                //    continue;
                //}

                // TODO This is not the way
                //PrimitiveType ptype = new PrimitiveType() {
                //    Name = typeof(bool).Name
                //};
                //MappedType.FromType(ptype).SystemType = typeof(bool);


                // Attribute: TypeArgument (wenn type generisch) / Type ist primitiv

                // TODO Check muss auf Type Argument stattfinden, wenn vorhanden
                if (IsPrimitive(specialType)) {
                    ImmutableArray<AttributeData> propertyAttributes = property.GetAttributes();
                    Attribute attribute = new() {
                        Name = property.Name,
                        IsUnique = false, 
                        IsOrdered = false,
                        LowerBound = GetLowerBound(propertyAttributes),
                        UpperBound = GetUpperBound(propertyAttributes, false),
                        Type = GetPrimitiveType(specialType)
                    };

                    attributes.Add(attribute);
                }
                else {
                    ImmutableArray<AttributeData> propertyAttributes = property.GetAttributes();
                    ITypeSymbol type = property.Type;
                    ITypeSymbol? typeArgument = GetTypeArgument(type);

                    // TODO Wenn generisch nach Expression gucken, wenn nicht generisch dann Fehler
                    //if (IsUnique || IsOrdered && !type.isGeneric) {
                    //    continue;
                    //}

                    Reference reference = new() {
                        Name = property.Name,
                        IsUnique = IsUnique(type, compilation),
                        IsOrdered = IsOrdered(type, compilation),
                        LowerBound = GetLowerBound(propertyAttributes),
                        UpperBound = GetUpperBound(propertyAttributes, typeArgument != null), // If typeArgument not null it means some sort of collection
                        IsContainment = Utilities.GetAttributeByName(propertyAttributes, nameof(ContainmentAttribute)) != null
                    };

                    if (typeArgument != null) {
                        if (IsPrimitive(typeArgument.SpecialType)) {
                            // Add primitive type
                            reference.ReferenceType = new PrimitiveType() { SystemType = typeArgument.SpecialType.ToString()}.GetClass();
                        }
                        else {
                            // Add none primitive
                            refTypeInfos.Add(reference, typeArgument.Name.Substring(1));
                        }
                    }
                    else {
                        if (IsPrimitive(type.SpecialType)) {
                            // Add primitive type
                            reference.ReferenceType = new PrimitiveType() { SystemType = type.SpecialType.ToString() }.GetClass();
                        }
                        else {
                            // Add none primitive
                            refTypeInfos.Add(reference, type.Name.Substring(1));
                        }
                    }

                    references.Add(reference); 

                    string? oppositeName = GetSecondString(propertyAttributes, nameof(OppositeAttribute)); 
                    if (oppositeName != null) {
                        opposites.Add(oppositeName, reference);
                    }
                }
            }

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
                var typeArgument = namedTypeSymbol.TypeArguments.FirstOrDefault();
                if (typeArgument != null) {
                    return typeArgument;
                }
            }

            return null;
        }

        public static bool IsType(ITypeSymbol type, INamedTypeSymbol isType) {
            return SymbolEqualityComparer.Default.Equals(type, isType)
                   || isType.AllInterfaces.Contains(type, SymbolEqualityComparer.Default);
        }

        public static bool IsValidGeneric(ITypeSymbol type) {
            return !(type is ICollectionExpression);
        }

        public static bool IsOrdered(ITypeSymbol type, Compilation compilation) {
            INamedTypeSymbol? listExpressionSymbol = compilation.GetTypeByMetadataName(typeof(IListExpression<>).FullName);
            INamedTypeSymbol? orderedSetExpressionSymbol = compilation.GetTypeByMetadataName(typeof(IOrderedSetExpression<>).FullName);

            bool isIListExpression = listExpressionSymbol == null ? false : IsType(type, listExpressionSymbol);
            bool isIOrderedSetExpression = orderedSetExpressionSymbol == null ? false : IsType(type, orderedSetExpressionSymbol);

            return isIListExpression || isIOrderedSetExpression;
        }

        public static bool IsUnique(ITypeSymbol type, Compilation compilation) {
            INamedTypeSymbol? setExpressionSymbol = compilation.GetTypeByMetadataName(typeof(ISetExpression<>).FullName);
            INamedTypeSymbol? orderedSetExpressionSymbol = compilation.GetTypeByMetadataName(typeof(IOrderedSetExpression<>).FullName);

            bool isSetExpressionSymbol = setExpressionSymbol == null ? false : IsType(type, setExpressionSymbol);
            bool isIOrderedSetExpression = orderedSetExpressionSymbol == null ? false : IsType(type, orderedSetExpressionSymbol);

            return isSetExpressionSymbol || isIOrderedSetExpression;
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

        // TODO 1 oder 0 (Nullable)
        public static int GetLowerBound(ImmutableArray<AttributeData> propertyAttributes) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(LowerBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? 0);
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

        /// <summary>
        /// Extracts the Ambient Namespace of a Namespace. 
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <returns>The ambient namespace or <code>null</code> if this process failed.</returns>
        public static string? GetAmbientNamespaceName(string? namespaceName) {
            if (!string.IsNullOrEmpty(namespaceName)) return "CodeToModel";

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
