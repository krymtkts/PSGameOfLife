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

    type State = { swap: bool }

    let init () = { swap = true }, Cmd.none

    type Msg =
        | Swap
        | Noop

    let update (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | Swap -> { state with swap = not state.swap }, Cmd.none
        | Noop -> state, Cmd.none

    let view (state: State) (dispatch: Msg -> unit) =
        let cellSize = 10.0
        let rows, cols = 50, 50
        let width = float cols * cellSize
        let height = float rows * cellSize
        let isAlive row col = (row + col) % 2 = 0 = state.swap

        let cells =
            Array2D.init (int height) (int width) (fun row col -> if isAlive row col then "white" else "black")

        Canvas.create
            [ Canvas.width width
              Canvas.height height
              Canvas.children
                  [ for i in 0 .. (rows * cols - 1) do
                        let row = i / cols
                        let col = i % cols
                        let color = cells.[row, col]

                        yield
                            Rectangle.create
                                [ Shapes.Rectangle.width cellSize
                                  Shapes.Rectangle.height cellSize
                                  Shapes.Rectangle.fill (Media.SolidColorBrush(Avalonia.Media.Color.Parse color))
                                  Canvas.left (float col * cellSize)
                                  Canvas.top (float row * cellSize)
                                  Shapes.Rectangle.onTapped (fun _ -> dispatch Swap) ] ]

              ]
        |> generalize

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Example"
        base.Height <- 500.0
        base.Width <- 500.0
        base.CanResize <- false

        Program.mkProgram Main.init Main.update Main.view
        |> Program.withHost this
        |> Program.run

    override __.OnClosed(e: EventArgs) : unit = base.OnClosed(e: EventArgs)

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


type Screen() =
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

    member __.Column = LanguagePrimitives.Int32WithMeasure<col> 30
    member __.Row = LanguagePrimitives.Int32WithMeasure<row> 30

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

    let mainWindow = new MainWindow()
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
