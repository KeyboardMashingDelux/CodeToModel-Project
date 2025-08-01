

using Microsoft.CodeAnalysis;
using NMF.Models;
using NMF.Utilities;
using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;

namespace CTMGenerator {
    public class Utilities {

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
        public static List<(string uri, string filename)> GetMetadata(IAssemblySymbol assembly) {
            if (assembly == null)
                return [];

            List<AttributeData> metadataAttributes = Utilities.GetAttributesByName(assembly.GetAttributes(), nameof(ModelMetadataAttribute));
            if (metadataAttributes.IsNullOrEmpty()) {
                return [];
            }

            List<(string uri, string filename)> metadata = [];
            foreach (var attribute in metadataAttributes) {
                var ca = attribute.ConstructorArguments;
                string? uri = ca[0].Value?.ToString();
                string? filename = ca[1].Value?.ToString();

                // Ignore null values
                if (uri == null || filename == null) {
                    continue;
                }

                metadata.Add((uri, filename));
            }

            return metadata;
        }
    }
}
