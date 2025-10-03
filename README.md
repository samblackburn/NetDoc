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

Let's suppose you have a library with a test project, and a consumer that we imagine is on the other side of a NuGet boundary:

```bash
dotnet new classlib -o Library
echo "public class ShouldBePrivate{ public void DoNotUse() {} }" > Library/ShouldBePrivate.cs

dotnet new classlib -o MyConsumer
dotnet add ./MyConsumer/MyConsumer.csproj reference Library/Library.csproj
echo "public class NaughtyConsumer{ public void Foo() { new ShouldBePrivate().DoNotUse(); } }" > MyConsumer/NaughtyConsumer.cs

dotnet new classlib -o LibraryTests
dotnet add ./LibraryTests/LibraryTests.csproj reference Library/Library.csproj

rm */Class1.cs

dotnet build Library
dotnet build LibraryTests
dotnet test LibraryTests
dotnet build MyConsumer
```
If we make a breaking change (e.g. `dotnet format Library --severity info` would make a method static), we won't know that we're breaking anyone. However, NetDoc can help:
```
NetDoc.exe \
  --referencingDir MyConsumer \
  --referencedFile Library/Bin/Debug/net9.0/Library.dll \
  --outDir LibraryTests/ContractAssertions
```
The above command will create a `MyConsumerContractAssertions` class which documents the sneaky usage of the class `ShouldBePrivate`:
```c#
    private void UsedByMyConsumer()
    {
        CheckReturnType<ShouldBePrivate>(new ShouldBePrivate());
        Create<ShouldBePrivate>().DoNotUse();
    }
```
This means `Library` maintainers will immediately see that `ShouldBePrivate` is in use and its API should not be changed without notifying the maintainers of the consuming codebase.

Once all usages are represented inside a solution, refactorings like removing unused code become a lot easier. I wrote a [`RemoveUnused`](https://github.com/samblackburn/RemoveUnused) routine for this purpose.

## Build/test

![Build + run tests](https://github.com/samblackburn/NetDoc/workflows/Build%20+%20run%20tests/badge.svg)

No build dependencies - should be easy to checkout, `dotnet build` and run the tests in this repo.  I think NetDoc ought to work on Linux but the tests depend on compilers that aren't on the default Linux agent.  The tests do a lot of compilation, so take around 20 seconds each!
