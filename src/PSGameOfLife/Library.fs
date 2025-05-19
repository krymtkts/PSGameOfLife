namespace PSGameOfLife

open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Start, "GameOfLife")>]
type StartGameOfLifeCommand() =
    inherit Cmdlet()

    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()
    override __.EndProcessing() = ()
