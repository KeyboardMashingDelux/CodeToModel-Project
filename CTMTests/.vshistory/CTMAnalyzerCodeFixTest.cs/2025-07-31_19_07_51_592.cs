using CTMCodeFixes;
using CTMLib;
using CTMGenerator;
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

        private const string TestCode = $@"
using CTMLib;
using NMF.Models;
using NMF.Expressions;

[assembly: ModelMetadata(""http://github.com/CodeToModel"", ""CodeToModel.Example.Sentence.nmeta"")]

[assembly: ModelMetadata(""http://github.com/CodeToModel"", ""TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta"")]

namespace CodeToModel.Example {{
    [ModelInterface]
    public interface ISentence {{

        [UpperBound(64)] 
        [LowerBound(0)]
        public IList<IWord> Words {{ get; }} 
    }}
}}
";

        private static void configureTestState(SolutionState testState, string source, DiagnosticResult? expected) {
            testState.Sources.Add(source);
            if (expected != null) {
                testState.ExpectedDiagnostics.Add((DiagnosticResult) expected);
            }
            testState.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
            testState.AdditionalReferences.Add(typeof(ModelInterface).Assembly);
        }

        [Fact]
        public async Task analyzeVerify() {
            //DiagnosticResult expected = VerifyAnal.Diagnostic().WithLocation(11, 5);
            DiagnosticResult expected = new DiagnosticResult(CTMDiagnostics.RequiredModelInterfaceKeyword).WithLocation(1, 1);
            CSharpAnalyzerTest<CTMAnalyzer, DefaultVerifier> analyzerTest = new();
            configureTestState(analyzerTest.TestState, TestCode, expected);
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