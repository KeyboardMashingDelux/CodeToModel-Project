using CTMLib;
using NMF.Models;

namespace CodeToModel.Example {

    [ModelInterface]
    public interface IWord : IModelElement {
        public string Text { get; set; }

        public object SomeObject { get; set; }
    }
}
