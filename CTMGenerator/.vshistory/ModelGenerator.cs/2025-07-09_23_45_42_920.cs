using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Expressions.Linq;
using NMF.Models;
using NMF.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace CTMGenerator {
    [Generator]
    public class ModelGenerator : IIncrementalGenerator {

        public void Initialize(IncrementalGeneratorInitializationContext context) {
            //Debugger.Launch();

            // Möglich Werte aus AdditionalFiles zu erhalten -> Anstatt einzelner RegisterSourceOutput einfach per Combine mit modelParts kombinieren
            // 
            //var pro = context.AnalyzerConfigOptionsProvider.Select((provider, ct) => 
            //        provider.GlobalOptions.TryGetValue("build_property.CompilerGeneratedFilesOutputPath", out var compilerGeneratedFilesOutputPath)
            //        ? compilerGeneratedFilesOutputPath : "NOPE");
            //context.RegisterSourceOutput(pro, (ctx, value) =>
            //{
            //    Debug.WriteLine("#####------##########" + value);
            //});

            //Debug.WriteLine("#####------##########" + CompilerGeneratedFilesOutputPath);

            var modelParts = context.SyntaxProvider.CreateSyntaxProvider(IsModelPart, GetModelParts).Where(type => type is not null).Collect();

            var compilation = context.CompilationProvider.Select((compilation, ct) => compilation);

            var fullProvider = modelParts.Combine(compilation);
            context.RegisterSourceOutput(fullProvider, action: GenerateCode);
        }

        private static bool IsModelPart(SyntaxNode syntaxNode, CancellationToken cancellationToken) {
            if (syntaxNode is not AttributeSyntax attribute)
                return false;

            var name = ExtractName(attribute.Name);

            if (name != nameof(ModelInterface))
                return false;

            Debugger.Launch();
            Debug.WriteLine($"### {name} ###");


            return attribute.Parent?.Parent is InterfaceDeclarationSyntax;
        }

        /// <summary>
        /// Tries to extracts the name from a NameSyntax node.
        /// </summary>
        private static string? ExtractName(NameSyntax? name) {
            return name switch {
                SimpleNameSyntax ins => ins.Identifier.Text,
                QualifiedNameSyntax qns => qns.Right.Identifier.Text,
                AliasQualifiedNameSyntax aqns => aqns.Name.Identifier.Text,
                _ => name?.ToString()
            };
        }

        private static ITypeSymbol? GetModelParts(GeneratorSyntaxContext context, CancellationToken cancellationToken) {
            var attributeSyntax = (AttributeSyntax)context.Node;

            // "attribute.Parent" is "AttributeListSyntax"
            // "attribute.Parent.Parent" is a C# fragment the attributes are applied to
            if (attributeSyntax.Parent?.Parent is not InterfaceDeclarationSyntax interfaceDeclaration)
                return null;

            // This is now the actual Symbol and not the node (as seen in the Syntax Visualiser)
            var type = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration) as ITypeSymbol;

            Debug.WriteLine($"+++ {type} +++");

            return type is null || !IsFromCorrectLib(type) ? null : type;
        }

        private static bool IsFromCorrectLib(ISymbol type) {
            return type.GetAttributes()
                       .Any(a => Utilities.IsLibAttributeClass(a.AttributeClass, nameof(ModelInterface)));
        }

        private static void GenerateCode(SourceProductionContext context,
                (ImmutableArray<ITypeSymbol?> elements, Compilation compilation) providerData) {
            if (providerData.elements.IsDefaultOrEmpty)
                return;

            List<(string uri, string filename)> metadata = GetMetadata(providerData.compilation.Assembly);
            Dictionary<string, ModelBuilder> models = [];
            foreach (var (uri, filename) in metadata) {
                ModelBuilder mb = new(uri, filename, providerData.compilation);
                models.Add(mb.GetFullName(), mb);
            }


            foreach (var element in providerData.elements) {
                if (element == null)
                    continue;

                // TODO Warum "is not null here"?
                string ns = element.ContainingNamespace.OriginalDefinition.Name;

                // Ignore elements which don't have a matching assembly set namespace
                if (!models.ContainsKey(ns)) {
                    continue;
                }

                models[ns].AddElement(element);
            }

            foreach (var model in models.Values) {
                model.CreateReferences();
                model.DoSave();
                context.AddSource($"{model.GetName()}.g.cs", model.DoCreateCode());
            }
        }

//        private static void GenerateModel(SourceProductionContext context,
//            (ImmutableArray<ITypeSymbol?> elements, IAssemblySymbol assembly) providerData) {
//            if (providerData.elements.IsDefaultOrEmpty)
//                return;

//            // Get all of the assembly options
//            // For each namespace create new builder 
//            // Add classes only to namespace which matches filename defined namespace
//            var (uri, filename) = GetMetadata(providerData.assembly);
//            ModelBuilder mb = new();
//            mb.Initalize(uri, filename);

//            foreach (var element in providerData.elements) {
//                if (element == null)
//                    continue;

//                // Check for namespace of element
//                mb.AddElement(element);

//                var ns = element.ContainingNamespace.IsGlobalNamespace
//                          ? null
//                          : element.ContainingNamespace.ToString();
//                var name = element.Name;
//                var className = name.Substring(1);
//                var inheritance = GetInheritance(element);

//                var members = element.GetMembers();
//                var (variables, methodes, events) = GetClassMembers(members);


//                var code = @$"// <auto-generated />

//{(ns is null ? null : $@"namespace {ns} {{")}
//   public partial class {className} : {(string.IsNullOrWhiteSpace(inheritance) ? null : inheritance + ",")} {name} {{

//        {String.Join("\n\t", variables)}

//        {String.Join("\n\t", methodes)}

//        {String.Join("\n\t", events)}
//      }}
//{(ns is null ? null : @"}
//")}";
//                ;

//                context.AddSource($"{className}.g.cs", code);
//            }

//            mb.DoSave();
//            //context.AddSource($"{mb.name}.g.cs", mb.DoCreateCode());
//        }

        /// <summary>
        /// Creates a list of all ModelMetadataAttribute attribute data. 
        /// Should a ModelMetadataAttribute attribute hold null references it will be ignored.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static List<(string uri, string filename)> GetMetadata(IAssemblySymbol assembly) {
            if (assembly == null)
                return [];

            List<AttributeData> metadataAttributes = Utilities.GetAttributesByName(assembly.GetAttributes(), nameof(ModelMetadataAttribute));
            if (metadataAttributes.IsNullOrEmpty()) {
                return [];
            }

            List<(string uri, string filename)> metadata = []; 
            foreach (var attribute in metadataAttributes) {
                var ca = attribute.ConstructorArguments;
                string? uri = ca[0].Value?.ToString();
                string? filename = ca[1].Value?.ToString();

                // Ignore null values
                if (uri == null || filename == null) {
                    continue;
                }

                metadata.Add((uri, filename));
            }

            return metadata;
        }

        private static string GetInheritance(ITypeSymbol element) {
            var interfaces = element.Interfaces;
            List<string> inheritance = [];
            foreach (var interfaceSymbol in interfaces) {
                var interfaceAttributes = interfaceSymbol.GetAttributes();
                if (interfaceAttributes.Any(a => Utilities.IsLibAttributeClass(a.AttributeClass, nameof(ModelInterface)))) {
                    inheritance.Add(interfaceSymbol.Name.Substring(1));
                }
            }

            return String.Join(",", inheritance);
        }

        private static (List<string> variables, List<string> methodes, List<string> events) GetClassMembers(ImmutableArray<ISymbol> members) {
            List<string> variables = [];
            List<string> methodes = [];
            List<string> events = [];

            string visibility = "";
            string type = "";
            string memberName = "";

            foreach (var member in members) {
                switch (member) {
                    case IPropertySymbol property:
                        visibility = GetAccessibility(property.DeclaredAccessibility);
                        type = property.Type.ToDisplayString();
                        memberName = property.Name;

                        variables.Add($"private {type} _{memberName};");
                        // TODO has to check accessor with property.GetMethod; property.SetMethod;
                        methodes.Add($"{visibility} {type} {memberName} {{ get {{ return this._{memberName}; }} }}");
                        break;

                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                        visibility = GetAccessibility(method.DeclaredAccessibility);
                        type = method.ReturnType.ToDisplayString();
                        memberName = method.Name;

                        methodes.Add($"{visibility} {type} {memberName} {{ }}");
                        break;

                    case IEventSymbol eventMember:
                        visibility = GetAccessibility(eventMember.DeclaredAccessibility);
                        type = eventMember.Type.ToDisplayString();
                        memberName = eventMember.Name;

                        events.Add($"{visibility} event {type} {memberName};");
                        break;

                    // Skip accessors (get/set/add/remove)
                    default:
                        continue;
                }
            }

            return (variables, methodes, events);
        }

        private static string GetAccessibility(Accessibility accessibility) {
            return accessibility == Accessibility.NotApplicable ? "" : accessibility.ToString().ToLower();
        }
    }
}