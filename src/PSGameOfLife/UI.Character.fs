module PSGameOfLife.UI.Character

open System
open System.IO

open PSGameOfLife.Core

type Screen(width: int, height: int) =
    let startY = Console.GetCursorPosition().ToTuple() |> snd
    let originalOut = Console.Out

    let writer =
        let sw = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding)

        sw.AutoFlush <- false
        sw |> Console.SetOut
        sw

    [<Literal>]
    let heightOfHeader = 4

    let diff =
        Console.WindowHeight - height - heightOfHeader - startY
        |> function
            | x when x < 0 -> height + heightOfHeader
            | _ -> 0

    do
        // // NOTE: add lines to the end of the screen for scrolling using the PSReadLine method.
        String.replicate diff Environment.NewLine |> Console.Write

        writer.Flush()
        (0, if diff = 0 then startY else 0) |> Console.SetCursorPosition

    interface IDisposable with
        member __.Dispose() =
            let pos = Console.GetCursorPosition().ToTuple()

            // NOTE: Reset Console.Out before disposing screen to prevent buffer loss.
            Console.SetOut originalOut
            writer.Dispose()

            String.replicate Console.WindowWidth " "
            |> Array.replicate (height + heightOfHeader)
            |> String.concat Environment.NewLine
            |> Console.Write

            pos |> Console.SetCursorPosition

    member __.Write(s: string) = s |> Console.Write
    member __.WriteLine(s: string) = s |> Console.WriteLine
    member __.Flush() = writer.Flush()

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

    "" |> screen.WriteLine

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
