using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NMF.Models;
using NMF.Utilities;
using System.Collections;
using System.Collections.Immutable;
using CTMLib;

namespace CTMAnalyzer {

    /// <summary>
    /// CodeToModel <see cref="DiagnosticAnalyzer"/> implementation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CTMDiagnosticAnalyzer : DiagnosticAnalyzer {

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(
                CTMDiagnostics.InterfaceNameDescriptor, 
                CTMDiagnostics.ModelInterfaceNoModelMetadataDescriptor, 
                CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor,
                CTMDiagnostics.RequiredModelInterfaceKeyword,
                CTMDiagnostics.IListExpressionInstead,
                CTMDiagnostics.ISetExpressionInstead,
                CTMDiagnostics.IOrderedSetExpressionInstead);


        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSyntaxNodeAction(AnalyzeAssembly, SyntaxKind.AttributeList);
        }

        /// <summary>
        /// Analyzes named types and reports diagnostics.
        /// </summary>
        private static void AnalyzeNamedType(SymbolAnalysisContext context) {
            Diagnostic diagnostic;

            if (!CTMAnylzerHelper.IsModelInterface(context.Symbol)) {
                return;
            }

            INamedTypeSymbol interfaceType = (INamedTypeSymbol) context.Symbol;
            foreach (SyntaxReference declaringSyntaxReference in interfaceType.DeclaringSyntaxReferences) {
                if (declaringSyntaxReference.GetSyntax() is not InterfaceDeclarationSyntax interfaceDeclaration) {
                    continue;
                }

                // Check interface name
                string interfaceName = interfaceType.Name;
                if (!Utilities.IsValidInterfaceName(interfaceName)) {
                    diagnostic = Diagnostic.Create(
                            CTMDiagnostics.InterfaceNameDescriptor,
                            interfaceDeclaration.Identifier.GetLocation(),
                            interfaceName);
                    context.ReportDiagnostic(diagnostic);
                }

                // Check interface modifiers
                if (!(CTMAnylzerHelper.IsPartial(interfaceDeclaration) || CTMAnylzerHelper.ImplementsIModelElement(interfaceDeclaration))) {
                    diagnostic = Diagnostic.Create(
                                    CTMDiagnostics.RequiredModelInterfaceKeyword,
                                    interfaceDeclaration.Identifier.GetLocation(),
                                    interfaceName);
                    context.ReportDiagnostic(diagnostic);
                }

                // Check if a matching ModelMetatdata Assembly entry is available
                string namespaceName = interfaceType.ContainingNamespace.ToDisplayString();
                List<(string uri, string resourceName)> metadata = Utilities.GetMetadata(context.Compilation.Assembly);
                foreach ((string uri, string resourceName) in metadata) {
                    string resourceNamespace = CTMAnylzerHelper.GetNamespace(resourceName);
                    if (resourceNamespace.Equals(namespaceName)) {
                        return;
                    }
                }

                diagnostic = Diagnostic.Create(
                            CTMDiagnostics.ModelInterfaceNoModelMetadataDescriptor,
                            interfaceDeclaration.GetLocation(),
                            [namespaceName, interfaceName]);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Analyzes properties and reports diagnostics.
        /// </summary>
        private static void AnalyzeProperty(SymbolAnalysisContext context) {
            Diagnostic diagnostic;

            IPropertySymbol property = (IPropertySymbol) context.Symbol;
            Location? propertyLocation = CTMAnylzerHelper.GetPropertyLocation(property);
            if (propertyLocation == null) {
                return;
            }

            ITypeSymbol propertyType = property.Type;
            string propertyName = property.Name;
            string typeName = propertyType.Name;

            if (typeName.Equals(nameof(IList))) {
                diagnostic = Diagnostic.Create(
                                CTMDiagnostics.IListExpressionInstead,
                                propertyLocation,
                                propertyName);
                context.ReportDiagnostic(diagnostic);
            }

            if (typeName.Equals(nameof(ISet<string>))) {
                diagnostic = Diagnostic.Create(
                                CTMDiagnostics.ISetExpressionInstead,
                                propertyLocation,
                                propertyName);
                context.ReportDiagnostic(diagnostic);
            }

            if (typeName.Equals(nameof(SortedSet<string>))) {
                diagnostic = Diagnostic.Create(
                                CTMDiagnostics.IListExpressionInstead,
                                propertyLocation,
                                propertyName);
                context.ReportDiagnostic(diagnostic);
            }

        }

        /// <summary>
        /// Analyzes the assembly attribute list and reports diagnostics.
        /// </summary>
        private static void AnalyzeAssembly(SyntaxNodeAnalysisContext context) {
            HashSet<string> namespaces = CTMAnylzerHelper.GetNamespaces(context.Compilation.GlobalNamespace);
            if (namespaces.IsNullOrEmpty()) {
                return;
            }

            AttributeListSyntax attributeList = (AttributeListSyntax)context.Node;
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
                        Diagnostic diagnostic = Diagnostic.Create(
                            CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor,
                            attribute.GetLocation(),
                            resourceName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
