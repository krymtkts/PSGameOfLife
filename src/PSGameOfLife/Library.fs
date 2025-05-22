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
        let random = Random()

        let height = Console.WindowHeight - 3 - 2 // TODO: capsule into the screen.

        let board =
            Core.createBoard
                (fun _ _ -> if random.NextDouble() < 0.41 then Core.Live else Core.Dead)
                Console.WindowWidth
                height

        use screen = new UI.Character.Screen(Console.WindowWidth, height)

        UI.Character.game screen board |> Async.RunSynchronously
