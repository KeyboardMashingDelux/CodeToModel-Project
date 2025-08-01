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

        // TODO Wenn Fehler in generierter Klasse IListExpression anstatt IList erfordern -> Analyzer 
        // TODO Am besten gleich CodeFix

        [UpperBound(64)] 
        [LowerBound(0)]
        public IList<IWord> Words { get; } 
        // TODO Wenn IWord Enumeration wäre
        // TODO Bei bereits vorhandener = Primitiver Typ erzeugen
        // TODO Bei eigener als model element & Reference erzeugen -> Neues Enum Attribut ModelEnum

        public IWord FirstWord { get; }

        public int WordCount { get; }
    }
}
