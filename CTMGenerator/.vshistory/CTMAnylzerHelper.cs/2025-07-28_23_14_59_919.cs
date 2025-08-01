using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models;

namespace CTMGenerator {

    public class CTMAnylzerHelper {

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
    }
}
