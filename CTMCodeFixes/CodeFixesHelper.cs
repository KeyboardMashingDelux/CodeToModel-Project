using CTMAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CTMCodeFixes {

    public class CodeFixesHelper {

        private static readonly TypeSyntax typeIListExpression = SyntaxFactory.ParseName("NMF.Expressions.IListExpression");

        private static readonly TypeSyntax typeISetExpression = SyntaxFactory.ParseName("NMF.Expressions.ISetExpression");

        private static readonly TypeSyntax typeIOrderedSetExpression = SyntaxFactory.ParseName("NMF.Collections.Generic.IOrderedSetExpression");

        public static InterfaceDeclarationSyntax? FindInterfaceDeclaration(Diagnostic makePartial, SyntaxNode root) {
            TextSpan diagnosticSpan = makePartial.Location.SourceSpan;

            return root.FindToken(diagnosticSpan.Start)
                       .Parent?.AncestorsAndSelf()
                       .OfType<InterfaceDeclarationSyntax>()
                       .First();
        }

        public static PropertyDeclarationSyntax? FindPropertyDeclaration(Diagnostic makePartial, SyntaxNode root) {
            TextSpan diagnosticSpan = makePartial.Location.SourceSpan;

            return root.FindToken(diagnosticSpan.Start)
                       .Parent?.AncestorsAndSelf()
                       .OfType<PropertyDeclarationSyntax>()
                       .First();
        }

        public static TypeSyntax GetReplaceType(Diagnostic diagnostic) {
            if (diagnostic.Id.Equals(CTMDiagnostics.ISetExpressionInstead.Id)) {
                return typeISetExpression;
            }
            else if (diagnostic.Id.Equals(CTMDiagnostics.IOrderedSetExpressionInstead.Id)) {
                return typeIOrderedSetExpression;
            }
            else { // Assume IListExpression is valid 
                return typeIListExpression;
            }
        }

        // Credits: https://stackoverflow.com/questions/6219454/efficient-way-to-remove-all-whitespace-from-string/37368176#37368176
        public static string TrimAllWithInplaceCharArray(string str) {

            var len = str.Length;
            var src = str.ToCharArray();
            int dstIdx = 0;

            for (int i = 0; i < len; i++) {
                var ch = src[i];

                switch (ch) {
                    case '\u0020':
                    case '\u00A0':
                    case '\u1680':
                    case '\u2000':
                    case '\u2001':
                    case '\u2002':
                    case '\u2003':
                    case '\u2004':
                    case '\u2005':
                    case '\u2006':
                    case '\u2007':
                    case '\u2008':
                    case '\u2009':
                    case '\u200A':
                    case '\u202F':
                    case '\u205F':
                    case '\u3000':
                    case '\u2028':
                    case '\u2029':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                        continue;

                    default:
                        src[dstIdx++] = ch;
                        break;
                }
            }
            return new string(src, 0, dstIdx);
        }
    }
}
