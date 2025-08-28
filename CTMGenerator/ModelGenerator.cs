using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Expressions.Linq;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;

namespace CTMGenerator {

    /// <summary>
    /// CodeToModel implemenation of a <see cref="IIncrementalGenerator"/>.
    /// </summary>
    [Generator]
    public class ModelGenerator : IIncrementalGenerator {

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            var modelParts = context.SyntaxProvider.CreateSyntaxProvider(IsModelPart, GetModelParts).Where(type => type is not null).Collect();

            var compilation = context.CompilationProvider.Select((compilation, ct) => compilation);

            var outputPaths = context.AdditionalTextsProvider
                           .Where(text => text.Path.EndsWith("OutputPaths.xml", StringComparison.OrdinalIgnoreCase))
                           .Select((text, token) => SafeParseXML(text.GetText(token)?.ToString()))
                           .Where(text => text is not null)!
                           .Collect<XDocument>();

            var fullProvider = modelParts.Combine(compilation).Combine(outputPaths);
            context.RegisterSourceOutput(fullProvider, action: GenerateCode);
        }

        private static XDocument? SafeParseXML(string? xml) {
            if (string.IsNullOrWhiteSpace(xml)) {
                return null;
            }

            return XDocument.Parse(xml);
        }

        private static bool IsModelPart(SyntaxNode syntaxNode, CancellationToken cancellationToken) {
            if (syntaxNode is not AttributeSyntax attribute) {
                return false;
            }

            string name = Utilities.ExtractName(attribute.Name);

            if (name is not nameof(ModelInterface) and not nameof(ModelEnum)) {
                return false;
            }

            return attribute.Parent?.Parent is InterfaceDeclarationSyntax or EnumDeclarationSyntax;
        }

        private static ITypeSymbol? GetModelParts(GeneratorSyntaxContext context, CancellationToken cancellationToken) {
            AttributeSyntax attributeSyntax = (AttributeSyntax)context.Node;

            if (attributeSyntax.Parent?.Parent is not InterfaceDeclarationSyntax and not EnumDeclarationSyntax) {
                return null;
            }

            ITypeSymbol? type = context.SemanticModel.GetDeclaredSymbol(attributeSyntax.Parent.Parent) as ITypeSymbol;

            return type is null || !IsFromCorrectLib(type) ? null : type;
        }

        private static bool IsFromCorrectLib(ISymbol type) {
            return type.GetAttributes()
                       .Any(a => Utilities.IsLibAttributeClass(a.AttributeClass, nameof(ModelInterface))
                                 || Utilities.IsLibAttributeClass(a.AttributeClass, nameof(ModelEnum)));
        }

        private static void GenerateCode(SourceProductionContext context,
                ((ImmutableArray<ITypeSymbol?> elements, Compilation compilation) builderParts,
                     ImmutableArray<XDocument> outputPaths) providerData) {
            var (elements, compilation) = providerData.builderParts;
            if (elements.IsDefaultOrEmpty) { 
                return;
            }

            List<(string uri, string filename)> metadata = Utilities.GetMetadata(compilation.Assembly);
            Dictionary<string, ModelBuilder> models = [];
            foreach (var (uri, filename) in metadata) {
                ModelBuilder mb = new(uri, filename, compilation);
                mb.SetOutputPath(GetOutputPathForNamespace(providerData.outputPaths, mb.GetFullName()));
                models.Add(mb.GetFullName(), mb);
            }


            foreach (INamedTypeSymbol element in elements.OfType<INamedTypeSymbol>()) {
                if (element == null) {
                    continue;
                }

                string ns = element.ContainingNamespace.ToString();

                // Ignore elements which don't have a matching assembly set namespace
                if (!models.ContainsKey(ns)) {
                    continue;
                }

                models[ns].AddElement(element);
            }

            foreach (ModelBuilder model in models.Values) {
                model.CreateModel();
                model.DoSave();
                context.AddSource($"{model.GetName()}.g.cs", model.DoCreateCode());
            }
        }

        private static string? GetOutputPathForNamespace(ImmutableArray<XDocument> outputPaths, string ns) {
            foreach (var outputPath in outputPaths) {
                foreach(XElement element in outputPath.Root.Elements()) {
                    if (element.Name.LocalName.Equals("path", StringComparison.OrdinalIgnoreCase)) {
                        string elementNamespace = element.Attribute("namespace").Value;
                        if (elementNamespace.Equals(ns) || elementNamespace.Equals("ALL", StringComparison.OrdinalIgnoreCase)) {
                            return element.Value.Trim();
                        }
                    }
                }
            }

            return null;
        }
    }
}