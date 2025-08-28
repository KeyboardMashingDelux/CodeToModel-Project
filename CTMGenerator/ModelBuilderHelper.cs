
using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace CTMGenerator {

    internal class ModelBuilderHelper {

        public const string DefaultUri = "http://GENERATED.com";
        public const string DefaultResourceName = "GENERATED.FORGOT.ASSEMBLY.INFO.nmeta";



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

        /// <summary>
        /// Helper method, calls <see cref="GetFirstString"/> with the name of the <see cref="Refines"/> attribute.
        /// </summary>
        /// <returns>An empty string instead of <see langword="null"/> or the <see cref="Refines"/> attribute target.</returns>
        public static string GetRefinesTarget(ImmutableArray<AttributeData> attributes) {
            return Utilities.GetFirstString(attributes, nameof(Refines)) ?? "";
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

        /// <summary>
        /// Convenienve method for <see cref="GetDocElementText"/> called with <see cref="Utilities.SUMMARY"/>.
        /// </summary>
        public static string? GetElementSummary(ISymbol element) {
            return GetDocElementText(element, Utilities.SUMMARY);
        }

        /// <summary>
        /// Convenienve method for <see cref="GetDocElementText"/> called with <see cref="Utilities.REMARKS"/>.
        /// </summary>
        public static string? GetElementRemarks(ISymbol element) {
            return GetDocElementText(element, Utilities.REMARKS);
        }

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
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public static (string fullName, string name, string ambientName, string prefix, string suffix) GetResourceInfo(string? resourceName) {
            if (resourceName == null) {
                return GetResourceInfo(DefaultResourceName);
            }

            string[] parts = resourceName.Split('.');
            int length = parts.Length;
            if (length < 3) {
                return GetResourceInfo(DefaultResourceName);
            }

            string fullName = string.Join(".", parts.Take(length - 2));
            string name = parts[length - 3];
            string ambientName = string.Join(".", parts.Take(length - 3));
            string prefix = parts[length - 2];
            string suffix = parts[length - 1];

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

        public static bool ImplementsIModelElement(INamedTypeSymbol namedType, Compilation compilation) {
            foreach (SyntaxReference syntaxRef in namedType.DeclaringSyntaxReferences) {
                InterfaceDeclarationSyntax? syntaxNode = syntaxRef.GetSyntax() as InterfaceDeclarationSyntax;
                if (syntaxNode == null) {
                    continue;
                }

                bool isModelInterfae = syntaxNode.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => Utilities.ExtractName(attr.Name).Equals(nameof(ModelInterface)));

                if (isModelInterfae) {
                    SemanticModel model = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
                    return syntaxNode.BaseList?.Types
                            .Any(baseTypeSyntax => model
                                    .GetTypeInfo(baseTypeSyntax.Type).Type?
                                    .Name.Equals(nameof(IModelElement)) ?? false) ?? false;
                }
            }

            return false;
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
    }
}
