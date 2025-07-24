

using Microsoft.CodeAnalysis;
using NMF.Models;
using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices.ComTypes;

namespace CTMGenerator {
    public class Utilities {

        public static AttributeData GetAttributeByName(ImmutableArray<AttributeData> attributes, string name) {
            foreach (var attribute in attributes) {
                if (attribute.AttributeClass?.Name == name) {
                    return attribute;
                }
            }
        }
    }
}
