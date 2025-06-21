namespace PSGameOfLife

open System
open System.Management.Automation

[<AutoOpen>]
module Data =
    open System.Collections
    open Microsoft.FSharp.Reflection

    let generateArrayOfDu aType = FSharpType.GetUnionCases aType

    let makeUnion<'DU> (u: UnionCaseInfo) = FSharpValue.MakeUnion(u, [||]) :?> 'DU

    let generateDictOfDu<'DU> () =
        let dict = Generic.Dictionary<string, 'DU> StringComparer.InvariantCultureIgnoreCase

        generateArrayOfDu typeof<'DU>
        |> Array.fold
            (fun (acc: Generic.Dictionary<string, 'DU>) u ->
                acc.Add(u.Name, makeUnion<'DU> u)
                acc)
            dict

    let tryGetDu (dict: Generic.Dictionary<string, 'DU>) s =
        let aType: Type = typeof<'DU>

        match dict.TryGetValue s with
        | true, du -> Ok du
        | _ -> Error <| $"Unknown %s{aType.Name} '%s{s}'."

    let private fromString<'DU> (dict: Generic.Dictionary<string, 'DU>) s =
        tryGetDu dict s
        |> function
            | Ok x -> x
            | Error e -> failwith e

    [<RequireQualifiedAccess>]
    [<NoEquality>]
    [<NoComparison>]
    [<Struct>]
    type UIMode =
        | Cui
        | Gui

    [<RequireQualifiedAccess>]
    module UIMode =
        let fromString = generateDictOfDu<UIMode> () |> fromString<UIMode>

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

    [<Parameter(Mandatory = false, HelpMessage = "UI mode for the game.")>]
    [<ValidateSet("Cui", "Gui")>]
    member val UIMode: string = "Cui" with get, set

    override __.BeginProcessing() = ()
    override __.ProcessRecord() = ()

    override __.EndProcessing() =
        __.UIMode
        |> UIMode.fromString
        |> function
            | UIMode.Cui ->
                use screen = new UI.Character.Screen()
                let initializer = Algorithm.random __.FateRoll
                let board = Core.createBoard initializer screen.Width screen.Height __.IntervalMs
                UI.Character.game screen board
            | UIMode.Gui ->
                use screen = new UI.Avalonia.Screen()
                let initializer = Algorithm.random __.FateRoll
                let board = Core.createBoard initializer screen.Width screen.Height __.IntervalMs
                UI.Avalonia.game screen board
