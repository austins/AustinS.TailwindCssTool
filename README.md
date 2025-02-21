# Tailwind CSS .NET Tool

A .NET tool to install the standalone Tailwind CSS CLI, and build or watch a CSS file.

## Usage

### Installation
- Local from a project directory: `dotnet new tool-manifest` then `dotnet tool install --local AustinS.TailwindCssTool`
- Global: `dotnet tool install --global AustinS.TailwindCssTool`

### Commands
You can use `dotnet tool run tailwindcss` or the shorthand `dotnet tailwindcss`.

- `dotnet tailwindcss install` Install the Tailwind CSS binary.
  - (Optional) `-t` or `--tailwind-version` The version of Tailwind CSS to install (e.g. v4.0.0, v3.4.17). If not specified, the latest is installed.
  - (Optional) `-o` or `--overwrite` Whether to overwrite an existing Tailwind CSS binary.
- `dotnet tailwindcss build` Generate Tailwind CSS output.
    - `-i` or `--input` The input CSS file path.
    - `-o` or `--output` The output CSS file path.
    - (Optional) `-m` or `--minify` Whether to minify the output CSS.
- `dotnet tailwindcss watch` Watch for changes and generate Tailwind CSS output on any change.
    - `-i` or `--input` The input CSS file path.
    - `-o` or `--output` The output CSS file path.
    - (Optional) `-m` or `--minify` Whether to minify the output CSS.

### Generate Tailwind CSS File on Build

Here is an example target you can place in your web project's csproj file to run the build command on every build:
```
<Target Name="RestoreAssets" AfterTargets="BeforeBuild">
    <Exec Command="dotnet tool restore"/>
    <Exec Command="dotnet tool run tailwindcss install -t v4.0.7"/>
    <Exec Command="dotnet tool run tailwindcss build -i <RELATIVE PATH TO YOUR TAILWIND CSS FILE HERE> -o <RELATIVE PATH TO THE OUTPUT CSS FILE HERE> --minify"/>
</Target>
```

It is recommended to set the Tailwind CSS version so that builds will be consistent across environments and not commit the output file (instead, have that generated during CI). Be sure to change the input and output paths.
