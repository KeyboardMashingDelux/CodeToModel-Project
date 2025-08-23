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
using System.Diagnostics;


namespace CTMGenerator {

    /// <summary>
    /// Class used for constructing models and generate source code from them.
    /// </summary>
    public class ModelBuilder {

        private readonly ModelRepository ModelRepository;
        private readonly Namespace Namespace;

        private readonly string FullName, Name, AmbientName, Prefix, Suffix;
        private string? OutputPath;

        private readonly PropertyConversionHelper PropertyConverter;
        private readonly MethodConversionHelper MethodConverter;
        private readonly LiteralConversionHelper LiteralConverter;

        private readonly List<TypeHelper> RefTypeInfos;

        private readonly Dictionary<string, INamedTypeSymbol> NamespaceSymbols;

        private readonly Compilation GeneratorCompilation;



        /// <summary>
        /// Initalizes a basic <see cref="NMF.Models.Repository.ModelRepository"/> and <see cref="NMF.Models.Meta.Namespace"/>.
        /// </summary>
        /// <param name="uri">The model namespace uri.</param>
        /// <param name="ressourceName">The full model ressource name containing at least the name, prefix and suffix of the model
        /// with the following syntax: NAME.PREFIX.SUFFIX.</param>
        /// <param name="generatorCompilation"><see cref="Compilation"/> needed for corretly generating source code.</param>
        public ModelBuilder(string? uri, string? ressourceName, Compilation generatorCompilation) {
            GeneratorCompilation = generatorCompilation;
            Uri namespaceURI = new(uri ?? ModelBuilderHelper.DefaultUri);
            (FullName, Name, AmbientName, Prefix, Suffix) = ModelBuilderHelper.GetResourceInfo(ressourceName);

            ModelRepository = new ModelRepository();
            Namespace = new Namespace() {
                Name = Name,
                Prefix = Prefix,
                Uri = namespaceURI
            };

            PropertyConverter = new();
            MethodConverter = new();
            LiteralConverter = new();

            RefTypeInfos = [];
            NamespaceSymbols = [];

            //Debugger.Launch();
        }

        /// <summary>
        /// Adds a element to the model.
        /// <br/>
        /// After adding all elements <see cref="CreateModel"/> has to be called!
        /// </summary>
        /// <param name="element"></param>
        public void AddElement(INamedTypeSymbol element) {
            NamespaceSymbols.Add(element.Name, element);

            if (string.IsNullOrWhiteSpace(OutputPath)) {
                OutputPath = ModelBuilderHelper.GetSavePath(element);
            }
        }

        /// <summary>
        /// Creates the model from the elements added through <see cref="AddElement"/>.
        /// </summary>
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
                        break;
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
                Remarks = ModelBuilderHelper.GetElementRemarks(element),
                Summary = ModelBuilderHelper.GetDocElementText(element, Utilities.SUMMARY)
            };

            List<IFieldSymbol> literalSymbols = element.GetMembers()
                                                       .OfType<IFieldSymbol>()
                                                       .Where(f => f.IsConst)
                                                       .ToList();

            LiteralConverter.CleanConvert(literalSymbols);
            enumeration.Literals.AddRange(LiteralConverter.Literals);

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
                Remarks = ModelBuilderHelper.GetElementRemarks(element),
                Summary = ModelBuilderHelper.GetElementSummary(element)
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

                PropertyConverter.CleanConvert(properties);
                RefTypeInfos.AddRange(PropertyConverter.RefTypeInfos);

                MethodConverter.CleanConvert(methodes);
                RefTypeInfos.AddRange(MethodConverter.RefTypeInfos);

                classType.References.AddRange(PropertyConverter.References);
                classType.Attributes.AddRange(PropertyConverter.Attributes);
                classType.Operations.AddRange(MethodConverter.Operations);


                // Add identifier
                // TODO Was wenn Referenz die zu Attribut wird?
                classType.Identifier = PropertyConverter.IdAttribute;
            }
        }

        /// <summary>
        /// Adds a base type to the given <see cref="IClass"/>
        /// if the given base type name is part of the model.
        /// <br/>
        /// Should the given baseTypeName represent an interface retries with
        /// a non-interface name.
        /// </summary>
        public void AddBaseType(IClass classType, string? baseTypeName) {
            if (!string.IsNullOrWhiteSpace(baseTypeName) && !baseTypeName!.Equals("IModelElement")) {
                IEnumerable<IType> possibleRefType = Namespace.Types.Where((type) => type.Name.Equals(baseTypeName));
                if (possibleRefType != null && possibleRefType.Count() == 1) {
                    if (possibleRefType.First() is IClass refClass) {
                        classType.BaseTypes.Add(refClass);
                    }
                }
                else if (Utilities.IsValidInterfaceName(baseTypeName)) {
                    AddBaseType(classType, baseTypeName.Substring(1));
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
                TypeHelper refTypeInfo = RefTypeInfos[i];
                if (!refTypeInfo.SetType(Namespace.Types) && refTypeInfo.Reference != null) {
                    IReference reference = refTypeInfo.Reference;
                    if (reference.Parent is IClass referenceParent) {
                        referenceParent.References.Remove(reference);
                        referenceParent.Attributes.Add(refTypeInfo.ConvertToAttribute(reference, Namespace.Types));
                    }
                }
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

        /// <summary>
        /// Generates source code from the mode constructed with <see cref="CreateModel"/>.
        /// </summary>
        /// <returns>The generated source code</returns>
        public string DoCreateCode() {
            // Creates compile unit from Namespace data (Code model - Keine Datei - Sprachunabhängig
            var compileUnit = MetaFacade.CreateCode(Namespace, AmbientName);
            // Interfaces need to be removed or edited
            AdaptInterfaces(compileUnit);

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

        /// <summary>
        /// Removes the body or whole interface depending if the model source has them already defined or not.
        /// </summary>
        private void AdaptInterfaces(CodeCompileUnit ccu) {
            CodeNamespaceCollection nsCollection = ccu.Namespaces;
            foreach (CodeNamespace cn in nsCollection) {

                CodeTypeDeclarationCollection types = cn.Types;
                for (int i = types.Count - 1; i >= 0; i--) {

                    CodeTypeDeclaration currentType = types[i];
                    if (currentType.IsInterface) {
                        if (!NamespaceSymbols.TryGetValue(currentType.Name, out INamedTypeSymbol modelSymbol)) {
                            continue;
                        }

                        if (ModelBuilderHelper.ImplementsIModelElement(modelSymbol, GeneratorCompilation)) {
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
