# Code To Model Project Attribute Library - CTMLib

Adds several new attributes for use with the CTMGenerator package.

### ModelMetadata

Assembly attribute to identfy a model. 
Each entry represents a new model and needs to be paired with a namespace declaration.
If declared in the interfaces file directly needs to be put above the namespace declaration.

```C#
[assembly: ModelMetadata("MODELURI", "NAME.PREFIX.SUFFIX")]
``` 

### ModelInterface

Identfies a interface delcaration as part of a model.

```C#
[ModelInterface]
public partial interface IMyExample {}
``` 

### ModelEnum

Identfies a enum delcaration as part of a model.

```C#
[ModelEnum]
public enum MyEnum {}
``` 

### IsAbstract

Tells the generator to generate a abstract implementation of this interface.

```C#
[IsAbstract]
public partial interface IMyExample {}
``` 

### IsContainment

Marks a method as containment.

```C#
[IsContainment]
public string SomeOperation();
``` 

### InstanceOf

Identfies a interface delcaration as part of a model.

```C#
[ModelInterface]
[InstanceOf("SuperExample")]
public partial interface IMyExample : SuperExample {}
``` 

### IdentifierScope

Declares the scope of the identifier defined by the Id attribute.

```C#
[ModelInterface]
[IdentifierScope(NMF.Models.Meta.IdentifierScope.Global)]
public partial interface IMyExample {}
``` 

### Refines

Declares that a property refines another property.

```C#
[ModelInterface]
public interface IMyExample : IModelElement {
    IListExpression<IWheel> SomeList { get; }
}

[ModelInterface]
public interface IMyOtherExample : IMyExample, IModelElement {
    [Refines(nameof(SomeList))]
    IBikeWheel SomeProp { get; set; }
}

[ModelInterface]
public partial interface IWheel {}

[ModelInterface]
public partial interface IBikeWheel : IWheel {}
``` 
