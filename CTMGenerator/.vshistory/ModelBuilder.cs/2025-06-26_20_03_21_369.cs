using Microsoft.CodeAnalysis;
using Microsoft.CSharp;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.CodeDom;
using System.CodeDom.Compiler;
using CTMLib;
using NMF.Utilities;
using System.Diagnostics;


namespace CTMGenerator {

    public class ModelBuilder {

        private ModelRepository? ModelRepository;
        private INamespace? Namespace;

        private string? Name, Prefix, Suffix, OutputPath;

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
            Uri namespaceURI = new(uri == null ? ModelBuilderHelper.DefaultUri : uri);
            (Name, Prefix, Suffix) = ModelBuilderHelper.GetFilenameInfo(filename);

            ModelRepository = new ModelRepository();
            Namespace = new Namespace() {
                Name = Name,
                Prefix = Prefix,
                Uri = namespaceURI,
                Parent = null,
                ParentNamespace = null,
                Remarks = null,
                Summary = null
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
            if (string.IsNullOrWhiteSpace(OutputPath)) {
                OutputPath = ModelBuilderHelper.GetSavePath(element);
            }

            var isAbstract = Utilities.GetAttributeByName(element.GetAttributes(), nameof(IsAbstract));

            var members = element.GetMembers();
            var (properties, methodes, events) = ModelBuilderHelper.GetClassMembers(members);

            Debugger.Launch();

            var (propertyReferences, propertyAttributes) = ModelBuilderHelper.ConvertProperties(properties);
            var (methodReferences, methodAttributes) = ModelBuilderHelper.ConvertMethods(methodes);
            var (eventReferences, eventAttributes) = ModelBuilderHelper.ConvertEvents(events);

            List<IReference> references = [];
            List<IAttribute> attributes = [];

            var elementAttributes = element.GetAttributes();

            var instanceOfClass = element.BaseType != null ? new Class() { Name = element.BaseType.Name } : null;

            var elementClass = new Class {
                Name = element.Name.Substring(1),
                IsAbstract = isAbstract != null,
                IdentifierScope = IdentifierScope.Local,
                Identifier = null,
                InstanceOf = instanceOfClass,
                Namespace = Namespace,
                Parent = null,
                Remarks = ModelBuilderHelper.GetFirstString(elementAttributes, nameof(Remarks)),
                Summary = ModelBuilderHelper.GetFirstString(elementAttributes, nameof(Summary))
            };

            elementClass.References.AddRange(references);
            elementClass.Attributes.AddRange(attributes);

            Namespace?.Types.Add(elementClass);
        }

        /// <summary>
        /// Saves the created model. Result should be saved to the same location as the first added element.
        /// Otherwise will be put to the root of the drive.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void DoSave() {
            if (ModelRepository == null) {
                throw new InvalidOperationException("Did you forget to call ModelBuilder's initalize()?");
            }

            ModelRepository.Save(Namespace, $"{OutputPath}/{Name}.{Suffix}");
        }

        public string DoCreateCode() {
            if (ModelRepository == null) {
                throw new InvalidOperationException();
            }

            // Creates compile unit from Namespace data (Code model - Keine Datei - Sprachunabhängig
            var compileUnit = MetaFacade.CreateCode(Namespace, ModelBuilderHelper.GetAmbientNamespaceName(Name));
            // Interfaces need to be removed!
            compileUnit = RemoveInterfaces(compileUnit);

            StringWriter writer = new();
            CodeGeneratorOptions options = new() {
                IndentString = "\t"
            };
            CSharpCodeProvider codeProvider = new();
            codeProvider.GenerateCodeFromCompileUnit(compileUnit, writer, options);
            return writer.ToString();

            // Creates actual code 
            //MetaFacade.GenerateCode(ccu, cscp, "D:\\Tools\\Microsoft Visual Studio\\Repos\\Code First Modeling\\CodeToModel\\Generated\\CTMGenerator\\", true);
            //return "";
        }

        private CodeCompileUnit RemoveInterfaces(CodeCompileUnit ccu) {
            // TODO Able to improve?
            CodeNamespaceCollection nsCollection = ccu.Namespaces;
            foreach (CodeNamespace cn in nsCollection) {
                List<CodeTypeDeclaration> interfacesToRemove = [];
                CodeTypeDeclarationCollection types = cn.Types;
                foreach (CodeTypeDeclaration type in types) {
                    if (type.IsInterface) {
                        interfacesToRemove.Add(type);
                    }
                }

                foreach (CodeTypeDeclaration typeIndex in interfacesToRemove) {
                    types.Remove(typeIndex);
                }
            }

            return ccu;
        }      
        
        public string? GetName() {
            return Name;
        }
    }
}
