# NetDoc
## Understand your accidental public API

If you have a good API, you won't need this tool.  However, if you have to maintain a library with:
 - a ton of public classes/methods that should have been private
 - several consumers that you're afraid of breaking
 
...then this may be the tool for you.

## How it works

NetDoc can automatically generate contract assertions by analysing .NET binaries.
Rather than listing the entire public surface area of your dll, it documents only what is actually used by a consumer.
It does this by creating contract assertion classes, which will cause tools such as CodeLens to show usages of your code.
This means you should be able to change/remove unused code safely, as long as you don't have to change the assertions.

## Command line syntax

Switch             | Description
:------------------|:-------------------
`--referencingDir` | The folder containing your referencing dlls.
`--referencedFile` | Your referencing dll.
`--excludeDir`     | Files in the referencing dir will be excluded if there is a file with the same name in the exclude dir.
`--outDir`         | Where to output the .cs files containing the contract assertions.  This switch should only be used once.

## Example usage

```
netdoc.exe --referencingDir MyProduct\Bin\Debug `
           --referencingDir AnotherProduct\Bin\Debug `
           --referencedFile Library\Bin\Debug\Library.dll `
           --outDir LibraryTests\ContractAssertions
```
The above command would scan 2 directories for referencing dll/exe files and document all usages of the public API of `Library.dll`.  It would then output `MyProduct.cs` and `AnotherProduct.cs` contract classes, which break down the usages into a method for each assembly (in those product folders) that consumes the library.

## Build/test

![Build + run tests](https://github.com/samblackburn/NetDoc/workflows/Build%20+%20run%20tests/badge.svg)

No build dependencies - should be easy to checkout, `dotnet build` and run the tests in this repo.  I think NetDoc ought to work on Linux but the tests depend on compilers that aren't on the default Linux agent.  The tests do a lot of compilation, so take around 20 seconds each!
