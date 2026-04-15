# README

This directory contains high level overviews for the various stages of the compiler. More detailed documentation can be found in the code itself.

## API

The compiler itself is exposed in `decaf/Compiler.cs` as a static class with simple functions so the compiler can be consumed by other projects as a library. The main compilation function is `CompileString` which takes in a string of source code and returns a `WasmModule`. There are also functions for compiling from a file and for compiling to a file.

Careful work is taken to ensure that the compiler itself is not dependent on any particular runtime or environment, so all IO operations are done by the library consumer such as the `CLI`, this makes the compiler more flexible and easier to test.

### Stages

The compiler itself is broken into three stages:
* Frontend - Responsible for converting the source code to a `ParseTree`
* Middleend - Responsible for converting the `ParseTree` to a `AnfTree`
* Backend - Responsible for converting the `AnfTre` to a `WasmModule`

