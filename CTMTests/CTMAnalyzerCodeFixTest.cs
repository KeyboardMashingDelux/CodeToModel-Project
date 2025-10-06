using CTMCodeFixes;
using CTMAnalyzer;
using CTMLib;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NMF.Expressions;
using NMF.Models;

namespace CTMTests {

    /// <summary>
    /// Conecept testcases for testing analyzers & code fixes
    /// </summary>
    public class CTMAnalyzerCodeFixTest {

        private const string TestCode = $@"
using CTMLib;
using NMF.Models;
using NMF.Expressions;
using System.Collections.Generic;

[assembly: ModelMetadata(""http://github.com/CodeToModel"", ""CodeToModel.Example.Sentence.nmeta"")]

[assembly: ModelMetadata(""http://github.com/CodeToModel"", ""TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta"")]

namespace CodeToModel.Example {{
    [ModelInterface]
    public interface ISentence {{

        [UpperBound(64)] 
        [LowerBound(0)]
        public IList<string> Words {{ get; }} 
    }}
}}
";

        private static void ConfigureTestState(SolutionState testState, string source, params DiagnosticResult?[]? expected) {
            testState.Sources.Add(source);
            if (expected != null) {
                foreach (var expectedItem in expected) {
                    if (expectedItem != null) {
                        testState.ExpectedDiagnostics.Add((DiagnosticResult)expectedItem);
                    }
                }
            }
            testState.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
            testState.AdditionalReferences.Add(typeof(ModelInterface).Assembly);
            testState.AdditionalReferences.Add(typeof(ModelMetadataAttribute).Assembly);
            testState.AdditionalReferences.Add(typeof(IListExpression<string>).Assembly);
        }

        [Fact]
        public async Task AnalyzeVerify() {
            DiagnosticResult interfaceModfiers = new DiagnosticResult(CTMDiagnostics.RequiredModelInterfaceKeyword).WithLocation(13, 22).WithArguments("ISentence");
            // Gets correctly reported in a test environment
            DiagnosticResult assemblyNoNamespace = new DiagnosticResult(CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor).WithLocation(9, 12).WithArguments("TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta");
            DiagnosticResult listToExpression = new DiagnosticResult(CTMDiagnostics.IListExpressionInstead).WithLocation(17, 30).WithArguments("Words");
            // Only NMF collections have correct get/set checks since they should be used
            DiagnosticResult getSetRequired = new DiagnosticResult(CTMDiagnostics.GetSetNeeded).WithLocation(17, 30).WithArguments("Words");
            
            CSharpAnalyzerTest<CTMDiagnosticAnalyzer, DefaultVerifier> analyzerTest = new();
            ConfigureTestState(analyzerTest.TestState, TestCode, listToExpression, assemblyNoNamespace, interfaceModfiers, getSetRequired);
            await analyzerTest.RunAsync();
        }

        [Fact]
        public async Task CodeFixVerify() {
            DiagnosticResult interfaceModfiers = new DiagnosticResult(CTMDiagnostics.RequiredModelInterfaceKeyword).WithLocation(13, 22).WithArguments("ISentence");
            DiagnosticResult assemblyNoNamespace = new DiagnosticResult(CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor).WithLocation(9, 12).WithArguments("TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta");
            DiagnosticResult listToExpression = new DiagnosticResult(CTMDiagnostics.IListExpressionInstead).WithLocation(17, 30).WithArguments("Words");
            // Only NMF collections have correct get/set checks since they should be used
            DiagnosticResult getSetRequired = new DiagnosticResult(CTMDiagnostics.GetSetNeeded).WithLocation(17, 30).WithArguments("Words");
            var codeFixTest = new CSharpCodeFixTest<CTMDiagnosticAnalyzer, CTMCodeFixProvider, DefaultVerifier> { };
            ConfigureTestState(codeFixTest.TestState, TestCode, listToExpression, assemblyNoNamespace, interfaceModfiers, getSetRequired);
            await codeFixTest.RunAsync();
        }

        //[Fact]
        //public async Task noCodeFixVerify() {
        //    var codeFixTest = new CSharpCodeFixTest<DemoAnalyzer, DemoCodeFixProvider, DefaultVerifier> { };
        //    configureTestState(codeFixTest.TestState, fixedCode, null);
        //    await codeFixTest.RunAsync();
        //}
    }
}