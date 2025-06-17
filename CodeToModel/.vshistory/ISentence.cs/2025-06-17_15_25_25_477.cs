using CTMLib;
using NMF.Models;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.fsm.nmeta")]

namespace CodeToModel {

    [ModelInterface]
    public interface ISentence {

        public List<IWord> Words { get; }
    }
}
