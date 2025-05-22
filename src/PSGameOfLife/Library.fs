namespace PSGameOfLife

open System
open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Start, "GameOfLife")>]
type StartGameOfLifeCommand() =
    inherit Cmdlet()

    member val Width = 120 with get, set
    member val Height = 40 with get, set
    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()

    override __.EndProcessing() =
        let random = Random()

        let board =
            Core.createBoard (fun _ _ -> if random.NextDouble() < 0.41 then Core.Live else Core.Dead) __.Width __.Height

        use screen = new UI.Character.Screen(__.Width, __.Height)

        UI.Character.game screen board |> Async.RunSynchronously
