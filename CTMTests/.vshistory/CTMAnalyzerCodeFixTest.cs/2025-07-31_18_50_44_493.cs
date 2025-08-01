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
        public void Test1() {

        }
    }
}