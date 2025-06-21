module PSGameOfLife.View.Avalonia

open System
// open System.IO

open PSGameOfLife.Core

open System
open System.Collections
open System.Runtime.InteropServices

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Logging
open Avalonia.Themes.Fluent
open Elmish

module AssemblyHelper =
    let getModuleDir () =
        System.IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location)
        |> function
            | null -> failwith "Could not determine module directory."
            | dir -> dir

    let detectExtension () =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            "dll"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            "so"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            "dylib"
        else
            failwith "Unsupported OS"

    let resolver (ptrCache: Concurrent.ConcurrentDictionary<string, nativeint>) moduleDir extension =
        let tryLoadLibrary (moduleDir: string) (extension: string) (libraryName: string) =
            let libPath =
                let libPath =
                    if extension |> libraryName.EndsWith then
                        $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}"
                    else
                        $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}.{extension}"

                System.IO.Path.Combine(moduleDir, libPath)

            if libPath |> IO.File.Exists then

                libPath
                |> NativeLibrary.TryLoad
                |> function
                    | true, ptr -> ptr
                    | _ -> IntPtr.Zero
            else
                IntPtr.Zero

        DllImportResolver(fun libraryName assembly searchPath ->
            match libraryName |> ptrCache.TryGetValue with
            | true, ptr -> ptr
            | _ ->
                match tryLoadLibrary moduleDir extension libraryName with
                | ptr when ptr = IntPtr.Zero ->
                    // NOTE: fallback to the default behavior if the library is not found.
                    match NativeLibrary.TryLoad(libraryName, assembly, searchPath) with
                    | true, ptr ->
                        ptrCache.TryAdd(libraryName, ptr) |> ignore
                        ptr
                    | _ ->
                        // NOTE: Returning IntPtr.Zero means the library was not found. This will cause an error when P/Invoke is called.
                        IntPtr.Zero
                | ptr ->
                    ptrCache.TryAdd(libraryName, ptr) |> ignore
                    ptr)

    do
        // NOTE: those bindings cannot move out to static, it will cause a deadlock.
        let moduleDir = getModuleDir ()
        let extension = detectExtension ()
        let cache = new Concurrent.ConcurrentDictionary<string, nativeint>()
        let resolver = resolver cache moduleDir extension

        [| typeof<SkiaSharp.SKImageInfo>.Assembly
           typeof<HarfBuzzSharp.Buffer>.Assembly
           typeof<AppBuilder>.Assembly
           typeof<Win32.AngleOptions>.Assembly |]
        |> Array.iter (fun assembly -> NativeLibrary.SetDllImportResolver(assembly, resolver))

