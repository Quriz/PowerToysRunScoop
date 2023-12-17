# Scoop Plugin for PowerToys Run

This is a plugin for [PowerToys Run](https://github.com/microsoft/PowerToys/wiki/PowerToys-Run-Overview) that allows you to search and install packages from the [Scoop](https://scoop.sh/) package manager.

![banner](https://github.com/Quriz/PowerToysRunScoop/assets/75581292/4b76a285-aff2-4515-adab-9ed4ab37fa13)

## Features

- Search for packages in all Scoop repositories
- Install, update, and uninstall packages directly from PowerToys Run
- View package description
- Go to package homepage

## Installation

1. Download the latest release of the plugin from the [releases page](https://github.com/Quriz/PowerToysRunScoop/releases/latest).
2. Extract the zip file's contents to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`.
3. Restart PowerToys.

## Usage

1. Open PowerToys Run (default shortcut is `Alt+Space`).
2. Type `sc` followed by your search query.
3. Select a package from the search results and press `Enter` to install it.

## Build

Summarized setup commands for steps 1-6:
```cmd
git clone https://github.com/microsoft/PowerToys
cd PowerToys
git submodule update --init --recursive
git clone https://github.com/Quriz/PowerToysRunScoop PowerToys/src/modules/launcher/Plugins/Community.PowerToys.Run.Plugin.Scoop
dotnet sln PowerToys.sln add --solution-folder .\modules\launcher\Plugins .\src\modules\launcher\Plugins\Community.PowerToys.Run.Plugin.Scoop\Community.PowerToys.Run.Plugin.Scoop.csproj
```

1. Clone the [PowerToys repo](https://github.com/microsoft/PowerToys).
2. cd into the `PowerToys` directory.
3. Initialize the submodules: `git submodule update --init --recursive`
4. Clone this repo into the `PowerToys/src/modules/launcher/Plugins` directory.
5. Open the `PowerToys.sln` solution in Visual Studio.
6. Add this project to the `PowerToys.sln` solution under the path `PowerToys/src/modules/launcher/Plugins`.
7. Add the `ApiKey` to the [`Scoop.cs`](https://github.com/Quriz/PowerToysRunScoop/blob/main/Scoop.cs#L61) file. This is the API key used to connect to Scoop's API from [`scoop.sh`](https://scoop.sh/). You can find it there with a bit of dev tools magic.
8. Build the solution.
9. Run the `PowerToys` project.

# Thanks to

- [bostrot](https://github.com/bostrot): I used his [PowerToysRunPluginWinget](https://github.com/bostrot/PowerToysRunPluginWinget/tree/main) repository as a base.
- Microsoft: For creating PowerToys Run!
