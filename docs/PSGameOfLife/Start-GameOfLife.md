---
document type: cmdlet
external help file: PSGameOfLife.dll-Help.xml
HelpUri: https://github.com/krymtkts/PSGameOfLife/blob/main/docs/PSGameOfLife/Start-GameOfLife.md
Module Name: PSGameOfLife
ms.date: 06-08-2025
PlatyPS schema version: 2024-05-01
---

# Start-GameOfLife

## SYNOPSIS

Starts an interactive Conway's Game of Life simulation in the console.

## SYNTAX

### Default (Default)

```
Start-GameOfLife [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### \_\_AllParameterSets

```
Start-GameOfLife [-FateRoll <double>] [-IntervalMs <int>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Runs Conway's Game of Life in the console window.
The board is randomly initialized. The simulation shows each generation in the console.
Press 'Q' to quit the simulation at any time.
You can control the randomness of the initial state. You can also set the interval between generations using parameters.

## EXAMPLES

### Example 1

```powershell
PS C:\> Start-GameOfLife -FateRoll 0.25 -IntervalMs 200
```

This command starts the Game of Life. Each cell has a 25% chance to be alive at the start. The board updates every 200 milliseconds.

## PARAMETERS

### -FateRoll

Specifies the probability (between 0.1 and 0.5).
This value sets the chance that each cell is alive at the start of the simulation.
Lower values create sparser initial boards. Higher values create denser ones.

```yaml
Type: System.Double
DefaultValue: ""
SupportsWildcards: false
ParameterValue: []
Aliases: []
ParameterSets:
  - Name: (All)
    Position: Named
    IsRequired: false
    ValueFromPipeline: false
    ValueFromPipelineByPropertyName: false
    ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ""
```

### -IntervalMs

Specifies the interval in milliseconds between each generation update (between 0 and 1000).
The default is 100 ms. Increase this value to slow down the simulation.

```yaml
Type: System.Int32
DefaultValue: ""
SupportsWildcards: false
ParameterValue: []
Aliases: []
ParameterSets:
  - Name: (All)
    Position: Named
    IsRequired: false
    ValueFromPipeline: false
    ValueFromPipelineByPropertyName: false
    ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ""
```

### -ProgressAction

This follows the PowerShell standard.
This parameter has no effect in this version.

```yaml
Type: ActionPreference
DefaultValue: None
SupportsWildcards: false
ParameterValue: []
Aliases:
  - proga
ParameterSets:
  - Name: (All)
    Position: Named
    IsRequired: false
    ValueFromPipeline: false
    ValueFromPipelineByPropertyName: false
    ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ""
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS

- [Conway's Game of Life - Wikipedia](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life)
