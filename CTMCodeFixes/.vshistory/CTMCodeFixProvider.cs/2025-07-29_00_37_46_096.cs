using CTMGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CTMCodeFixes {

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class CTMCodeFixProvider : CodeFixProvider {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } 
            = ImmutableArray.Create(
                CTMDiagnostics.RequiredModelInterfaceKeyword.Id,
                CTMDiagnostics.IListExpressionInstead.Id,
                CTMDiagnostics.ISetExpressionInstead.Id,
                CTMDiagnostics.IOrderedSetExpressionInstead.Id
            );

        public override Task RegisterCodeFixesAsync(CodeFixContext context) {
            CodeAction action;

            foreach (var diagnostic in context.Diagnostics) {
                LocalizableString title = diagnostic.Descriptor.Title;
                string equivalanceToken = TrimAllWithInplaceCharArray(title.ToString());

                if (diagnostic.Id == CTMDiagnostics.RequiredModelInterfaceKeyword.Id) {
                    action = CodeAction.Create(title,
                                            token => AddPartialKeyword(context, diagnostic, token),
                                            equivalanceToken);
                    context.RegisterCodeFix(action, diagnostic);

                    action = CodeAction.Create(title,
                                            token => AddIModelElement(context, diagnostic, token),
                                            equivalanceToken);
                    context.RegisterCodeFix(action, diagnostic);
                }
                else {
                    action = CodeAction.Create(title,
                                                token => ReplaceType(context, diagnostic, token),
                                                equivalanceToken);
                    context.RegisterCodeFix(action, diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private static async Task<Document> AddPartialKeyword(CodeFixContext context,
            Diagnostic makePartial, CancellationToken cancellationToken) {

            var root = await context.Document.GetSyntaxRootAsync(cancellationToken);

            if (root is null)
                return context.Document;

            var classDeclaration = FindClassDeclaration(makePartial, root);

            if (classDeclaration == null)
                return context.Document;

            var partial = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
            var newDeclaration = classDeclaration.AddModifiers(partial);
            var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
            var newDoc = context.Document.WithSyntaxRoot(newRoot);

            return newDoc;
        }

        private static async Task<Document> AddIModelElement(CodeFixContext context,
            Diagnostic makePartial, CancellationToken cancellationToken) {
            return null;
        }

        private static async Task<Document> ReplaceType(CodeFixContext context,
            Diagnostic makePartial, CancellationToken cancellationToken) {
            return null;
        }

        public override FixAllProvider? GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private static InterfaceDeclarationSyntax? FindInterfaceDeclaration(Diagnostic makePartial, SyntaxNode root) {
            TextSpan diagnosticSpan = makePartial.Location.SourceSpan;

            return root.FindToken(diagnosticSpan.Start)
                       .Parent?.AncestorsAndSelf()
                       .OfType<InterfaceDeclarationSyntax>()
                       .First();
        }

        // Credits: https://stackoverflow.com/questions/6219454/efficient-way-to-remove-all-whitespace-from-string/37368176#37368176
        private static string TrimAllWithInplaceCharArray(string str) {

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
