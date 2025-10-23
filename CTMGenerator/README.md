# Code To Model Project Model and Source Code Generator - CTMGenerator

A code-first modeling approach utilizing [NMF](https://nmfcode.github.io/).

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

**ALL** means this path will be used for every namespace which has no __previously__ defined path.

### Generated source code location

By default generated code will not be directly visible and just be part of the compilation. 
Should it be desired to have the generated files put in a specific location adding the following properties to the .csproj file will achive it:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>OUTPUT/LOCATION</CompilerGeneratedFilesOutputPath>
``` 