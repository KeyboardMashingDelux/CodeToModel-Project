using CTMLib;
using NMF.Models;

namespace CodeToModel.Example {

    // TODO [IstanceOf(IOtherModelClass)] Create -> Annehmen das im gleichen Modell, sonst null
    // TODO Analyzer -> gibt es IOtherModelClass?
    [InstanceOf(nameof(IWord))]
    [ModelInterface]
    public interface IPunctuation : IModelElement, IWord {
    }
}
