# Scoop Plugin for PowerToys Run [![Mentioned in Awesome PowerToys Run Plugins](https://awesome.re/mentioned-badge.svg)](https://github.com/hlaueriksson/awesome-powertoys-run-plugins)

This is a plugin for [PowerToys Run](https://github.com/microsoft/PowerToys/wiki/PowerToys-Run-Overview) that allows you to search, install, update and uninstall packages from the [Scoop](https://scoop.sh/) package manager.

![banner](https://github.com/Quriz/PowerToysRunScoop/assets/75581292/4b76a285-aff2-4515-adab-9ed4ab37fa13)

> [!IMPORTANT]
> This plugin currently doesn't work with PowerToys v0.87.0 or later. An update is in progress.

## Features

- Search for packages in all Scoop repositories
- Install, update and uninstall packages directly from PowerToys Run
- View package description
- Go to package homepage

## Installation

1. Make sure you have Scoop installed. If not, get it [here](https://scoop.sh/).
2. Download the latest release of the plugin from the [releases page](https://github.com/Quriz/PowerToysRunScoop/releases/latest).
3. Extract the zip file's contents to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`.
4. Restart PowerToys.

## Usage

1. Open PowerToys Run (default shortcut is `Alt+Space`).
2. Type `sc` followed by your search query.
3. Select a package from the search results and press `Enter` to install it.
4. Press `Ctrl+H` to open the homepage of the package.
5. When a package is installed, press `Ctrl+U` to update it or `Ctrl+D` to uninstall it.

## How to build

1. Run `copyLib.ps1` once to copy the necessary DLLs from PowerToys to the project.
2. Build the project!

## How to debug

1. Build the project.
2. Run `debug.ps1`.
3. Attach to the process `PowerToys.PowerLauncher`.

## Thanks to

- [lukesampson](https://github.com/lukesampson): For creating Scoop!
- [Microsoft](https://github.com/microsoft): For creating PowerToys Run!
- [bostrot](https://github.com/bostrot): I used his [PowerToysRunPluginWinget](https://github.com/bostrot/PowerToysRunPluginWinget/tree/main) repository as a base.
- [8LWXpg](https://github.com/8LWXpg): For providing a better [project structure](https://github.com/8LWXpg/PowerToysRun-PluginTemplate).
