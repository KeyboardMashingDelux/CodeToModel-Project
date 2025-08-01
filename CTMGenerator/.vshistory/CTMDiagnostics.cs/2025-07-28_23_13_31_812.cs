using Microsoft.CodeAnalysis;

namespace CTMGenerator {
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
            messageFormat: "Type {0} is not supported, use IListExpression instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ISetExpressionInstead = new(
            id: "CTM006",
            title: "Unsupported collection type",
            messageFormat: "Type {0} is not supported, use ISetExpression instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor IOrderedSetExpressionInstead = new(
            id: "CTM007",
            title: "Unsupported collection type",
            messageFormat: "Type {0} is not supported, use IOrderedSetExpression instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    }
}
