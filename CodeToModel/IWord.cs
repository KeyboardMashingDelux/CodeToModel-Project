using CTMLib;

namespace CodeToModel {

    [ModelInterface]
    public interface IWord : ISentence {

        public string Text { get; }
    }
}
