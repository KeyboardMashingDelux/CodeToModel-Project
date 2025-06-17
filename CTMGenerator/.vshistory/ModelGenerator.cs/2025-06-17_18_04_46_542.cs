using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.Collections.Immutable;
using System.Diagnostics;

namespace CTMGenerator {
    [Generator]
    public class ModelGenerator : IIncrementalGenerator {


        private const string AttributesLibName = "CTMLib";

        public void Initialize(IncrementalGeneratorInitializationContext context) {
            var modelParts = context.SyntaxProvider.CreateSyntaxProvider(IsModelPart, GetModelParts).Where(type => type is not null).Collect();

            context.RegisterSourceOutput(modelParts, action: GenerateModel);
        }

        private static bool IsModelPart(SyntaxNode syntaxNode, CancellationToken cancellationToken) {
            if (syntaxNode is not AttributeSyntax attribute)
                return false;

            var name = ExtractName(attribute.Name);

            if (name != GeneratorResources.ModelInterafaceAttributeName)
                return false;

            //Debugger.Launch();
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
            foreach (var element in type.GetAttributes())
                Debug.WriteLine($"??? {element} <-> {element.AttributeClass?.Name} <-> {element.AttributeClass?.ContainingNamespace} ???");

            return type.GetAttributes()
                       .Any(a => a.AttributeClass?.Name == GeneratorResources.ModelInterafaceAttributeName &&
                                 a.AttributeClass.ContainingNamespace is {
                                     Name: AttributesLibName,
                                     ContainingNamespace.IsGlobalNamespace: true
                                 }
                       );
        }

        private static void GenerateModel(SourceProductionContext context, ImmutableArray<ITypeSymbol?> elements) {
            if (elements.IsDefaultOrEmpty)
                return;

            Debug.WriteLine($"=== {elements} ===");

            foreach (var element in elements) {
                if (element == null)
                    continue;

                var ns = element.ContainingNamespace.IsGlobalNamespace
                          ? null
                          : element.ContainingNamespace.ToString();
                var name = element.Name;
                var className = name.Substring(1);

                var members = element.GetMembers().OfType<IPropertySymbol>();
                var classVars = "";
                var classMembers = "";
                foreach (var member in members) { 
                    string visibility = "";
                    string type = "";
                    string memberName = "";

                    switch (member) {
                        case IMethodSymbol methodMember:
                            visibility = GetAccessibility(methodMember.DeclaredAccessibility);
                            type = methodMember.ReturnType.ToString();
                            memberName = methodMember.Name;
                            break;
                        case IPropertySymbol propertyMember:
                            visibility = GetAccessibility(propertyMember.DeclaredAccessibility);
                            type = propertyMember.Type.ToString();
                            memberName = "_" + propertyMember.Name;

                            classVars += $"{visibility} {type} {memberName};\n";
                            classMembers += $"{visibility} {type} {memberName} {{ get; }}\n";
                            propertyMember.
                            break;
                        default:
                            continue;
                    }
                    
                }

                var code = @$"// <auto-generated />

{(ns is null ? null : $@"namespace {ns} {{")}
   public partial class {className} : {name} {{

        {classVars}

        {classMembers}
      }}
{(ns is null ? null : @"}
")}";
                ;

                context.AddSource($"{className}.g.cs", code);
            }

            CreateModel(elements);
        }

        private static (List<string>, List<string>) GetClassMembers(ImmutableArray<ISymbol> members) {
            List<string> variables = [];
            List<string> methodes = [];

            foreach (var member in members) {
                switch (member) {
                    case IPropertySymbol property:
                        break;

                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                        break;

                    case IEventSymbol evt:
                        break;

                    // Skip accessors (get/set/add/remove)
                    default: continue;
                }
            }

                return (variables, methodes);
        }

        private static string GetAccessibility(Accessibility accessibility) {
            return accessibility == Accessibility.NotApplicable ? "" : accessibility.ToString();
        }

        private static void CreateModel(ImmutableArray<ITypeSymbol?> elements) {
            if (!elements.IsDefaultOrEmpty)
                return;

            var repository = new ModelRepository();
            var ns = new Namespace();
            ns.Name = "GENERATED";
            foreach (var element in elements) {
                if (element == null)
                    continue;

                var elementClass = new Class();
                elementClass.Name = element.Name;
                ns.Types.Add(elementClass);
            }

            repository.Save(ns, $"GENERATED.nmeta");
        }
    }
}