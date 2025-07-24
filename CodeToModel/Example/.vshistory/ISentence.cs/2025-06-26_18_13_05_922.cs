using CTMLib;
using NMF.Models;

[assembly: ModelMetadata("http://github.com/CodeToModel", "Example.fsm.nmeta")]

namespace CodeToModel.Example {

    [ModelInterface]
    public interface ISentence {

        [UpperBound(64)] 
        [LowerBound(0)]
        [IsUnique]
        public List<IWord> Words { get; }
    }
}
