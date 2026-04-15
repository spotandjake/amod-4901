# Command Line Interface

This document exists to provide an overview of the command line interface (CLI) for the decaf compiler.

The CLI is designed to be a simple, minimal and intuitive way to interact with the compiler. It provides a basic set of commands that allow for you to compile your decaf code and run it.


The CLI is the only part of the compiler that uses an external library, [`CommandLineParser`](https://github.com/commandlineparser/commandline) to parse out arguments, options and commands. The decision to use a library for this was not made lightly however there are a lot of subtle edge cases when it comes to providing a clean and intuitive CLI experience and using a library that has been battle tested and develop on it's own for years was the best option. The library is also rather small and extremely specific to the task so it doesn't add a significant amount of bloat to the project.

The CLI is implemented in `decaf/Main.cs` along with the compiler library, and is the entry point for the application. The CLI is completely self contained under the `CLI` namespace.

## Usage

Currently the cli only has one command that takes a series of arguments, instead of duplicating a description of the usage here, you can run either `task` or `decaf --help` to get a description of the usage and options available.