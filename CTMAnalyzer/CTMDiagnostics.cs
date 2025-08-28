using Microsoft.CodeAnalysis;

namespace CTMAnalyzer {
    public class CTMDiagnostics {

        private const string Category = "CodeToModel";

        public static readonly DiagnosticDescriptor InterfaceNameDescriptor = new(
            id: "CTM001",
            title: "Interface annotated with ModelInterface Attribute does not start with uppercase 'I'",
            messageFormat: "Interface {0} does not start with uppercase 'I' followed by another uppercase letter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error, 
            isEnabledByDefault: true);

        // TODO Fix implementation
        public static readonly DiagnosticDescriptor ModelInterfaceNoModelMetadataDescriptor = new(
            id: "CTM002",
            title: "ModelInterface has no ModelMetadata assembly entry",
            messageFormat: "Namespace {0} of Interface {1} annotated with the ModelInterface Attribute has no matching ModelMetadata assembly entry",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AssemblyMetadataNoNamespaceDescriptor = new(
            id: "CTM003",
            title: "No matching Namespace found",
            messageFormat: "Assembly entry has no matching Namespace named {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RequiredModelInterfaceKeyword = new(
            id: "CTM004",
            title: "Missing model Interface modifier",
            messageFormat: "Model Interface {0} does not implement IModelElement or has the partial modifier",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor IListExpressionInstead = new(
            id: "CTM005",
            title: "Unsupported collection type",
            messageFormat: "Type of Property {0} is not supported, use IListExpression instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ISetExpressionInstead = new(
            id: "CTM006",
            title: "Unsupported collection type",
            messageFormat: "Type of Property {0} is not supported, use ISetExpression instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor IOrderedSetExpressionInstead = new(
            id: "CTM007",
            title: "Unsupported collection type",
            messageFormat: "Type of Property {0} is not supported, use IOrderedSetExpression instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ModelMetadataResourceNameParts = new(
            id: "CTM008",
            title: "ModelMetadata resource name should consist of 3 parts (NAME.PREFIX.SUFFIX)",
            messageFormat: "ModelMetadata assembly entry {0} does not consist of 3 parts",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InstanceOfValid = new(
            id: "CTM009",
            title: "InstanceOf value not valid",
            messageFormat: "InstanceOf type name {0} could not be found in this namespace",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BaseTypeNotModelElement = new(
            id: "CTM010",
            title: "Base type not part of this model - may need to be added manually",
            messageFormat: "Base type {0} was not found in this namespaces source or was not annotated with IModelInterface",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GetSetNeeded = new(
            id: "CTM011",
            title: "Get and Set needed for non collection properties",
            messageFormat: "Property {0} needs to have get and set",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GetOnlyNeeded = new(
            id: "CTM012",
            title: "Get only needed for collection properties",
            messageFormat: "Property {0} needs to have only get",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
