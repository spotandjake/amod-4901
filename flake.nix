{
  inputs = {
    flakelight.url = "github:nix-community/flakelight";
  };
  outputs = { flakelight, grain, ... }:
    flakelight ./. ({ lib, ... }: {
      systems = lib.systems.flakeExposed;
      devShell = {
        packages = pkgs: [
          pkgs.dotnet-sdk # .NET SDK
          pkgs.jdk25 # Java 25
          pkgs.antlr # ANTLR4 tool
          pkgs.go-task # task command - script runner
        ];
        # Put java on the path
        shellHook = pkgs: ''
          export JAVA_HOME=${pkgs.jdk25}
          PATH="${pkgs.jdk25}/bin:$PATH"
          PATH="${pkgs.antlr}/bin:$PATH"
        '';
      };
    });
}