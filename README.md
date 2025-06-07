# PSGameOfLife

![Top Language](https://img.shields.io/github/languages/top/krymtkts/PSGameOfLife?color=%23b845fc)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

PSGameOfLife is a PowerShell module written in F#.
It runs [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) interactively in your console.

Tested on PowerShell 7.4 or later for both Windows and Ubuntu.

## Features

- Interactive [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) simulation in the terminal
- Customizable initial randomness and update interval
- Simple PowerShell cmdlet interface
- CUI available, GUI planned

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

Start the Game of Life simulation with the default settings:

```powershell
Start-GameOfLife
```

Press `Q` during the simulation to quit.

You can customize the initial randomness and update interval:

```powershell
Start-GameOfLife -FateRoll 0.2 -IntervalMs 200
```

- `-FateRoll` sets the probability (0.1 ~ 0.5) that each cell is alive at the start.
- `-IntervalMs` sets the interval in milliseconds between generations (default: 100).

## License

The MIT License applies to this project. For details, see the [LICENSE](./LICENSE) file.

## Links

- [Conway's Game of Life - Wikipedia](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life)
