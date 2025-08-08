using CTMLib;
using NMF.Models;
using NMF.Expressions;
using System.ComponentModel;

// Voller Namespace in Attribut -> Analyzer Information möglich (Gibt es irgendwo im Project eine Namespace dieses Namens?)?
// Wenn ModelInterface Attribut vorhanden -> Gibt es Assembly ModelMetadata
[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Example.Sentence.nmeta")]

//[assembly: ModelMetadata("http://github.com/CodeToModel", "TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta")]

namespace CodeToModel.Example {


    // Interface muss entweder partial sein oder IModelElement implementieren
    [ModelInterface]
    [Remarks("TEST REMARK")]
    [Summary("TEST SUMMARY")]
    public partial interface ISentence {

        // TODO Wenn Fehler in generierter Klasse IListExpression anstatt IList erfordern -> Analyzer 
        // TODO Am besten gleich CodeFix

        [UpperBound(64)] 
        [LowerBound(0)]
        public IListExpression<IWord> Words { get; } 

        // TODO Für Enumerationen:
        // TODO Bei bereits vorhandener = Primitiver Typ erzeugen
        // TODO Bei eigener als model element & Reference erzeugen -> Neues Enum Attribut ModelEnum
        // -=> GEHT ABER NICHT ???
        //public ISetExpression<SentenceTypes> SentenceTypes { get; set; }

        //public SentenceTypes MainSentenceType { get; set; }

        public IWord FirstWord { get; set; }

        public int? WordCount { get; set; }

        public void PrintSentence(int times);

        public IWord WordsAsURI();

        public event PropertyChangedEventHandler WordCountChanged;
    }
}
