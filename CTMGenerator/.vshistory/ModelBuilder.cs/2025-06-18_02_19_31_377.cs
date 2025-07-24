using Microsoft.CodeAnalysis;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.Collections.Immutable;
using System.Diagnostics;


namespace CTMGenerator {

    public class ModelBuilder {

        private const string defaultUri = "http://GENERATED.com";
        private const string defaultFilename = "GENERATED.fsm.nmeta";

        private ModelRepository? ModelRepository;
        private INamespace? Namespace;

        private string? name, prefix, suffix, path;

        public ModelBuilder() {
        
        }

        /// <summary>
        /// Creates a <typeparamref name="ModelRepository"/> and <typeparamref name="Namespace"/>. 
        /// <para/>
        /// This needs to be the first methode called!
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="filename"></param>
        public void Initalize(string? uri, string? filename) {
            Uri namespaceURI = new(uri == null ? defaultUri : uri);
            (name, prefix, suffix) = GetFilenameInfo(filename == null ? defaultFilename : filename);

            ModelRepository = new ModelRepository();
            Namespace = new Namespace() {
                Name = name,
                Prefix = prefix,
                Uri = namespaceURI,
            };
        }

        public void AddElement(ITypeSymbol element) {
            switch (element.TypeKind) {
                case TypeKind.Interface:
                    AddClass(element);
                    break;
                case TypeKind.Array:
                    break;
                default: return;
            }
        }

        private void AddClass(ITypeSymbol element) {
            if (string.IsNullOrWhiteSpace(path)) {
                path = GetSavePath(element);
            }

            var elementClass = new Class {
                Name = element.Name.Substring(1),
                // TODO Depend abstract state on attribute
                IsAbstract = element.TypeKind == TypeKind.Interface ? false : element.IsAbstract
            };
            Namespace?.Types.Add(elementClass);
        }

        public void DoSave() {
            if (ModelRepository == null) {
                throw new InvalidOperationException();
            }

            ModelRepository.Save(Namespace, $"{path}/{name}.{suffix}");
        }

        /// <summary>
        /// Extracts name, prefix and suffix from filename. It is assumed the filename has the following syntax: name.prefix.suffix.
        /// If the wrong filename syntax is used, uses the default filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private (string name, string prefix, string suffix) GetFilenameInfo(string filename) {
            int firstDot = filename.IndexOf('.');
            int lastDot = filename.LastIndexOf('.');

            if (firstDot == -1 || lastDot == -1 || firstDot == lastDot) { 
                return GetFilenameInfo(defaultFilename);
            }
            
            return (filename.Substring(0, firstDot), filename.Substring(firstDot + 1, lastDot - firstDot - 1), filename.Substring(lastDot + 1));
        }

        /// <summary>
        /// Gets the path based on the given ITypeSymbol. This will result in the path of the file. 
        /// This can be the project root or another folder inside the project.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>Directory path of which contains the file.</returns>
        private string? GetSavePath(ITypeSymbol symbol) {
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
    }
}
