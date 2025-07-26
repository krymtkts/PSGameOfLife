# PSGameOfLife

![Top Language](https://img.shields.io/github/languages/top/krymtkts/PSGameOfLife?color=%23b845fc)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

PSGameOfLife is a PowerShell module written in F#.
It runs [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) interactively in your console.

Tested on PowerShell 7.4 or later with Windows Terminal on both Windows and Ubuntu.
Visual Studio Code terminal appears to have a higher load than Windows Terminal.

![capture](./docs/images/psgameoflife.gif)

## Features

- Interactive [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) simulation in the terminal
- Customizable initial randomness and update interval
- Simple PowerShell cmdlet interface
- CUI and GUI, GUI mode is Avalonia-based cross-platform

## Installation

You can install PSGameOfLife from the PowerShell Gallery:

```powershell
# Recommended: PSResourceGet (PowerShellGet 3.0)
Install-PSResource -Name PSGameOfLife

# Or, with PowerShellGet 2.x:
Install-Module -Name PSGameOfLife
```

## Cmdlet Help

See the [Start-GameOfLife.md](./docs/PSGameOfLife/Start-GameOfLife.md) file for detailed cmdlet help.

## Usage

### CUI (Console) mode

```powershell
Start-GameOfLife
```

Press `Q` during the simulation to quit.

You can customize the initial randomness and update interval:

```powershell
Start-GameOfLife -FateRoll 0.2 -IntervalMs 200
```

- `-FateRoll` sets the probability (0.1 ~ 0.5) that each cell is alive at the start
- `-IntervalMs` sets the interval in milliseconds between generations (default: 100)

### GUI mode

In GUI mode, the Game of Life shows in a window using Avalonia (cross-platform).

```powershell
Start-GameOfLife -GuiMode
```

You can adjust the appearance and size of the window with the following parameters:

- `-CellSize` sets pixel size of each cell (1 to 10, default: 10)
- `-Width` : sets number of cells horizontally (default: 50)
- `-Height` : sets number of cells vertically (default: 50)

Example:

```powershell
Start-GameOfLife -GuiMode -CellSize 8 -Width 80 -Height 60
```

You can use `-FateRoll` and `-IntervalMs` in GUI mode as well as in CUI mode.

```powershell
Start-GameOfLife -GuiMode -CellSize 8 -Width 80 -Height 60 -FateRoll 0.2 -IntervalMs 200
```

## License

The MIT License applies to this project. For details, see the [LICENSE](./LICENSE) file.

## Links

- [Conway's Game of Life - Wikipedia](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life)
