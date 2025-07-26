namespace PSGameOfLife

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

[<Cmdlet(VerbsLifecycle.Start, "GameOfLife", DefaultParameterSetName = "CUI")>]
type StartGameOfLifeCommand() =
    inherit Cmdlet()

    // NOTE: use for random initialization only.
    [<Parameter(Mandatory = false, HelpMessage = "Fate roll for the cell.", ParameterSetName = "CUI")>]
    [<Parameter(Mandatory = false, HelpMessage = "Fate roll for the cell.", ParameterSetName = "GUI")>]
    [<ValidateRange(0.1, 0.5)>]
    member val FateRoll = Algorithm.defaultFateRoll with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Interval millisecond for the game.", ParameterSetName = "CUI")>]
    [<Parameter(Mandatory = false, HelpMessage = "Interval millisecond for the game.", ParameterSetName = "GUI")>]
    [<ValidateRange(0, 1000)>]
    member val IntervalMs = View.Character.defaultInterval with get, set

    [<Parameter(Mandatory = false, HelpMessage = "GUI mode for the game.", ParameterSetName = "GUI")>]
    member val GuiMode: SwitchParameter = SwitchParameter(false) with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Cell size for the GUI.", ParameterSetName = "GUI")>]
    [<ValidateRange(1, 10)>]
    member val CellSize = 10 with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Width for the GUI.", ParameterSetName = "GUI")>]
    [<ValidateRange(10, 1000)>]
    member val Width = 50 with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Height for the GUI.", ParameterSetName = "GUI")>]
    [<ValidateRange(10, 1000)>]
    member val Height = 50 with get, set

    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()

    override __.EndProcessing() =
        let initializer = Algorithm.random __.FateRoll

        __.GuiMode
        |> UIMode.fromSwitch
        |> function
            | UIMode.Cui ->
                use screen = new View.Character.Screen()
                let board = Core.createBoard initializer screen.Column screen.Row __.IntervalMs
                View.Character.game screen board
            | UIMode.Gui ->
                use screen = new View.Avalonia.Screen(__.CellSize, __.Width, __.Height)
                let board = Core.createBoard initializer screen.Column screen.Row __.IntervalMs
                View.Avalonia.game screen board