module Main =
    open Avalonia.FuncUI
    open Avalonia.Threading

    [<Struct>]
    type State = { Board: Board }

    [<Struct>]
    type Msg = Next

    let update (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | Next ->
            // #if DEBUG
            //             printfn "Next generation requested. Current generation: %d" state.Board.Generation
            // #endif
            { Board = state.Board |> nextGeneration }, Cmd.none

    let view (cellSize: float) (state: State) (dispatch: Msg -> unit) =
        let width = float state.Board.Column * cellSize
        let height = float state.Board.Row * cellSize

        Canvas.create
            [ Canvas.width width
              Canvas.height height
              Canvas.children
                  [ for i in 0 .. (int state.Board.Row * int state.Board.Column - 1) do
                        let row = i / int state.Board.Column
                        let col = i % int state.Board.Column

                        let color =
                            state.Board.Cells.[row, col]
                            |> fun c ->
                                match c with
                                | Dead -> "white"
                                | Live -> "black"

                        yield
                            Rectangle.create
                                [ Shapes.Rectangle.width cellSize
                                  Shapes.Rectangle.height cellSize
                                  Shapes.Rectangle.fill (Media.SolidColorBrush(Avalonia.Media.Color.Parse color))
                                  Canvas.left (float col * cellSize)
                                  Canvas.top (float row * cellSize)
                                  //                                   Shapes.Rectangle.onTapped (fun _ ->
                                  // #if DEBUG
                                  //                                       printfn "Tapped cell at (%d, %d) generation %d" col row state.Board.Generation
                                  // #endif
                                  //                                       Next |> dispatch
                                  // #if DEBUG
                                  //                                       printfn
                                  //                                           "Exit tapped cell at (%d, %d) generation %d"
                                  //                                           col
                                  //                                           row
                                  //                                           state.Board.Generation
                                  // #endif
                                  //                                   )

                                  ]

                    ] ]
        |> generalize

    let subscriptions (ms: float) (_state: State) =
        let timerSub (dispatch: Msg -> unit) =
            let invoke () =
                dispatch Next
                true

            DispatcherTimer.Run(Func<bool>(invoke), TimeSpan.FromMilliseconds ms)

        [ [ nameof timerSub ], timerSub ]

type MainWindow(board: Board) as this =
    inherit HostWindow()

    let cellSize = 10.0

    do
        base.Title <- "PSGameOfLife"
        base.Width <- float board.Column * cellSize
        base.Height <- float board.Row * cellSize
        base.CanResize <- false

#if DEBUG
        printfn "Starting PSGameOfLife with board size %d x %d" board.Column board.Row
#endif
        let init () : Main.State * Cmd<_> = { Board = board }, Cmd.none

        Program.mkProgram init Main.update (Main.view cellSize)
        |> Program.withHost this
        |> Program.withSubscription (Main.subscriptions <| float board.Interval)
        |> Program.run

    override __.OnClosed(e: EventArgs) : unit = base.OnClosed(e)

type App() =
    inherit Application()

    member val mainWindow: MainWindow | null = null with get, set
    member val desktopLifetime: IClassicDesktopStyleApplicationLifetime | null = null with get, set

    override __.Initialize() =
        __.Styles.Add(FluentTheme())
        __.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override __.OnFrameworkInitializationCompleted() =
        match __.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as (desktopLifetime: IClassicDesktopStyleApplicationLifetime) ->
            __.desktopLifetime <- desktopLifetime
        | _ -> ()


type Screen(col: int, row: int) =
    static let app =
        let app =
            let lt = new ClassicDesktopStyleApplicationLifetime()

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                // NOTE: Graphics operations can produce an extremely large amount of log output, which may be useful for debugging.
                // .LogToTextWriter(Console.Out, LogEventLevel.Verbose)
                .SetupWithLifetime(lt)

        app

    member val App = app with get

    interface IDisposable with
        member __.Dispose() =
            let app =
                app.Instance
                |> function
                    | :? App as app -> app
                    | _ -> failwith "Application instance is not of type App."

            match app.mainWindow with
            | null -> ()
            | window -> window.Close()

    member __.Column = LanguagePrimitives.Int32WithMeasure<col> col
    member __.Row = LanguagePrimitives.Int32WithMeasure<row> row

// let toSymbol cell =
//     match cell with
//     | Dead -> " "
//     | Live -> "X"

// let inline render<'Screen
//     when 'Screen: (member GetCursorPosition: unit -> int * int)
//     and 'Screen: (member Diff: int)
//     and 'Screen: (member Write: string -> unit)
//     and 'Screen: (member WriteLine: string -> unit)
//     and 'Screen: (member EmptyWriteLine: unit -> unit)
//     and 'Screen: (member Flush: unit -> unit)
//     and 'Screen: (member SetCursorPosition: int * int -> unit)>
//     (screen: 'Screen)
//     (board: Board)
//     =
//     let pos = screen.GetCursorPosition()
//     let info = $"#Press Q to quit. Board: %d{board.Width} x %d{board.Height}"
// #if DEBUG
//     // NOTE: additional info for debugging.
//     let info = $"%s{info} Position: %A{pos} Diff: %d{screen.Diff}"
// #endif
//     info |> screen.WriteLine

//     $"#Generation: %10d{board.Generation} Living: %10d{board.Lives}"
//     |> screen.WriteLine

//     screen.EmptyWriteLine()

//     board.Cells
//     |> Array2D.iteri (fun y x cell ->
//         toSymbol cell
//         |> if x + 1 < int board.Width then
//                screen.Write
//            else
//                screen.WriteLine)

//     screen.Flush()
//     pos |> screen.SetCursorPosition


[<Literal>]
let defaultInterval = 100<ms>

let inline game (screen: Screen) (board: Board) =

    let app =
        screen.App.Instance
        |> function
            | :? App as app -> app
            | _ -> failwith "Application instance is not of type App."

    let mainWindow = new MainWindow(board)
    app.mainWindow <- mainWindow
    mainWindow.WindowStartupLocation <- WindowStartupLocation.CenterScreen

    match app.desktopLifetime with
    | null -> failwith "Application lifetime is not set."
    | desktopLifetime ->
        desktopLifetime.MainWindow <- app.mainWindow
        desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose

    let cts = new Threading.CancellationTokenSource()

    mainWindow.Closed.Add(fun _ -> cts.Cancel())

    mainWindow.Show()
    app.Run(cts.Token)
