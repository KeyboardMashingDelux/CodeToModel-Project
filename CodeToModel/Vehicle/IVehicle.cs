using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using CTMLib;
using NMF.Models.Tests.Railway;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Vehicle.Bike.nmeta")]

namespace CodeToModel.Vehicle {

    [ModelInterface]
    public interface IVehicle : IModelElement {

        IListExpression<IWheel> Wheels { get; }

        public ICollectionExpression<ILight> Lights();

        public IRailwayElement RailwayElement { get; set; }
    }

    [ModelInterface]
    public interface IBicylce : IVehicle, IModelElement {

        //[Refines(nameof(Lights))]
        //public IOrderedSetExpression<ITailLight> TailLights();

        [Refines(nameof(Wheels))]
        IBikeWheel FrontWheel { get; set; }

        [Refines(nameof(Wheels))]
        IBikeWheel RearWheel { get; set; } 

        [Refines(nameof(Wheels))]
        IOrderedSetExpression<IWheel> HelperWheels { get; } 

        [Refines(nameof(Wheels))]
        ISetExpression<IBikeWheel> MoreWheels { get; } // Nicht erlaubt

        [Refines(nameof(RailwayElement))]
        public ISemaphore Semaphore { get; set; }
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
