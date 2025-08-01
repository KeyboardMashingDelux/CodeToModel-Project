using CTMGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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
                var title = diagnostic.Descriptor.Title;

                if (diagnostic.Id == CTMDiagnostics.RequiredModelInterfaceKeyword.Id) {
                    action = CodeAction.Create(title,
                                            token => AddPartialKeyword(context, diagnostic, token));
                    context.RegisterCodeFix(action, diagnostic);

                    action = CodeAction.Create(title,
                                            token => AddIModelElement(context, diagnostic, token),
                                            title);
                    context.RegisterCodeFix(action, diagnostic);
                }
                else {
                    action = CodeAction.Create(title,
                                                token => ReplaceType(context, diagnostic, token),
                                                title);
                    context.RegisterCodeFix(action, diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        public override FixAllProvider? GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
