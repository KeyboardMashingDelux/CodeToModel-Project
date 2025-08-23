using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models;
using NMF.Utilities;
using System.Collections.Immutable;

namespace CTMLib {

    /// <summary>
    /// Contains various utilitie functions for the CodeToModel Library.
    /// </summary>
    public class Utilities {

        /// <summary>
        /// Constant value for the summary xml doc comment element.
        /// </summary>
        public const string SUMMARY = "summary";

        /// <summary>
        /// Constant value for the remarks xml doc comment element.
        /// </summary>
        public const string REMARKS = "remarks";



        /// <summary>
        /// Tries to extracts the name from a NameSyntax node.
        /// </summary>
        public static string ExtractName(NameSyntax name) {
            return name switch {
                SimpleNameSyntax ins => ins.Identifier.Text,
                QualifiedNameSyntax qns => qns.Right.Identifier.Text,
                AliasQualifiedNameSyntax aqns => aqns.Name.Identifier.Text,
                _ => name.ToString()
            };
        }

        /// <summary>
        /// Finds the first occurence of an attribute by the given name.
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AttributeData? GetAttributeByName(ImmutableArray<AttributeData> attributes, string name) {
            foreach (var attribute in attributes) {
                var attrClass = attribute.AttributeClass;
                if (attrClass?.Name == name) {
                    return attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all occurences of an attribute by the given name.
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static List<AttributeData> GetAttributesByName(ImmutableArray<AttributeData> attributes, string name) {
            List<AttributeData> result = [];
            foreach (var attribute in attributes) {
                var attrClass = attribute.AttributeClass;
                if (attrClass?.Name == name) {
                    result.Add(attribute);
                }
            }

            return result;
        }

        /// <summary>
        /// Determins if the attribute comes from <see cref="CTMLib"/>.
        /// </summary>
        public static bool IsLibAttributeClass(INamedTypeSymbol? attributeClass, string attributeName) {
            return attributeClass?.Name == attributeName && attributeClass.ContainingNamespace is {
                Name: nameof(CTMLib),
                ContainingNamespace.IsGlobalNamespace: true
            };
        }

        /// <summary>
        /// Creates a list of all ModelMetadataAttribute attribute data. 
        /// Should a ModelMetadataAttribute attribute hold null references it will be ignored.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<(string uri, string resourceName)> GetMetadata(IAssemblySymbol assembly) {
            if (assembly == null)
                return [];

            List<AttributeData> metadataAttributes = GetAttributesByName(assembly.GetAttributes(), nameof(ModelMetadataAttribute));
            if (metadataAttributes.IsNullOrEmpty()) {
                return [];
            }

            List<(string uri, string filename)> metadata = [];
            foreach (var attribute in metadataAttributes) {
                var ca = attribute.ConstructorArguments;
                string? uri = ca[0].Value?.ToString();
                string? resourceName = ca[1].Value?.ToString();

                // Ignore null values
                if (uri == null || resourceName == null) {
                    continue;
                }

                metadata.Add((uri, resourceName));
            }

            return metadata;
        }

        /// <summary>
        /// An interface should start with and upper case "I" followed by another uppercase letter.
        /// </summary>
        /// <returns><see langword="true"/> if the interface name is valid.</returns>
        public static bool IsValidInterfaceName(string interfaceName) {
            return interfaceName.StartsWith("I") && interfaceName.Length >= 2 && char.IsUpper(interfaceName[1]);
        }
    }
}
