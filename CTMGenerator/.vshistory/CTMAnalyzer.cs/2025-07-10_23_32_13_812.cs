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
                CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor);



        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            context.RegisterCompilationStartAction(PrepareAnalyzeAssembly);
        }

        private static void PrepareAnalyzeAssembly(CompilationStartAnalysisContext context) {
            HashSet<string> namespaces = GetNamespaces(context.Compilation.GlobalNamespace);

            context.RegisterSyntaxNodeAction(syntaxContext => AnalyzeAssembly(syntaxContext, namespaces), SyntaxKind.AttributeList);
        }

        private static void AnalyzeAssembly(SyntaxNodeAnalysisContext context, HashSet<string> namespaces) {
            if (namespaces.IsNullOrEmpty()) {
                return;
            }

            AttributeListSyntax attributeList = (AttributeListSyntax) context.Node;
            AttributeTargetSpecifierSyntax? target = attributeList.Target;
            if (target is null || !target.Identifier.IsKind(SyntaxKind.AssemblyKeyword)) {
                return;
            }

            foreach (var attribute in attributeList.Attributes) {
                IMethodSymbol? symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol;
                if (string.Equals(symbol?.ContainingType.Name, nameof(ModelMetadataAttribute))) {
                    continue;
                }

                ExpressionSyntax? resourceNameExpression = attribute.ArgumentList?.Arguments.ElementAt(1).Expression;
                if (resourceNameExpression == null) {
                    continue;
                }

                Optional<object?> resourceNameValue = context.SemanticModel.GetConstantValue(resourceNameExpression);
                if (resourceNameValue.HasValue && resourceNameValue.Value != null) {
                    string resourceName = resourceNameValue.Value.ToString();
                    if (!namespaces.Contains(resourceName)) {
                        var diagnostic = Diagnostic.Create(
                            CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor, 
                            attribute.GetLocation(),
                            resourceName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
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
