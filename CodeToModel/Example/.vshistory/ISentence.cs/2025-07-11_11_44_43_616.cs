using CTMLib;
using NMF.Models;

// Voller Namespace in Attribut -> Analyzer Information möglich (Gibt es irgendwo im Project eine Namespace dieses Namens?)?
// Wenn ModelInterface Attribut vorhanden -> Gibt es Assembly ModelMetadata
[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Example.Sentence.nmeta")]

//[assembly: ModelMetadata("http://github.com/CodeToModel", "TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta")]

namespace CodeToModel.Example {


    // Interface muss entweder partial sein oder IModelElement implementieren
    [ModelInterface]
    public interface ISentence {

        [UpperBound(64)] 
        [LowerBound(0)]
        public List<IWord> Words { get; }


        public IWord FirstWord { get; }

        public int WordCount { get; }
    }
}
