using Microsoft.CodeAnalysis;

namespace CTMGenerator {
    public class CTMDiagnostics {

        private const string Category = "CodeToModel";

        // TODO Implement
        public static readonly DiagnosticDescriptor InterfaceNameDescriptor = new(
            id: "CTM001",
            title: "Interface annotated with ModelInterface Attribute does not start with uppercase 'I'",
            messageFormat: "Interface {0} does not start with uppercase 'I' followed by another uppercase letter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error, 
            isEnabledByDefault: true);

        // TODO Implement
        public static readonly DiagnosticDescriptor ModelInterfaceNoModelMetadataDescriptor = new(
            id: "CTM002",
            title: "ModelInterface has no ModelMetadata assembly entry",
            messageFormat: "Namepsace of Interface {0} annotated with the ModelInterface Attribute has no matching ModelMetadata assembly entry",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // TODO Implement
        public static readonly DiagnosticDescriptor AssemblyMetadataNoNamespaceMetadataNo = new(
            id: "CTM003",
            title: "No matching Namespace found",
            messageFormat: "Assembly entry has no matching Namespace named {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
