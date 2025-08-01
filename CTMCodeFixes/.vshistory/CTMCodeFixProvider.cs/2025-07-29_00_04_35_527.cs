using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;

namespace CTMCodeFixes {

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class CTMCodeFixProvider : CodeFixProvider {
        public override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

        public override Task RegisterCodeFixesAsync(CodeFixContext context) {
            throw new NotImplementedException();
        }

        public override FixAllProvider? GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
