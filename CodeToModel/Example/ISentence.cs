using CTMLib;
using NMF.Models;
using NMF.Expressions;

// Voller Namespace in Attribut -> Analyzer Information möglich (Gibt es irgendwo im Project eine Namespace dieses Namens?)?
// Wenn ModelInterface Attribut vorhanden -> Gibt es Assembly ModelMetadata
[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Example.Sentence.nmeta")]

//[assembly: ModelMetadata("http://github.com/CodeToModel", "TEST.ANALYZER.ASSEMBLY.UNESCESSARY.nmeta")]

namespace CodeToModel.Example {

    [ModelInterface]
    public partial interface ISentence {

        //[Opposite(nameof(IWord.soemthng))]

        /// <summary>
        /// TEST SUMMARY
        /// </summary>
        /// <remarks>
        /// REMARKS TEST
        /// </remarks>
        [UpperBound(64)] 
        [LowerBound(0)]
        public IListExpression<IWord> Words { get; }
        // Analyzer -> XExpressio = Kein set nur get

        // TODO Für Enumerationen:
        // TODO Bei bereits vorhandener = Primitiver Typ erzeugen
        // TODO Bei eigener als model element & Reference erzeugen -> Neues Enum Attribut ModelEnum
        // -=> GEHT ABER NICHT ???
        public ISetExpression<SentenceTypes> SentenceTypes { get; }

        public SentenceTypes MainSentenceType { get; set; }

        public IWord FirstWord { get; set; }
        // Analyzer -> Keine XExpressio = Unbedingt set & get

        [Id]
        public int? WordCount { get; set; }

        public void PrintSentence(int times, IWord seperator);

        public IWord WordsAsURI();
    }
}
