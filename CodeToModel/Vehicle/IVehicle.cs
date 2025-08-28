using NMF.Collections.Generic;
using NMF.Expressions;
using NMF.Models;
using CTMLib;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Vehicle.Bike.nmeta")]

namespace CodeToModel.Vehicle {

    [ModelInterface]
    public interface IVehicle : IModelElement {

        // Wenn Refined wird dann darf nicht unique sein (ISetExpression oder IOrderedSet)
        IListExpression<IWheel> Wheels { get; } // Referenz -> Refines = null

        public ICollectionExpression<ILight> Lights(); // Referenz -> Refines = null
    }

    [ModelInterface]
    public interface IBicylce : IVehicle, IModelElement {

        // TODO Neues Attribut: [Refines(nameof(BasisClass.attribut/refernz/operation))]
        // -> Attribut muss auf Attribut, Ref auf Ref, Op auf Op
        // -> MUSS aus basis Klasse kommen
        // -> Attr -> Gleicher Type, ander multiplizität möglich (List<strin> kann string werden) -> Basis darf auch nicht unique sein
        // -> Ref -> Gleicher Type oder unterklasse, ander multiplizität
        // -> Op -> Wie Attr und Ref

        // Analyzer: IBikeWheel muss IWheel inteface als BaseType haben oder gleich sein
        [Refines(nameof(Wheels))]
        IBikeWheel FrontWheel { get; set; } // Referenz -> Refines = wheelsModelElement

        [Refines(nameof(Wheels))]
        IBikeWheel RearWheel { get; set; } // Referenz -> Refines = wheelsModelElement

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
