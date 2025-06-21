module PSGameOfLife.View.Character

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
    let width = LanguagePrimitives.Int32WithMeasure<col> Console.WindowWidth

    let height =
        Console.WindowHeight - heightOfHeader - heightForAdjustment
        |> LanguagePrimitives.Int32WithMeasure<row>

    let originalOut = Console.Out

    let writer =
        let sw = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding)

        sw.AutoFlush <- false
        sw |> Console.SetOut
        sw

    let screenHeight = int height + heightOfHeader

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

            // NOTE: get_CursorPosition doesn't work on Linux, so we cannot backup the cursor position. We can only set it back to true directly.
            Console.CursorVisible <- true

    member __.Write(s: string) = s |> Console.Write
    member __.WriteLine(s: string) = s |> Console.WriteLine
    member __.EmptyWriteLine() = Console.WriteLine()
    member __.Flush() = writer.Flush()
    member __.GetCursorPosition() = Console.GetCursorPosition().ToTuple()
    member __.SetCursorPosition(x: int, y: int) = (x, y) |> Console.SetCursorPosition
    member __.KeyAvailable() = Console.KeyAvailable
    member __.ReadKey() = Console.ReadKey(true)

    member __.Column = width
    member __.Row = height
    member __.Diff = diff

let toSymbol cell =
    match cell with
    | Dead -> " "
    | Live -> "X"

let inline render<'Screen
    when 'Screen: (member GetCursorPosition: unit -> int * int)
    and 'Screen: (member Diff: int)
    and 'Screen: (member Write: string -> unit)
    and 'Screen: (member WriteLine: string -> unit)
    and 'Screen: (member EmptyWriteLine: unit -> unit)
    and 'Screen: (member Flush: unit -> unit)
    and 'Screen: (member SetCursorPosition: int * int -> unit)>
    (screen: 'Screen)
    (board: Board)
    =
    let pos = screen.GetCursorPosition()
    let info = $"#Press Q to quit. Board: %d{board.Column} x %d{board.Row}"
#if DEBUG
    // NOTE: additional info for debugging.
    let info = $"%s{info} Position: %A{pos} Diff: %d{screen.Diff}"
#endif
    info |> screen.WriteLine

    $"#Generation: %10d{board.Generation} Living: %10d{board.Lives}"
    |> screen.WriteLine

    screen.EmptyWriteLine()

    board.Cells
    |> Array2D.iteri (fun y x cell ->
        toSymbol cell
        |> if x + 1 < int board.Column then
               screen.Write
           else
               screen.WriteLine)

    screen.Flush()
    pos |> screen.SetCursorPosition

let inline stopRequested<'Screen
    when 'Screen: (member KeyAvailable: unit -> bool) and 'Screen: (member ReadKey: unit -> ConsoleKeyInfo)>
    (screen: 'Screen)
    =
    if screen.KeyAvailable() then
        let key = screen.ReadKey()
        if key.Key = ConsoleKey.Q then true else false
    else
        false

[<Literal>]
let defaultInterval = 100<ms>

let inline game (screen: ^Screen) (board: Board) =
    async {
        let mutable b = board

        while not <| stopRequested screen do
            render screen b
            do! Async.Sleep(int board.Interval)
            b <- b |> nextGeneration
    }
    |> Async.RunSynchronously
