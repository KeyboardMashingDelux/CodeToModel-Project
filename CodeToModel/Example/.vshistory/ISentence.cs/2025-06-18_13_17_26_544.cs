using CTMLib;
using NMF.Models;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.fsm.nmeta")]

namespace CodeToModel {

    [ModelInterface]
    public interface ISentence {

        [UpperBound(64)] 
        [LowerBound(0)]
        public List<IWord> Words { get; }
    }
}
