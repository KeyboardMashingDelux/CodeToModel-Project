using NMF.Collections.Generic;
using NMF.Expressions;

namespace CodeToModel {

    public interface IVehicle {

        // Wenn Refined wird dann darf nicht unique sein (ISetExpression oder IOrderedSet)
        IListExpression<IWheel> wheels { get; } // Referenz -> Refines = null
    }

    public interface IBicylce : IVehicle {

        // TODO Neues Attribut: [Refines(nameof(BasisClass.attribut/refernz/operation))]
        // -> Attribut muss auf Attribut, Ref auf Ref, Op auf Op
        // -> MUSS aus basis Klasse kommen
        // -> Attr -> Gleicher Type, ander multiplizität möglich (List<strin> kann string werden) -> Basis darf auch nicht unique sein
        // -> Ref -> Gleicher Type oder unterklasse, ander multiplizität
        // -> Op -> Wie Attr und Ref

        // Analyzer: IBikeWheel muss IWheel inteface als BaseType haben oder gleich sein
        //[Refines(IVehicle.wheels)]
        IBikeWheel frontWheel { get; set; } // Referenz -> Refines = wheelsModelElement

        //[Refines(IVehicle.wheels)]
        IBikeWheel rearWheel { get; set; } // Referenz -> Refines = wheelsModelElement

        //[Refines(IVehicle.wheels)]
        // Geht weil IOrderedSetExpression auch Ordered -> ISetExpression ist nicht Ordered
        // Wenn wheels ICollectionExpression -> Dann kann alle Expression sein
        IOrderedSetExpression<IBikeWheel> helperWheels { get; } // Referenz -> Refines = wheelsModelElement
    }

    public interface IWheel {

    }

    public interface IBikeWheel : IWheel {

    }
}
