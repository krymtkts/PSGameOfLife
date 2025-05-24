namespace PSGameOfLife

open System
open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Start, "GameOfLife")>]
type StartGameOfLifeCommand() =
    inherit Cmdlet()

    // NOTE: disable board sizing for CUI.
    // member val Width = 120 with get, set
    // member val Height = 40 with get, set

    // NOTE: use for random initialization only.
    [<Parameter(Mandatory = false, HelpMessage = "Fate roll for the cell.")>]
    [<ValidateRange(0.1, 0.5)>]
    member val FateRoll = Algorithm.defaultFateRoll with get, set

    [<Parameter(Mandatory = false, HelpMessage = "Interval millisecond for the game.")>]
    [<ValidateRange(0, 1000)>]
    member val IntervalMs = UI.Character.defaultInterval with get, set

    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()

    override __.EndProcessing() =
        use screen = new UI.Character.Screen()
        let initializer = Algorithm.random __.FateRoll
        let board = Core.createBoard initializer screen.Width screen.Height __.IntervalMs
        UI.Character.game screen board |> Async.RunSynchronously
