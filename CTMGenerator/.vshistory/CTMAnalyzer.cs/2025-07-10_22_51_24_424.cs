using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NMF.Models;
using NMF.Utilities;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;

namespace CTMGenerator {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CTMAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(
                CTMDiagnostics.InterfaceNameDescriptor, 
                CTMDiagnostics.ModelInterfaceNoModelMetadataDescriptor, 
                CTMDiagnostics.AssemblyMetadataNoNamespaceMetadataNo);



        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            //context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            context.RegisterCompilationStartAction(PrepareAnalyzeAssembly);
        }

        private static void PrepareAnalyzeAssembly(CompilationStartAnalysisContext context) {
            HashSet<string> namespaces = GetNamespaces(context.Compilation.GlobalNamespace);

            Debugger.Launch();

            context.RegisterSyntaxNodeAction(syntaxContext => AnalyzeAssembly(syntaxContext, namespaces), SyntaxKind.AttributeList);
        }

        private static void AnalyzeAssembly(SyntaxNodeAnalysisContext context, HashSet<string> namespaces) {
            var assembly = context.Compilation.Assembly;

            Debugger.Launch();


            if (namespaces.IsNullOrEmpty()) {
                return;
            }

            //List<AttributeData> metadataAttributes = Utilities.GetAttributesByName(assembly.GetAttributes(), nameof(ModelMetadataAttribute));
            //foreach (var attribute in metadataAttributes) {
            //    string? resourceName = attribute.ConstructorArguments.ElementAtOrDefault(1).ToString();
            //    if (string.IsNullOrEmpty(resourceName)) {
            //        continue;
            //    }

            //    if (titleArg.Value?.ToString() == "BadTitle") {
            //            // Report diagnostic
            //            var diagnostic = Diagnostic.Create(Rule, Location.None, "Assembly title should not be 'BadTitle'");
            //            compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
            //            {
            //                compilationEndContext.ReportDiagnostic(diagnostic);
            //            });
            //        }
                
            //}
        }

        private static HashSet<string> GetNamespaces(INamespaceSymbol ns) {
            HashSet<string> namespaces = [];
            if (!string.IsNullOrEmpty(ns.Name))
                namespaces.Add(ns.ToDisplayString());

            foreach (var nested in ns.GetNamespaceMembers()) {
                namespaces.AddRange(GetNamespaces(nested));
            }

            return namespaces;
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context) {
            //if (!MultiUseFunctions.IsEnumeration(context.Symbol))
            //    return;

            //var type = (INamedTypeSymbol)context.Symbol;

            //foreach (var declaringSyntaxReference in type.DeclaringSyntaxReferences) {
            //    if (declaringSyntaxReference.GetSyntax() is not ClassDeclarationSyntax classDeclaration
            //        || MultiUseFunctions.isPartial(classDeclaration)) {
            //        continue;
            //    }

            //    var error = Diagnostic.Create(DemoDiagnosticsDescriptors.EnumerationMustBePartial,
            //        classDeclaration.Identifier.GetLocation(), type.Name);

            //    Debug.WriteLine("#################");

            //    context.ReportDiagnostic(error);
            //}
        }
    }
}
