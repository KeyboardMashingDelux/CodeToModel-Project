using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models;
using NMF.Utilities;
using System.Collections.Immutable;

namespace CTMAnalyzer {

    public class CTMAnylzerHelper {

        public static HashSet<string> GetNamespaces(INamespaceSymbol ns) {
            HashSet<string> namespaces = [];
            if (!string.IsNullOrEmpty(ns.Name))
                namespaces.Add(ns.ToDisplayString());

            foreach (var nested in ns.GetNamespaceMembers()) {
                namespaces.AddRange(GetNamespaces(nested));
            }

            return namespaces;
        }

        public static bool IsModelInterface(ISymbol type) {
            return Utilities.GetAttributeByName(type.GetAttributes(), nameof(ModelInterface)) != null;
        }

        public static bool IsPartial(InterfaceDeclarationSyntax interfaceDeclaration) {
            return interfaceDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        public static bool ImplementsIModelElement(InterfaceDeclarationSyntax interfaceDeclaration) {
            BaseListSyntax? baseList = interfaceDeclaration.BaseList;
            return baseList != null && baseList.Types.Any(t => t.ToString().Contains(nameof(IModelElement)));
        }

        public static bool IsValidInterfaceName(string interfaceName) {
            return interfaceName.StartsWith("I") && interfaceName.Length >= 2 && char.IsUpper(interfaceName[1]);
        }

        public static Location? GetPropertyLocation(IPropertySymbol property) {
            ImmutableArray<Location> fieldLocations = property.Locations;
            Location? location = fieldLocations.FirstOrDefault();

            // In case the first location is invalid
            int index = 1;
            while (location == null || !location.IsInSource) {
                if (index >= fieldLocations.Length) {
                    return null;
                }

                location = fieldLocations[index];
                index++;
            }

            return location;
        }
    }
}
