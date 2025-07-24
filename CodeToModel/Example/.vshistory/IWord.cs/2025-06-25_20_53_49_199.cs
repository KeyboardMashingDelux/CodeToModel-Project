using CTMLib;

namespace CodeToModel.Example {

    [ModelInterface]
    public interface IWord : ISentence {

        public string Text { get; }
    }
}
