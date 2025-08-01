using CTMCodeFixes;
using CTMLib;
using CTMGenerator;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyAnal = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DemoSourceGenerator.DemoAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using VerifyCF = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DemoSourceGenerator.DemoAnalyzer, DemoCodeFixes.DemoCodeFixProvider,
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
    public interface ISentence : IModelElement {{

        [UpperBound(64)] 
        [LowerBound(0)]
        public IListExpression<IWord> Words {{ get; }} 
    }}
}}
";

        [Fact]
        public async Task analyzeVerify() {
            var expected = VerifyAnal.Diagnostic().WithSpan(6, 18, 6, 33).WithArguments(className);
            var analyzerTest = new CSharpAnalyzerTest<DemoAnalyzer, DefaultVerifier> { };
            configureTestState(analyzerTest.TestState, testCode, expected);
            await analyzerTest.RunAsync();
        }

        [Fact]
        public async Task codeFixVerify() {
            var expected = VerifyCF.Diagnostic(DemoDiagnosticsDescriptors.EnumerationMustBePartial.Id)
                .WithLocation(6, 18).WithArguments(className);
            var codeFixTest = new CSharpCodeFixTest<DemoAnalyzer, DemoCodeFixProvider, DefaultVerifier> { };
            configureTestState(codeFixTest.TestState, testCode, expected);
            await codeFixTest.RunAsync();
        }

        [Fact]
        public async Task noCodeFixVerify() {
            var codeFixTest = new CSharpCodeFixTest<DemoAnalyzer, DemoCodeFixProvider, DefaultVerifier> { };
            configureTestState(codeFixTest.TestState, fixedCode, null);
            await codeFixTest.RunAsync();
        }
    }
}