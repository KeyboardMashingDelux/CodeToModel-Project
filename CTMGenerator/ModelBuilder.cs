using Microsoft.CodeAnalysis;
using Microsoft.CSharp;
using NMF.Models.Meta;
using NMF.Models.Repository;
using System.CodeDom;
using System.CodeDom.Compiler;
using CTMLib;
using NMF.Utilities;
using System.Diagnostics;
using NMF.Expressions.Linq;
using NMF.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;


namespace CTMGenerator {

    public class ModelBuilder {

        private ModelRepository ModelRepository;
        private INamespace Namespace;

        private string FullName, Name, AmbientName, Prefix, Suffix;
        private string? OutputPath;

        private Compilation GeneratorCompilation;

        /// Key = Ref Class name, Value Type to add reference to
        private Dictionary<IReference, string> RefTypeInfos;

        private IDictionary<string, INamedTypeSymbol> NamespaceSymbols;

        public ModelBuilder(string? uri, string? filename, Compilation compilation) {
            GeneratorCompilation = compilation;
            Uri namespaceURI = new(uri ?? ModelBuilderHelper.DefaultUri);
            (FullName, Name, AmbientName, Prefix, Suffix) = ModelBuilderHelper.GetFilenameInfo(filename);

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

            RefTypeInfos = [];
            NamespaceSymbols = new Dictionary<string, INamedTypeSymbol>();
        }

        public void AddElement(ITypeSymbol element) {
            switch (element.TypeKind) {
                case TypeKind.Interface:
                    AddClass(element);
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

            //Debugger.Launch();

            var (propertyReferences, propertyAttributes, refTypeInfos) = ModelBuilderHelper.ConvertProperties(properties, GeneratorCompilation);
            RefTypeInfos.AddRange(refTypeInfos);
            var (methodReferences, methodAttributes) = ModelBuilderHelper.ConvertMethods(methodes);
            var (eventReferences, eventAttributes) = ModelBuilderHelper.ConvertEvents(events);

            List<IReference> references = [];
            references.AddRange(propertyReferences);
            List<IAttribute> attributes = [];
            attributes.AddRange(propertyAttributes);  

            var elementAttributes = element.GetAttributes();

            var instanceOfClass = element.BaseType != null ? new Class() { Name = element.BaseType.Name } : null;

            IdentifierScope? identifierScope = ModelBuilderHelper.GetIdentifierScope(elementAttributes);

            var elementClass = new Class {
                Name = element.Name.Substring(1),
                IsAbstract = isAbstract != null,
                IdentifierScope = identifierScope != null ? (IdentifierScope) identifierScope : IdentifierScope.Local, // TODO Standardwert?
                Identifier = null, // Attribut welches IdentifierScope beinhaltet TODO Wie bestimmen?
                InstanceOf = instanceOfClass,
                Namespace = Namespace,
                Parent = null,
                Remarks = ModelBuilderHelper.GetFirstString(elementAttributes, nameof(Remarks)),
                Summary = ModelBuilderHelper.GetFirstString(elementAttributes, nameof(Summary))
            };

            elementClass.References.AddRange(references);
            elementClass.Attributes.AddRange(attributes);

            Namespace.Types.Add(elementClass);
            // Since elemtn represents an interface it is guaranteed to be an INamedTypeSymbol
            NamespaceSymbols.Add(element.Name, (INamedTypeSymbol) element);
        }

        /// <summary>
        /// Creates all non-generic references of the model. 
        /// This should be called before a call to DoSave() or DoCreateCode().
        /// </summary>
        public void CreateReferences() {
            IList<IReference> refToRemove = [];
            foreach (var refInfo in RefTypeInfos) {
                string refName = refInfo.Value;
                IReference refAddType = refInfo.Key;

                var possibleRefType = Namespace.Types.Where((type) => type.Name.Equals(refName));
                if (possibleRefType == null || possibleRefType.Count() != 1) {
                    continue;
                }

                IType refType = possibleRefType.First();
                if (refType is not null and IReferenceType) {
                    refAddType.ReferenceType = (IReferenceType) refType;
                    // Remove all successfull references to avoid duplicate reference creation
                    refToRemove.Add(refAddType);
                }
            }

            foreach (var toRemove in refToRemove) {
                RefTypeInfos.Remove(toRemove);
            }
        }

        /// <summary>
        /// Saves the created model. Result should be saved to the same location as the first added element.
        /// Otherwise will be put to the root of the drive.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void DoSave() {
            ModelRepository.Save(Namespace, $"{OutputPath}/{Name}.{Suffix}", true);
        }

        public string DoCreateCode() {
            // Creates compile unit from Namespace data (Code model - Keine Datei - Sprachunabhängig
            var compileUnit = MetaFacade.CreateCode(Namespace, AmbientName);
            // Interfaces need to be removed or edited
            compileUnit = AdaptInterfaces(compileUnit);

            StringWriter writer = new();
            CodeGeneratorOptions options = new() {
                IndentString = "\t"
            };
            CSharpCodeProvider codeProvider = new();
            codeProvider.GenerateCodeFromCompileUnit(compileUnit, writer, options);
            return writer.ToString();

            // Creates actual code 
            //MetaFacade.GenerateCode(compileUnit, codeProvider, "D:\\Tools\\Microsoft Visual Studio\\Repos\\Code First Modeling\\CodeToModel\\Generated\\CTMGenerator\\", true);
            //return "";
        }

        private CodeCompileUnit AdaptInterfaces(CodeCompileUnit ccu) {
            CodeNamespaceCollection nsCollection = ccu.Namespaces;
            foreach (CodeNamespace cn in nsCollection) {

                CodeTypeDeclarationCollection types = cn.Types;
                for (int i = types.Count - 1; i >= 0; i--) {

                    CodeTypeDeclaration currentType = types[i];
                    if (currentType.IsInterface) {
                        INamedTypeSymbol modelSymbol;
                        if (!NamespaceSymbols.TryGetValue(currentType.Name, out modelSymbol)) {
                            continue;
                        }

                        if (modelSymbol.AllInterfaces.Any(baseType => baseType.Name.Equals(nameof(IModelElement)))) {
                            types.RemoveAt(i);
                        }
                        else if (IsSymbolPartial(modelSymbol)) {
                            currentType.Members.Clear();
                        }
                        else {
                            string comment = $"TODO Model Interface should be partial or implement {nameof(IModelElement)}!";
                            currentType.Comments.Add(new CodeCommentStatement(comment));
                        }
                    }
                }
            }

            return ccu;
        }   
        
        private bool IsSymbolPartial(INamedTypeSymbol symbol) {
            return symbol.DeclaringSyntaxReferences
                            .Select(syntaxRef => syntaxRef
                            .GetSyntax())
                            .OfType<InterfaceDeclarationSyntax>()
                            .Any(declaration => declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
        }

        public string GetName() {
            return Name;
        }

        public string GetFullName() {
            return FullName;
        }
    }
}
