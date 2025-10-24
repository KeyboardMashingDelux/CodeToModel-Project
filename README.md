# Code To Model Project

A code-first modeling approach utilizing [NMF](https://nmfcode.github.io/).

## NuGet Packages

The project is divided into four packages. Three core packages of generator and assist features and one library package.

* [CTMGeneator](https://www.nuget.org/packages/NMF-Expressions/): Contains the core functionallity of generating a model and source code.
* [CTMAnalyzer](https://www.nuget.org/packages/NMF-Expressions/): Analyzer for assistence when using the CTMGenerator.
* [CTMCodeFixes](https://www.nuget.org/packages/NMF-Expressions/): Code Fixes for issues reported by the CTMAnalyzer.
* [CTMLib](https://www.nuget.org/packages/NMF-Expressions/): Library of custom attributes.

## Getting started

Once the generator package is added to a project it will run in the background of every build process. 

The minimum requirements to have a model created and code generate are:

* An assembly entry for the model interface namespace above the namespace declaration.
* An interface with the ModelInterface attribute.
* The interface needs to either be partial or have IModelElement as base type. 

Example interface implementation:
```C#
using NMF.Models;
using CTMLib;

[assembly: ModelMetadata("http://github.com/CodeToModel", "CodeToModel.Example.MyExample.nmeta")]

namespace CodeToModel.Example {

    [ModelInterface]
    public interface IMyExample : IModelElement {
    }
}
``` 


## Attributes

Most model information is derived from the source code directly. 
Attributes are used to add information which can not come from the code directly.

### ModelMetadata

Assembly attribute to identfy a model. 
Each entry represents a new model and needs to be paired with a namespace declaration.
If declared in the interfaces file directly needs to be put above the namespace declaration.

```C#
[assembly: ModelMetadata("MODELURI", "NAME.PREFIX.SUFFIX")]
``` 

> [!WARNING] 
> Having two ModelMetadata entries with the same NAME attribute will stop the source code generator from running.
> Each model only needs **one** ModelMetadata entry!


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

### UpperBound & LowerBound

Gives a collection an upper and lower bound.

```C#
[UpperBound(64)] 
[LowerBound(0)]
public IListExpression<Object> objects { get; }
``` 

### Id

Marks an attribute as identifier for this model element.

```C#
[Id]
public string UUID { get; set; }
``` 

### IdentifierScope

Declares the scope of the identifier defined by the Id attribute in which this attribute is unique.

```C#
[ModelInterface]
[IdentifierScope(NMF.Models.Meta.IdentifierScope.Global)]
public partial interface ISomeElement {}
``` 

### IsAbstract

Tells the generator to generate a abstract implementation of this interface.

```C#
[IsAbstract]
public partial interface IAbstractElement {}
``` 

### IsContainment

Marks a method as containment making it a composition (child element).

```C#
[IsContainment]
public IMyOtherModelElement MyChildModelElement { get; set; }
``` 

### InstanceOf

Marks an model element as instance of another model element.

```C#
[ModelInterface]
[InstanceOf("IVehicle")]
public partial interface IBike : IVehicle {}
``` 

### Opposite

Marks a property from another model element as opposite of this element.

```C#
namespace Worksystems {

    [ModelInterface]
    public interface IWorkSystem : IModelElement {

        [Opposite(typeof(IProcess), nameof(IProcess.AssignedWorkflowSystem))]
        public IListExpression<IProcess> Processes { get; }
    }

    [ModelInterface]
    public interface IProcess : IModelElement {

        [Opposite(typeof(IWorkflowSystem), nameof(IWorkflowSystem.Processes))]
        public IWorkflowSystem AssignedWorkflowSystem { get; set; }
    }
}
```


### Refines

Declares that a property refines another property.

```C#
[ModelInterface]
public interface IVehicle : IModelElement {

    IListExpression<IWheel> Wheels { get; }
}

[ModelInterface]
public interface IBicylce : IVehicle, IModelElement {

    [Refines(nameof(Wheels))]
    IBikeWheel FrontWheel { get; set; }

    [Refines(nameof(Wheels))]
    IOrderedSetExpression<IWheel> HelperWheels { get; } 
}

[ModelInterface]
public partial interface IWheel {}

[ModelInterface]
public partial interface IBikeWheel : IWheel {}
``` 

## Comments

To have summary and remarks doc comments transfered to the model elements and generated source code add **GenerateDocumentationFile** to the .csproj file and set it to **true**.

## Location of generated files

### Model location

By default the created model files will be saved to the location of the first file containing the model annotations.
This behavior can be overriden by providing a **OutputPaths.xml** file, added via the **\<AdditionalFiles Include="OutputPaths.xml"/>** property in the .csproj, with the following structure:

```xml
<paths>
	<path namespace="ALL OR FULL NAMESPACE NAME">
		C:\FULL\ABSOLUTE\PATH\TO\DESIRED\LOCATION
	</path>
</paths>
``` 

> [!NOTE] 
> **ALL** means this path will be used for every namespace which has no __previously__ defined path.

> [!NOTE]
> No paths will be created and have to already exist and be writable for the generator to work!

### Generated source code location

By default generated code will not be directly visible and just be part of the compilation. 
Should it be desired to have the generated files put in a specific location adding the following properties to the .csproj file will achive it:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>OUTPUT/LOCATION</CompilerGeneratedFilesOutputPath>
``` 
