using CTMLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp;
using NMF.Expressions.Linq;
using NMF.Models;
using NMF.Models.Meta;
using NMF.Models.Repository;
using NMF.Utilities;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Immutable;


namespace CTMGenerator {

    public class ModelBuilder {

        private ModelRepository ModelRepository;
        private INamespace Namespace;

        private string FullName, Name, AmbientName, Prefix, Suffix;
        private string? OutputPath;

        private List<TypeHelper> RefTypeInfos;

        private IDictionary<string, INamedTypeSymbol> NamespaceSymbols;

        public ModelBuilder(string? uri, string? filename) {
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
            if (element is INamedTypeSymbol namedElement) {
                NamespaceSymbols.Add(element.Name, namedElement);
            }
            else {
                throw new InvalidOperationException($"Added element ({element.Name}) is not a INamedTypeSymbol.");
            }

            if (string.IsNullOrWhiteSpace(OutputPath)) {
                OutputPath = ModelBuilderHelper.GetSavePath(element);
            }
        }

        public void CreateModel() {
            foreach (INamedTypeSymbol namedType in NamespaceSymbols.Values) {
                switch (namedType.TypeKind) {
                    case TypeKind.Interface:
                        AddClass(namedType);
                        break;
                    case TypeKind.Enum:
                        AddEnum(namedType);
                        break;
                    default:
                        throw new InvalidOperationException($"Added element ({namedType.Name}) is not an Interface or Enumeration.");
                }
            }

            AddClassInformation();
            CreateReferences();
        }

        /// <summary>
        /// Adds a <see cref="Enumeration"/> to the model namespace.
        /// </summary>
        /// <param name="element"></param>
        private void AddEnum(ITypeSymbol element) {
            ImmutableArray<AttributeData> elementAttributes = element.GetAttributes();

            Enumeration enumeration = new() {
                Name = element.Name,
                Remarks = ModelBuilderHelper.GetDocElementText(element, Utilities.REMARKS),
                Summary = ModelBuilderHelper.GetDocElementText(element, Utilities.SUMMARY)
            };

            List<IFieldSymbol> literalSymbols = element.GetMembers()
                                                       .OfType<IFieldSymbol>()
                                                       .Where(f => f.IsConst)
                                                       .ToList();

            List<ILiteral> literals = ModelBuilderHelper.ConvertLiterals(literalSymbols);
            enumeration.Literals.AddRange(literals);

            Namespace.Types.Add(enumeration);
        }

        /// <summary>
        /// Adds a <see cref="Class"/> with basic information like it's name to the model namespace.
        /// </summary>
        private void AddClass(ITypeSymbol element) {
            ImmutableArray<AttributeData> elementAttributes = element.GetAttributes();
 
            Class elementClass = new() {
                Name = element.Name.Substring(1),
                IsAbstract = Utilities.GetAttributeByName(elementAttributes, nameof(IsAbstract)) != null,
                IdentifierScope = ModelBuilderHelper.GetIdentifierScope(elementAttributes),
                Remarks = ModelBuilderHelper.GetDocElementText(element, Utilities.REMARKS),
                Summary = ModelBuilderHelper.GetDocElementText(element, Utilities.SUMMARY)
            };

            Namespace.Types.Add(elementClass);
        }

        /// <summary>
        /// Adds information, which was not added through <see cref="AddClass"/>, to all namespace classes.
        /// </summary>
        /// <remarks>
        /// Assumes all Model elements have been added already!
        /// </remarks>
        private void AddClassInformation() {
            foreach (IType type in Namespace.Types) {
                if (type is not IClass classType) {
                    continue;
                }

                // Analyzer should gurantee that the first letter of each interface is an "I"
                INamedTypeSymbol classElement = NamespaceSymbols["I" + classType.Name];
                ImmutableArray<AttributeData> classAttributes = classElement.GetAttributes();

                // Add instanceof IClass
                string? instanceOfClassName = ModelBuilderHelper.GetFirstString(classAttributes, nameof(InstanceOfAttribute));
                if (GetTypeByName(instanceOfClassName) is IClass instanceOfClass) {
                    classType.InstanceOf = instanceOfClass;
                }

                // Add base types
                AddBaseType(classType, classElement.BaseType?.Name);
                ImmutableArray<INamedTypeSymbol> classInterfaces = classElement.Interfaces;
                foreach (INamedTypeSymbol classInterface in classInterfaces) {
                    AddBaseType(classType, classInterface.Name);
                }

                // Add References, Attributes and Operations
                ImmutableArray<ISymbol> members = classElement.GetMembers();
                var (properties, methodes) = ModelBuilderHelper.GetClassMembers(members);

                var (references, attributes, idAttribute) = ModelBuilderHelper.ConvertProperties(properties, out var refTypeInfos);
                RefTypeInfos.AddRange(refTypeInfos);

                List<Operation> operations = ModelBuilderHelper.ConvertMethods(methodes, out refTypeInfos);
                RefTypeInfos.AddRange(refTypeInfos);

                classType.References.AddRange(references);
                classType.Attributes.AddRange(attributes);
                classType.Operations.AddRange(operations);


                // Add identifier
                classType.Identifier = idAttribute;
            }
        }

        /// <summary>
        /// Adds a base type to the given <see cref="IClass"/>. <br/>
        /// If the given base type name is not part of the model
        /// creates a new class with the given name.
        /// </summary>
        public void AddBaseType(IClass classType, string? baseTypeName) {
            if (!string.IsNullOrWhiteSpace(baseTypeName)) {
                IEnumerable<IType> possibleRefType = Namespace.Types.Where((type) => type.Name.Equals(baseTypeName));
                if (possibleRefType != null && possibleRefType.Count() == 1) {
                    if (possibleRefType.First() is IClass refClass) {
                        classType.BaseTypes.Add(refClass);
                    }
                }
                else {
                    classType.BaseTypes.Add(new Class() { Name = baseTypeName });
                }
            }
        }

        private IType? GetTypeByName(string? name) {
            if (!string.IsNullOrWhiteSpace(name)) {
                foreach (IType type in Namespace.Types) {
                    if (type.Name.Equals(name)) {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates all non-generic references of the model.
        /// </summary>
        private void CreateReferences() {
            for (int i = RefTypeInfos.Count - 1; i >= 0; i--) {
                RefTypeInfos[i].SetType(Namespace.Types);
                RefTypeInfos.RemoveAt(i);
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
                    else if (currentType.IsEnum) {
                        types.RemoveAt(i);
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
