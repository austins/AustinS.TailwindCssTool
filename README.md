# Tailwind CSS .NET Tool

A .NET tool to install the standalone [Tailwind CSS](https://tailwindcss.com/) CLI, and build or watch a CSS file.

## Usage

### Installation
- Local from a project directory: `dotnet new tool-manifest` then `dotnet tool install --local AustinS.TailwindCssTool`
- Global: `dotnet tool install --global AustinS.TailwindCssTool`

### Commands
You can use `dotnet tool run tailwindcss` or the shorthand `dotnet tailwindcss`.

- `dotnet tailwindcss build` Generate Tailwind CSS output.
    - `-i` or `--input` The input CSS file path.
    - `-o` or `--output` The output CSS file path.
    - (Optional) `-m` or `--minify` Whether to minify the output CSS.
    - (Optional) `-t` or `--tailwind-version` The version of Tailwind CSS to install (e.g. v4.0.0, v3.4.17). If not specified, the latest is installed.
- `dotnet tailwindcss watch` Watch for changes and generate Tailwind CSS output on any change.
    - `-i` or `--input` The input CSS file path.
    - `-o` or `--output` The output CSS file path.
    - (Optional) `-m` or `--minify` Whether to minify the output CSS.
    - (Optional) `-t` or `--tailwind-version` The version of Tailwind CSS to install (e.g. v4.0.0, v3.4.17). If not specified, the latest is installed.

If the latest or specified Tailwind CSS version is already installed on the system, it will be used. If there are any failures fetching a version, the latest installed version, if any, will be used. 

### Input CSS File for Tailwind CSS
For Tailwind CSS v4+, you can create a CSS file containing the following in your web project:
```css
@import "tailwindcss";
```
You can then use the path of this file as the input argument for the commands above.

### Generate Tailwind CSS File on Build

Here is an example target you can place in your web project's csproj file to run the build command on every build:
```
<Target Name="GenerateTailwindCss" AfterTargets="BeforeBuild">
    <Exec Command="dotnet tool restore"/>
    <Exec Command="dotnet tool run tailwindcss build -t v4.0.8 -m -i <PATH TO YOUR TAILWIND CSS FILE> -o <PATH THE OUTPUT CSS FILE WILL SAVE TO>"/>
</Target>
```

It is recommended to set the Tailwind CSS version so that builds will be consistent across environments and not commit the output file (instead, have that generated during CI). Be sure to change the input and output paths. See [the Tailwind CSS repo](https://github.com/tailwindlabs/tailwindcss/releases/latest) to find out the latest standalone CLI version number.

### Development Watcher

See the [ExampleWebApp](https://github.com/austins/AustinS.TailwindCssTool/tree/main/src/AustinS.TailwindCssTool.ExampleWebApp) in the repository for an example of how to create a background service that uses the tool to watch for style changes during development.
