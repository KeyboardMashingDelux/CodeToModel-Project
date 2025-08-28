using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using CTMLib;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Vehicle.Bike.nmeta")]

namespace CodeToModel.Vehicle {

    [ModelInterface]
    public interface IVehicle : IModelElement {

        IListExpression<IWheel> Wheels { get; }

        public ICollectionExpression<ILight> Lights();
    }

    [ModelInterface]
    public interface IBicylce : IVehicle, IModelElement {

        [Refines(nameof(Wheels))]
        IBikeWheel FrontWheel { get; set; }

        [Refines(nameof(Wheels))]
        IBikeWheel RearWheel { get; set; } 

        // Geht weil IOrderedSetExpression auch Ordered -> ISetExpression ist nicht Ordered
        // Wenn wheels ICollectionExpression -> Dann kann alle Expression sein
        [Refines(nameof(Wheels))]
        IOrderedSetExpression<IBikeWheel> HelperWheels { get; } // Referenz -> Refines = wheelsModelElement

        [Refines(nameof(Wheels))]
        ISetExpression<IBikeWheel> MoreWheels { get; } // Nicht erlaubt

        [Refines(nameof(Lights))]
        public IOrderedSetExpression<ITailLight> TailLights(); // TODO Warum darf kein ISetExpression sein?
    }

    [ModelInterface]
    public partial interface IWheel {

    }

    [ModelInterface]
    public partial interface IBikeWheel : IWheel {

    }

    [ModelInterface]
    public partial interface ILight {

    }

    [ModelInterface]
    public partial interface ITailLight : ILight {

    }
}
