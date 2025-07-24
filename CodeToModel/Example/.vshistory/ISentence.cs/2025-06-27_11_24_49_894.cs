using CTMLib;
using NMF.Models;

// Voller Namespace in Attribut -> Analyzer Information möglich (Gibt es irgendwo im Project eine Namespace dieses Namens?)?
[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Example.words.nmeta")]

namespace CodeToModel.Example {

    [ModelInterface]
    public interface ISentence {

        [UpperBound(64)] 
        [LowerBound(0)]
        [IsUnique]
        public List<IWord> Words { get; }
    }
}
