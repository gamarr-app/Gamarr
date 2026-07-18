{
  description = "Gamarr development shell";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { nixpkgs, ... }:
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
      ];

      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
    in
    {
      devShells = forAllSystems (system:
        let
          pkgs = import nixpkgs { inherit system; };
        in
        {
          default = pkgs.mkShell {
            packages = with pkgs; [
              bash
              dotnet-sdk_10
              git
              gnumake
              icu
              nodejs_22
              openssl
              python3
              sqlite
              yarn
              zlib
            ];

            DOTNET_ROOT = "${pkgs.dotnet-sdk_10}";
            DOTNET_CLI_TELEMETRY_OPTOUT = "1";
            DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1";
            NIX_LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
              pkgs.icu
              pkgs.openssl
              pkgs.sqlite
              pkgs.zlib
            ];

            shellHook = ''
              export PATH="$DOTNET_ROOT/bin:$PATH"
            '';
          };
        });
    };
}
