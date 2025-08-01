using CTMCodeFixes;
using CTMAnalyzer;
using CTMLib;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NMF.Expressions;
using NMF.Models;

namespace CTMTests {

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

        private static void configureTestState(SolutionState testState, string source, params DiagnosticResult?[]? expected) {
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
        public async Task analyzeVerify() {
            DiagnosticResult interfaceModfiers = new DiagnosticResult(CTMDiagnostics.RequiredModelInterfaceKeyword).WithLocation(1, 1);
            DiagnosticResult listToExpression = new DiagnosticResult(CTMDiagnostics.IListExpressionInstead).WithLocation(1, 1);
            DiagnosticResult dumbResult = new DiagnosticResult(CTMDiagnostics.AssemblyMetadataNoNamespaceDescriptor).WithLocation(1, 1);
            CSharpAnalyzerTest<CTMDiagnosticAnalyzer, DefaultVerifier> analyzerTest = new();
            configureTestState(analyzerTest.TestState, TestCode, interfaceModfiers, listToExpression);
            await analyzerTest.RunAsync();
        }

        //[Fact]
        //public async Task codeFixVerify() {
        //    var expected = VerifyCF.Diagnostic(DemoDiagnosticsDescriptors.EnumerationMustBePartial.Id)
        //        .WithLocation(6, 18).WithArguments(className);
        //    var codeFixTest = new CSharpCodeFixTest<DemoAnalyzer, DemoCodeFixProvider, DefaultVerifier> { };
        //    configureTestState(codeFixTest.TestState, testCode, expected);
        //    await codeFixTest.RunAsync();
        //}

        //[Fact]
        //public async Task noCodeFixVerify() {
        //    var codeFixTest = new CSharpCodeFixTest<DemoAnalyzer, DemoCodeFixProvider, DefaultVerifier> { };
        //    configureTestState(codeFixTest.TestState, fixedCode, null);
        //    await codeFixTest.RunAsync();
        //}
    }
}