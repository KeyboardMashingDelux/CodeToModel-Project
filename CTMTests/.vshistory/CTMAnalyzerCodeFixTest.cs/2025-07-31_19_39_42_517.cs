using CTMCodeFixes;
using CTMGenerator;
using CTMLib;
using NMF.Models;
using NMF.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyAnal = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    CTMGenerator.CTMAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using VerifyCF = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    CTMGenerator.CTMAnalyzer, CTMCodeFixes.CTMCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace CTMTests {

    public class CTMAnalyzerCodeFixTest {

        private const string AssemblyEntries = $@"
[assembly: ModelMetadata(""http://github.com/CodeToModel"", ""CodeToModel.Example.Sentence.nmeta"")]

";

        private const string TestCode = $@"
using CTMLib;
using NMF.Models;
using NMF.Expressions;
using System.Collections.Generic;



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
            testState.Sources.Add(AssemblyEntries);
            testState.Sources.Add(source);
            if (expected != null) {
                foreach (var expectedItem in expected) {
                    if (expectedItem != null) {
                        testState.ExpectedDiagnostics.Add((DiagnosticResult) expectedItem);
                    }
                }
            }
            testState.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
            testState.AdditionalReferences.Add(typeof(ModelInterface).Assembly);
            testState.AdditionalReferences.Add(typeof(ModelMetadataAttribute).Assembly);
            testState.AdditionalReferences.Add(typeof(IListExpression<string>).Assembly);
            testState.AdditionalReferences.Add(typeof(ModelMetadataAttribute).Assembly);
        }

        [Fact]
        public async Task analyzeVerify() {
            DiagnosticResult interfaceModfiers = new DiagnosticResult(CTMDiagnostics.RequiredModelInterfaceKeyword).WithLocation(1, 1);
            DiagnosticResult listToExpression = new DiagnosticResult(CTMDiagnostics.IListExpressionInstead).WithLocation(1, 1);
            CSharpAnalyzerTest<CTMAnalyzer, DefaultVerifier> analyzerTest = new();
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