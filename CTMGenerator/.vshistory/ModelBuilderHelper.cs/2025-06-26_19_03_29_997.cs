

using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Models;
using NMF.Models.Meta;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace CTMGenerator {
    public class ModelBuilderHelper {

        public const string DefaultUri = "http://GENERATED.com";
        public const string DefaultFilename = "GENERATED.fsm.nmeta";



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

        public static (List<IReference> references, List<IAttribute> attributes) ConvertProperties(List<IPropertySymbol> properties) {
            List<IReference> references = [];
            List<IAttribute> attributes = [];
            Dictionary<string, IReference> opposites = [];
            foreach (var property in properties) {
                var specialType = property.Type.SpecialType;
                if (IsPrimitive(specialType)) {
                    Debugger.Launch();
                }
                else {
                    ImmutableArray<AttributeData> propertyAttributes = property.GetAttributes();
                    Reference reference = new() {
                        Name = property.Name,
                        ReferenceType = new Class() { Name = property.Type.ToDisplayString()},
                        IsUnique = Utilities.GetAttributeByName(propertyAttributes, nameof(IsUnique)) != null,
                        IsOrdered = Utilities.GetAttributeByName(propertyAttributes, nameof(IsOrdered)) != null,
                        LowerBound = GetLowerBound(propertyAttributes),
                        UpperBound = GetUpperBound(propertyAttributes),
                        IsContainment = Utilities.GetAttributeByName(propertyAttributes, nameof(ContainmentAttribute)) != null
                    };

                    string? oppositeName = GetOppositeName(propertyAttributes);
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

            return (references, attributes);
        }

        private static string? GetOppositeName(ImmutableArray<AttributeData> propertyAttributes) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(OppositeAttribute));
            return GetFirstString(attribute);
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

        public static int GetLowerBound(ImmutableArray<AttributeData> propertyAttributes) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(LowerBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? 0);
        }

        public static int GetUpperBound(ImmutableArray<AttributeData> propertyAttributes) {
            var attribute = Utilities.GetAttributeByName(propertyAttributes, nameof(UpperBoundAttribute));
            var ca = attribute?.ConstructorArguments;
            return (int)(ca?[0].Value ?? -1);
        }

        public static string? GetFirstString(AttributeData? attribute) {
            var ca = attribute?.ConstructorArguments;
            return ca?[0].Value?.ToString();
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
                return namespaceName;
            }

            return namespaceName.Substring(0, lastDot);
        }

        /// <summary>
        /// Extracts name, prefix and suffix from filename. It is assumed the filename has the following syntax: name.prefix.suffix.
        /// If the wrong filename syntax is used, uses the default filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static (string name, string prefix, string suffix) GetFilenameInfo(string? filename) {
            if (filename == null) {
                return GetFilenameInfo(DefaultFilename);
            }

            int firstDot = filename.IndexOf('.');
            int lastDot = filename.LastIndexOf('.');

            if (firstDot == -1 || lastDot == -1 || firstDot == lastDot) {
                return GetFilenameInfo(DefaultFilename);
            }

            return (filename.Substring(0, firstDot), filename.Substring(firstDot + 1, lastDot - firstDot - 1), filename.Substring(lastDot + 1));
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
