using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using NMF.Utilities;
using System.Collections.Immutable;
using System.Runtime.InteropServices.ComTypes;

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

        /// <summary>
        /// Gets the full namespace name.
        /// </summary>
        public static string GetNamespace(string resourceName) {
            string[] parts = resourceName.Split('.');

            if (parts.Length < 3) {
                return resourceName;
            }

            return string.Join(".", parts.Take(parts.Length - 2));
        }

        /// <summary>
        /// Checks if the given list of <see cref="INamedTypeSymbol"/> has a member by name.
        /// </summary>
        /// <returns><see langword="true"/> if a member of this name was found, otherwise <see langword="false"/>./returns>
        public static bool HasMember(ImmutableArray<INamedTypeSymbol> members, string memberName) {
            foreach (INamedTypeSymbol member in members) {
                if (member.Name == memberName) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsCollection(ITypeSymbol type) {
            string typeName = type.Name;
            return typeName.Equals(nameof(IListExpression<int>))
                || typeName.Equals(nameof(ISetExpression<int>))
                || typeName.Equals(nameof(IOrderedSetExpression<int>))
                || typeName.Equals(nameof(ICollectionExpression<int>));
        }
    }
}
