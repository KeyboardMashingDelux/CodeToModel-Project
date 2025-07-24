

using Microsoft.CodeAnalysis;
using NMF.Models;
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
    }
}
