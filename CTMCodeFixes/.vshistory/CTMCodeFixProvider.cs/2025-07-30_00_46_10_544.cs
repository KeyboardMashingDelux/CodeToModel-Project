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

            //foreach (var diagnostic in context.Diagnostics) {
            //    LocalizableString title = diagnostic.Descriptor.Title;
            //    string equivalanceToken = CodeFixesHelper.TrimAllWithInplaceCharArray(title.ToString());

            //    if (diagnostic.Id == CTMDiagnostics.RequiredModelInterfaceKeyword.Id) {
            //        action = CodeAction.Create(title,
            //                                token => AddPartialKeyword(context, diagnostic, token),
            //                                equivalanceToken);
            //        context.RegisterCodeFix(action, diagnostic);

            //        action = CodeAction.Create(title,
            //                                token => AddIModelElement(context, diagnostic, token),
            //                                equivalanceToken);
            //        context.RegisterCodeFix(action, diagnostic);
            //    }
            //    else {
            //        action = CodeAction.Create(title,
            //                                    token => ReplaceType(context, diagnostic, token),
            //                                    equivalanceToken);
            //        context.RegisterCodeFix(action, diagnostic);
            //    }
            //}

            //return Task.CompletedTask;
        }

        private static async Task<Document> AddPartialKeyword(CodeFixContext context,
            Diagnostic diagnostic, CancellationToken cancellationToken) {

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

            if (root is null)
                return context.Document;

            InterfaceDeclarationSyntax? interfaceDeclaration = CodeFixesHelper.FindInterfaceDeclaration(diagnostic, root);

            if (interfaceDeclaration == null)
                return context.Document;

            SyntaxToken partial = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
            InterfaceDeclarationSyntax newDeclaration = interfaceDeclaration.AddModifiers(partial);
            SyntaxNode newRoot = root.ReplaceNode(interfaceDeclaration, newDeclaration);
            Document newDoc = context.Document.WithSyntaxRoot(newRoot);

            return newDoc;
        }

        private static async Task<Document> AddIModelElement(CodeFixContext context,
            Diagnostic diagnostic, CancellationToken cancellationToken) {

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

            if (root is null)
                return context.Document;

            InterfaceDeclarationSyntax? interfaceDeclaration = CodeFixesHelper.FindInterfaceDeclaration(diagnostic, root);

            if (interfaceDeclaration == null)
                return context.Document;

            NameSyntax baseTypeName = SyntaxFactory.ParseName("NMF.Model.IModelElement");
            SimpleBaseTypeSyntax newBaseType = SyntaxFactory.SimpleBaseType(baseTypeName);
            InterfaceDeclarationSyntax newDeclaration = interfaceDeclaration.AddBaseListTypes(newBaseType);
            SyntaxNode newRoot = root.ReplaceNode(interfaceDeclaration, newDeclaration);
            Document newDoc = context.Document.WithSyntaxRoot(newRoot);

            return newDoc;
        }

        private static async Task<Document> ReplaceType(CodeFixContext context,
            Diagnostic diagnostic, CancellationToken cancellationToken) {

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(cancellationToken);

            if (root is null)
                return context.Document;

            PropertyDeclarationSyntax? propertyDeclaration = CodeFixesHelper.FindPropertyDeclaration(diagnostic, root);

            if (propertyDeclaration == null)
                return context.Document;

            
            PropertyDeclarationSyntax newDeclaration = propertyDeclaration.WithType(CodeFixesHelper.GetReplaceType(diagnostic));
            SyntaxNode newRoot = root.ReplaceNode(propertyDeclaration, newDeclaration);
            Document newDoc = context.Document.WithSyntaxRoot(newRoot);

            return newDoc;
        }

        public override FixAllProvider? GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
