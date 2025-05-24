namespace PSGameOfLife

open System
open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Start, "GameOfLife")>]
type StartGameOfLifeCommand() =
    inherit Cmdlet()

    // NOTE: disable board sizing for CUI.
    // member val Width = 120 with get, set
    // member val Height = 40 with get, set
    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()

    override __.EndProcessing() =
        use screen = new UI.Character.Screen()

        let board =
            let random = Random()

            Core.createBoard
                (fun _ _ -> if random.NextDouble() < 0.41 then Core.Live else Core.Dead)
                screen.Width
                screen.Height

        UI.Character.game screen board |> Async.RunSynchronously
