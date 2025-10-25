using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models;
using NMF.Models.Meta;
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
        /// Helper method, calls <see cref="Utilities.GetFirstString"/> with the name of the <see cref="Refines"/> attribute.
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
        /// Retrieves the value of the <see cref="DefaultValue"/>.
        /// </summary>
        /// <returns>The default value as <see langword="string"/> or an empty <see langword="string"/> if this attribute does not exist</returns>
        public static string? GetDefaultValue(ImmutableArray<AttributeData> attributes) {
            var attribute = Utilities.GetAttributeByName(attributes, nameof(DefaultValue));
            var ca = attribute?.ConstructorArguments;
            return (string?)ca?[0].Value;
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

        public static string? GetModelURI(ITypeSymbol type) {
            return Utilities.GetFirstString(type.GetAttributes(), nameof(ModelRepresentationClassAttribute));
        }
    }
}
