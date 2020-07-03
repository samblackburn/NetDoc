# NetDoc
## Understand your accidental public API

If you have a good API, you won't need this tool.  However, if you have to maintain a library with:
 - a ton of public classes/methods that should have been private
 - several consumers that you're afraid of breaking
 
...then read on!

## How it works

NetDoc can automatically generate contract assertions by analysing .NET binaries.
Rather than listing the entire public surface area of your dll, it documents only what is actually used by a consumer.
It does this by creating contract assertion classes, which will cause tools such as CodeLens to show usages of your code.
This means you should be able to change/remove unused code safely, as long as you don't have to change the assertions.

## Command line syntax

Switch                        | Description
:-----------------------------|:-------------------
`--referencingDir C:\FooDir`  | The folder containing your referencing dlls.
`--referencedFile C:\Bar.dll` | Your referencing dll.
`--excludeDir C:\BazDir`      | Files in the referencing dir will be excluded if there is a file with the same name in the exclude dir.
`--outDir C:\OutDir`          | Where to output the .cs files containing the contract assertions.  This switch should only be used once.
