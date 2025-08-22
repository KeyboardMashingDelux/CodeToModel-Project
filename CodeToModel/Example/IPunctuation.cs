using CTMLib;
using NMF.Models;

namespace CodeToModel.Example {

    // TODO Analyzer warning für nicht modell BaseTypes z.B. wie IDisposable usw.
    // -> "Nicht teil des Modells, muss selbst hinzugefügt werden"

    // TODO [IstanceOf(IOtherModelClass)] Create -> Annehmen das im gleichen Modell, sonst null
    // TODO Analyzer -> gibt es IOtherModelClass?
    [InstanceOf(nameof(IWord))]
    [ModelInterface]
    public interface IPunctuation : IModelElement, IWord {
    }
}
