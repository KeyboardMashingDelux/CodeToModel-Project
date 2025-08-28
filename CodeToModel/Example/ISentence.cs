using CTMLib;
using NMF.Models;
using NMF.Expressions;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Example.Sentence.nmeta")]

//[assembly: ModelMetadata("http://github.com/CodeToModel", "TEST.ASSEMBLY.ANALYZER.UNESCESSARY.nmeta")]

//[assembly: ModelMetadata("http://github.com/CodeToModel", "FAIL")]


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

        public ISetExpression<SentenceTypes> SentenceTypes { get; }

        public SentenceTypes MainSentenceType { get; set; }

        public IWord FirstWord { get; set; }
        // Analyzer -> Keine XExpressio = Unbedingt set & get

        public int? WordCount { get; set; }

        /// <summary>
        /// PRINTING WAT ELSE
        /// </summary>
        /// <param name="times"><remarks>REMARKS?</remarks>>SOEMTHING ELSE?</param>
        /// <param name="seperator"><summary>SUMMARY?</summary>NOTHING ELSE?</param>
        public void PrintSentence(int times, IWord seperator);

        public IWord WordsAsURI();

        [Id]
        public Exception SomeNotPrimitivId { get; set; }

        public IFormattable InterfaceNotFromModel { get; set; }
    }
}
