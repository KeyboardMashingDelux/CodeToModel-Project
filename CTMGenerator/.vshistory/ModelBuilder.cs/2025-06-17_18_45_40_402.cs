using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.Collections.Immutable;
using System.Diagnostics;


namespace CTMGenerator {

    public class ModelBuilder {

        private static void CreateModel(ImmutableArray<ITypeSymbol?> elements) {
            var repository = new ModelRepository();
            var ns = new Namespace {
                Name = "GENERATED",
                Uri = new Uri("http://GENERATED.com"),
                Prefix = "FSM"
            };

            foreach (var element in elements) {
                if (element == null)
                    continue;

                var elementClass = new Class();
                elementClass.Name = element.Name.Substring(1);
                elementClass.IsAbstract = false;
                ns.Types.Add(elementClass);
            }

            repository.Save(ns, $"GENERATED.nmeta");
        }
    }

    //private static (List<string> variables, List<string> methodes, List<string> events) GetClassMembers(ImmutableArray<ISymbol> members) {
    //        List<string> variables = [];
    //        List<string> methodes = [];
    //        List<string> events = [];

    //        string visibility = "";
    //        string type = "";
    //        string memberName = "";

    //        foreach (var member in members) {
    //            switch (member) {
    //                case IPropertySymbol property:
    //                    visibility = GetAccessibility(property.DeclaredAccessibility);
    //                    type = property.Type.ToDisplayString();
    //                    memberName = property.Name;

    //                    variables.Add($"private {type} _{memberName};");
    //                    // TODO has to check accessor with property.GetMethod; property.SetMethod;
    //                    methodes.Add($"{visibility} {type} {memberName} {{ get {{ return this._{memberName}; }} }}");
    //                    break;

    //                case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
    //                    visibility = GetAccessibility(method.DeclaredAccessibility);
    //                    type = method.ReturnType.ToDisplayString();
    //                    memberName = method.Name;

    //                    methodes.Add($"{visibility} {type} {memberName} {{ }}");
    //                    break;

    //                case IEventSymbol eventMember:
    //                    visibility = GetAccessibility(eventMember.DeclaredAccessibility);
    //                    type = eventMember.Type.ToDisplayString();
    //                    memberName = eventMember.Name;

    //                    events.Add($"{visibility} event {type} {memberName};");
    //                    break;

    //                // Skip accessors (get/set/add/remove)
    //                default:
    //                    continue;
    //            }
    //        }

    //        return (variables, methodes, events);
    //    }
    }
