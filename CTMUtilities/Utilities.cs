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
        /// Identifier for a reference which is marked as id attribute.
        /// </summary>
        public const string REFIDATTRIBUTE = "R3F1DATTr1but3-/$§)$=JKLDJSD)?9DLJKLAS(";



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
        /// <param name="attributes">List of possible attributes</param>
        /// <param name="name">Name of the wanted attribute</param>
        /// <returns>The attributes <see cref="AttributeData"/> or <see langword="null"/> if none was found.</returns>
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
        /// <param name="attributes">List of possible attributes</param>
        /// <param name="name">Name of the wanted attribute</param>
        /// <returns><see cref="List{T}"/> of attributes <see cref="AttributeData"/>.</returns>
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
        /// <param name="attributeClass">Class which belongs to the attribute</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <returns><see langword="true"/> if the attribute is part of <see cref="CTMLib"/>.</returns>
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
        /// <param name="assembly">The <see cref="IAssemblySymbol"/> which contains the <see cref="ModelMetadataAttribute"/> data.</param>
        /// <returns>Two <see cref="List{T}"/> which contain model URIs and resouce names.</returns>
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
        /// <param name="interfaceName">Name to check.</param>
        /// <returns><see langword="true"/> if the interface name is valid.</returns>
        public static bool IsValidInterfaceName(string interfaceName) {
            return interfaceName.StartsWith("I") && interfaceName.Length >= 2 && char.IsUpper(interfaceName[1]);
        }

        /// <summary>
        /// Retrievs the attribute from the given list by name.
        /// </summary>
        /// <param name="attributes">List of possible attributes.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="index">Represents the strings constructor index</param>
        public static string? GetAttributeString(ImmutableArray<AttributeData> attributes, string attributeName, int index) {
            var attribute = GetAttributeByName(attributes, attributeName);
            var ca = attribute?.ConstructorArguments;
            return ca?[index].Value?.ToString();
        }

        /// <summary>
        /// Helper method, calls <see cref="GetAttributeString"/> with index 0.
        /// </summary>
        /// <param name="attributes">List of possible attributes.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        public static string? GetFirstString(ImmutableArray<AttributeData> attributes, string attributeName) {
            return GetAttributeString(attributes, attributeName, 0);
        }

        /// <summary>
        /// Helper method, calls <see cref="GetAttributeString"/> with index 1.
        /// </summary>
        /// <param name="attributes">List of possible attributes.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        public static string? GetSecondString(ImmutableArray<AttributeData> attributes, string attributeName) {
            return GetAttributeString(attributes, attributeName, 1);
        }
    }
}
