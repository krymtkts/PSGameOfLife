namespace PSGameOfLife

open System
open System.Management.Automation

[<AutoOpen>]
module Data =

    [<RequireQualifiedAccess>]
    [<NoEquality>]
    [<NoComparison>]
    [<Struct>]
    type UIMode =
        | Cui
        | Gui

    [<RequireQualifiedAccess>]
    module UIMode =
        let fromSwitch (switch: SwitchParameter) =
            switch.IsPresent
            |> function
                | true -> UIMode.Gui
                | false -> UIMode.Cui

[<Cmdlet(VerbsLifecycle.Start, "GameOfLife", DefaultParameterSetName = "Cui")>]
[<OutputType(typeof<unit>)>]
type StartGameOfLifeCommand() =
    inherit Cmdlet()

    // NOTE: use for random initialization only.
    [<Parameter(Mandatory = false, HelpMessage = "Fate roll for the cell.", ParameterSetName = "Cui")>]
    [<Parameter(Mandatory = false, HelpMessage = "Fate roll for the cell.", ParameterSetName = "Gui")>]
    [<ValidateRange(0.1, 0.5)>]
    member val FateRoll = Algorithm.defaultFateRoll with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Interval millisecond for the game.", ParameterSetName = "Cui")>]
    [<Parameter(Mandatory = false, HelpMessage = "Interval millisecond for the game.", ParameterSetName = "Gui")>]
    [<ValidateRange(0, 1000)>]
    member val IntervalMs = View.Character.defaultInterval with get, set

    [<Parameter(Mandatory = false, HelpMessage = "GUI mode for the game.", ParameterSetName = "Gui")>]
    member val GuiMode: SwitchParameter = SwitchParameter(false) with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Width for the GUI.", ParameterSetName = "Gui")>]
    [<ValidateRange(10, 1000)>]
    member val Width = 50 with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Height for the GUI.", ParameterSetName = "Gui")>]
    [<ValidateRange(10, 1000)>]
    member val Height = 50 with get, set

    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()

    override __.EndProcessing() =
        __.GuiMode
        |> UIMode.fromSwitch
        |> function
            | UIMode.Cui ->
                use screen = new View.Character.Screen()
                let initializer = Algorithm.random __.FateRoll
                let board = Core.createBoard initializer screen.Column screen.Row __.IntervalMs
                View.Character.game screen board
            | UIMode.Gui ->
                use screen = new View.Avalonia.Screen(__.Width, __.Height)
                let initializer = Algorithm.random __.FateRoll
                let board = Core.createBoard initializer screen.Column screen.Row __.IntervalMs
                View.Avalonia.game screen board
