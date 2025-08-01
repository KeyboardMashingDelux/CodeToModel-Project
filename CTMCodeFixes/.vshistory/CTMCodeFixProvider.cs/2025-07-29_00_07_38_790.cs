using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using CTMGenerator;

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
            throw new NotImplementedException();
        }

        public override FixAllProvider? GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
