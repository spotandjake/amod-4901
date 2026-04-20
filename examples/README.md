# Examples

This directory contains various basic examples of how to use the `decaf` language. Each example is a standalone program that demonstrates a specific feature or concept of the language. You can run these examples to see how `decaf` works in practice and to learn how to write your own `decaf` programs.

All of the examples can be run using `task all`.

## Hello World

The first example is the hello world program, which like any other programming language, is the simplest program you can effectively write. It demonstrates how to print a message to the console. This can be run using `task HelloWorld`.

## Loops

This example demonstrates how you can use both loops and arrays in `decaf`. It demonstrates creating an array, filling it with integers and then printing the contents of the array to the console. This can be run using `task Loops`.

## Fibonacci

This example demonstrates working with functions in `decaf`. It defines a recursive function to calculate the Fibonacci sequence and then prints the result of an invocation. This can be run using `task Fibonacci`.

## Functions

This example is a slightly more complex example where we demonstrate the first class nature of functions in `decaf`. It defines a function `arrayForEach` which works like you would expect `Array.forEach` in other languages to work, it takes a callback and an array and iterates the array applying the callback to each function. In the example we demonstrate printing the array. This can be run using `task Functions`.

## Random

This example is probably the most complex out of the bunch but also demonstrates the power provided by `decaf`. It demonstrates using [WASI](https://wasi.dev/) to access the system's random number generator and then generates a few random numbers and prints them to the console. This can be run using `task Random`.

This example is rather small but demonstrates how external wasm imports can be used, WASI while low level is a very powerful API and allows you to do a lot of things that you would expect from a normal programming language, such as printing, reading files, accessing the network and much more, all of which is already available to you in `decaf` as long as you have the right imports. Additionally these wasm exports can be used with custom hosts including ones you define yourself, so you can use `decaf` to integrate with other languages and systems in a very seamless way even though the language itself is very small and simple.
