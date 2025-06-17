using Microsoft.CodeAnalysis;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.Collections.Immutable;
using System.Diagnostics;


namespace CTMGenerator {

    public class ModelBuilder {

        private static readonly string defaultUri = "http://GENERATED.com";
        private static readonly string defaultFilename = "GENERATED.fsm.nmeta";


        /// <summary>
        /// Creates and saves the model.
        /// </summary>
        /// <param name="elements">Assumed to not be default or empty.</param>
        public static void CreateModel(ImmutableArray<ITypeSymbol?> elements, string? uri, string? filename) {
            Uri namespaceURI = new(uri == null ? defaultUri : uri);
            var (name, prefix, suffix) = GetFilenameInfo(filename == null ? defaultFilename : filename);

            var repository = new ModelRepository();
            var ns = new Namespace {
                Name = name,
                Uri = namespaceURI,
                Prefix = prefix
            };

            string? path = null;
            foreach (var element in elements) {
                if (element == null)
                    continue;

                if (string.IsNullOrWhiteSpace(path)) {
                    path = GetSavePath(element);
                }

                var elementClass = new Class();
                elementClass.Name = element.Name.Substring(1);
                elementClass.IsAbstract = false;
                ns.Types.Add(elementClass);

            }

            repository.Save(ns, $"{path}/{name}.{suffix}");
        }

        /// <summary>
        /// Extracts name, prefix and suffix from filename. It is assumed the filename has the following syntax: name.prefix.suffix.
        /// If the wrong filename syntax is used, uses the default filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static (string name, string prefix, string suffix) GetFilenameInfo(string filename) {
            int firstDot = filename.IndexOf('.');
            int lastDot = filename.LastIndexOf('.');

            if (firstDot == -1 || lastDot == -1 || firstDot == lastDot) { 
                return GetFilenameInfo(defaultFilename);
            }
            Debugger.Launch();
            return (filename.Substring(0, firstDot - 1), filename.Substring(firstDot - 1, lastDot - firstDot), filename.Substring(lastDot));
        }

        /// <summary>
        /// Gets the path based on the given ITypeSymbol. This will result in the path of the file. 
        /// This can be the project root or another folder inside the project.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>Directory path of which contains the file.</returns>
        private static string? GetSavePath(ITypeSymbol symbol) {
            string? path = null;
            var locations = symbol.Locations;
            foreach (var location in locations) {
                if (string.IsNullOrWhiteSpace(path)) {
                    path = location.SourceTree?.FilePath;
                }
                else { 
                    break;
                }
            }

            return Path.GetDirectoryName(path);
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
}
