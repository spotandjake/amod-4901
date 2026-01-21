# This is a list of semantic checks we will need to implement
* Program
  * Program Includes `Program.main`
    * A class named `Program` containing a method main.
    * The method takes no parameters.
  * No location references a property in a class.
  * Every class reference exists
* Arrays
  * Any array size is positive.
  * If the array size is known
    * Validate the size makes sense
* Scoping
  * Variables must be declared before used.
  * Methods can only be called by code appearing after the header
  * A class can only be used after it's declaration.

<!-- TODO: Collect more rules -->