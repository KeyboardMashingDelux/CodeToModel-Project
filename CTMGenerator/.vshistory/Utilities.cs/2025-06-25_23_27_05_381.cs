

using Microsoft.CodeAnalysis;
using NMF.Models;
using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;

namespace CTMGenerator {
    public class Utilities {
        private const string AttributesLibName = "CTMLib";

        public static AttributeData? GetAttributeByName(ImmutableArray<AttributeData> attributes, string name) {
            foreach (var attribute in attributes) {
                var attrClass = attribute.AttributeClass;
                if (attrClass?.Name == name && attrClass.ContainingNamespace is {
                                                            Name: AttributesLibName,
                                                            ContainingNamespace.IsGlobalNamespace: true
                                                            }
                    ) {
                    return attribute;
                }
            }

            return null;
        }
    }
}
