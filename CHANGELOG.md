# Changelog

This file records all notable changes to this project.

This changelog uses the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

## [Unreleased]

## [0.0.2] - 2025-07-27

### Added

- Add Avalonia-based GUI mode for `Start-GameOfLife` with the `-GuiMode` switch.
- Add `-CellSize`, `-Width`, and `-Height` parameters to configure GUI cell size and board dimensions.
- Add GUI status text that displays board dimensions and generation information.
- Add a `Q` keyboard shortcut to close GUI mode on supported platforms.

### Changed

- Improve CUI and GUI rendering performance.
- Update documentation to describe GUI mode, screenshots, and Linux exit behavior.

### Fixed

- Fix next generation calculation.
- Improve GUI update cancellation and window responsiveness.
- Disable the GUI `Q` keyboard shortcut on Linux to avoid leaving the window open after exit.

## [0.0.1] - 2025-06-08

### Added

- Add the `Start-GameOfLife` cmdlet to run Conway's Game of Life in the console.
- Add `-FateRoll` and `-IntervalMs` parameters to configure the simulation.
- Add PowerShell help and readme documentation for installation and usage.

### Notes

- This is the initial release of `PSGameOfLife`.
- Supported PowerShell versions are 7.4 and higher.
- The module targets .NET 8.

---

[Unreleased]: https://github.com/krymtkts/PSGameOfLife/compare/v0.0.2...HEAD
[0.0.2]: https://github.com/krymtkts/PSGameOfLife/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/krymtkts/PSGameOfLife/releases/tag/v0.0.1
