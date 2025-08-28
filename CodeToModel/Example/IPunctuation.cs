using CTMLib;
using NMF.Models;

namespace CodeToModel.Example {

    //[InstanceOf(nameof(IDisposable))] 
    [ModelInterface]
    public interface IPunctuation : IModelElement, IWord //, IDisposable
        {
    }
}
