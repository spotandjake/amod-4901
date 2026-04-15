# Error Handling

Error handling in the compiler is designed to be robust, informative and built in a way where the compiler can provide much better errors messages in the future.

All actual compiler errors are stored in `decaf/Utils/Errors.cs` and are split up into categories for each stage of the compilation pipeline. Every error uses the same message builder pattern, which would allow us in the future to easily add more information to the error message, such as suggestions on how to fix the errors, proper unique error ids and even more detailed source information such as the surrounding code to the error similar to rust error spans. Currently we just emit the basic error message along with the error position in a format where the user can easily jump to the error in their editor.

We do not provide a list of all the errors here since they are all stored in the same file it is better to check the source of truth so this document cannot get out of date.