module PSGameOfLife.UI.Character

open System
open System.IO

open PSGameOfLife.Core

type Screen() =
    [<Literal>]
    let heightOfHeader = 3

    // NOTE: this is a hack to render at the same position without causing scrolling.
    [<Literal>]
    let heightForAdjustment = 2

    let startY = Console.GetCursorPosition().ToTuple() |> snd
    let width = Console.WindowWidth
    let height = Console.WindowHeight - heightOfHeader - heightForAdjustment
    let cursorVisible = Console.CursorVisible
    let originalOut = Console.Out

    let writer =
        let sw = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding)

        sw.AutoFlush <- false
        sw |> Console.SetOut
        sw

    let screenHeight = height + heightOfHeader

    let diff, remaining =
        let remaining = Console.WindowHeight - startY

        // NOTE: 1 is a hack to render at the same position without causing scrolling.
        screenHeight - remaining + 1
        |> function
            | diff when diff > 0 -> diff, remaining
            | _ -> 0, 0

    do
        Console.CursorVisible <- false
        // NOTE: add lines to the end of the screen for scrolling using the PSReadLine method.
        String.replicate (diff + remaining) Environment.NewLine |> Console.Write

        writer.Flush()
        // NOTE: 1 is a hack to render at the same position without causing scrolling.
        (0, startY - (if diff = 0 then 0 else diff + 1)) |> Console.SetCursorPosition

    interface IDisposable with
        member __.Dispose() =
            let pos = Console.GetCursorPosition().ToTuple()

            String.replicate Console.WindowWidth " "
            |> Array.replicate screenHeight
            |> Array.iter Console.WriteLine

            writer.Flush()
            pos |> Console.SetCursorPosition

            Console.SetOut originalOut
            writer.Dispose()

            Console.CursorVisible <- cursorVisible

    member __.Write(s: string) = s |> Console.Write
    member __.WriteLine() = Console.WriteLine()
    member __.WriteLine(s: string) = s |> Console.WriteLine
    member __.Flush() = writer.Flush()
    member __.Width = width
    member __.Height = height

    member val Diff = diff

let toSymbol cell =
    match cell with
    | Dead -> " "
    | Live -> "X"

let render (screen: Screen) (board: Board) =
    let pos = Console.GetCursorPosition().ToTuple()

    $"Press Q to quit. Board: %d{board.Width}x%d{board.Height} Position: %A{pos} Height: %d{Console.WindowHeight} Diff: %d{screen.Diff}"
    |> screen.WriteLine

    $"#Generation: %d{board.Generation} Living: %d{board.Lives} "
    |> screen.WriteLine

    screen.WriteLine()

    board.Cells
    |> Array2D.iteri (fun y x cell ->
        toSymbol cell
        |> if x + 1 < board.Width then
               screen.Write
           else
               screen.WriteLine)

    screen.Flush()
    pos |> Console.SetCursorPosition

let stopRequested () =
    if Console.KeyAvailable then
        let key = Console.ReadKey(true)
        if key.Key = ConsoleKey.Q then true else false
    else
        false

[<TailCall>]
let rec game screen board =
    async {
        render screen board
        do! Async.Sleep 100

        match stopRequested () with
        | true -> return ()
        | false -> return! board |> nextGeneration |> game screen
    }
