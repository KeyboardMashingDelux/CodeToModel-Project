using CTMLib;
using NMF.Models;

namespace CodeToModel.Example {

    [ModelInterface]
    public interface IWord : IModelElement {

        //public ISentence something;

        public string Text { get; set; }
    }
}
