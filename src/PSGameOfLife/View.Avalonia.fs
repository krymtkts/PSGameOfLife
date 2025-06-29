module PSGameOfLife.View.Avalonia

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

open PSGameOfLife.Core

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

#nowarn "9"

#if DEBUG || SHOW_FPS
module FpsCounter =
    let mutable lastTime = DateTime.UtcNow
    let mutable frameCount = 0
    let mutable fps = 0.0

    let tick () =
        let now = DateTime.UtcNow
        frameCount <- frameCount + 1
        let elapsed = (now - lastTime).TotalSeconds

        if elapsed >= 1.0 then
            fps <- float frameCount / elapsed
            frameCount <- 0
            lastTime <- now

    let get () =
        tick ()
        fps
#endif

module Main =
    open System.Numerics
    open System.Runtime.CompilerServices
    open Avalonia.Media.Imaging
    open Avalonia.Platform
    open Microsoft.FSharp.NativeInterop
    open System.Threading.Tasks
    open System.Collections.Concurrent

    [<Struct>]
    type State = { Board: Board }

    [<Struct>]
    type Msg =
        | Next
        | Noop

    let update (cts: Threading.CancellationTokenSource) (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | Next ->
            let nextState = { Board = state.Board |> nextGeneration }
            let ms = int state.Board.Interval

            // NOTE: Using Cmd.OfAsyncImmediate for frame-driven rendering. Using DispatcherTimer will cause rendering to get stuck at a 0ms interval.
            let cmd =
                Cmd.OfAsyncImmediate.either
                    (fun _ ->
                        async {
                            if cts.IsCancellationRequested then
                                "Game was canceled." |> OperationCanceledException |> raise
                            else
                                do! Async.Sleep ms
                        })
                    ()
                    (fun _ -> Next)
                    (fun _ -> Noop)

            nextState, cmd
        | Noop -> state, Cmd.none

    let statusRowHeight = 20.0

    let createCellTemplate (cellSize: int) (color: byte * byte * byte * byte) : byte[] * Vector<byte>[] * int =
        let b, g, r, a = color
        let bytes = Array.zeroCreate<byte> (cellSize * 4)

        for x in 0 .. cellSize - 1 do
            let idx = x * 4
            bytes.[idx] <- b
            bytes.[idx + 1] <- g
            bytes.[idx + 2] <- r
            bytes.[idx + 3] <- a

        let vsize = Vector<byte>.Count
        let nvec = bytes.Length / vsize
        let rem = bytes.Length % vsize
        let vectors = Array.init nvec (fun i -> Vector<byte>(bytes, i * vsize))
        bytes, vectors, rem

    [<Struct>]
    type Templates =
        { LiveBytes: byte[]
          LiveVectors: Vector<byte>[]
          DeadBytes: byte[]
          DeadVectors: Vector<byte>[] }

    let vectorSize = Vector<byte>.Count

    let initCellTemplates cellSize : Templates =
        let liveBytes, liveVectors, liveRem =
            createCellTemplate cellSize (0uy, 0uy, 0uy, 255uy)

        let deadBytes, deadVectors, deadRem =
            createCellTemplate cellSize (255uy, 255uy, 255uy, 255uy)

        { LiveBytes = liveBytes
          LiveVectors = liveVectors
          DeadBytes = deadBytes
          DeadVectors = deadVectors }

    let writeTemplateSIMD (dst: nativeptr<byte>) (vectors: Vector<byte>[]) (template: byte[]) =
        let baseAddr = NativePtr.toNativeInt dst

        for i = 0 to vectors.Length - 1 do
            Unsafe.WriteUnaligned((baseAddr + nativeint (i * vectorSize)).ToPointer(), vectors.[i])

        for i = vectors.Length * vectorSize to template.Length - 1 do
            NativePtr.set dst i template.[i]

    let view (cellSize: int) (templates: Templates) (state: State) (dispatch: Msg -> unit) =
        let width = int state.Board.Column * cellSize
        let height = int state.Board.Row * cellSize

        let wb =
            new WriteableBitmap(PixelSize(width, height), Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque)

        use fb = wb.Lock()
        let dstPtr = fb.Address.ToPointer()

        let partitioner = Partitioner.Create(0, Array2D.length1 state.Board.Cells)

        Parallel.ForEach(
            partitioner,
            fun (startIdx, endIdx) ->
                for y = startIdx to endIdx - 1 do
                    let yc = y * cellSize

                    for x = 0 to Array2D.length2 state.Board.Cells - 1 do
                        let xc = x * cellSize

                        let isLive =
                            match state.Board.Cells.[y, x] with
                            | Live -> true
                            | Dead -> false

                        for dy = 0 to cellSize - 1 do
                            let dstOffset = ((yc + dy) * width + xc) * 4
                            let dstLinePtr = NativePtr.add (NativePtr.ofVoidPtr<byte> dstPtr) dstOffset

                            if isLive then
                                writeTemplateSIMD dstLinePtr templates.LiveVectors templates.LiveBytes
                            else
                                writeTemplateSIMD dstLinePtr templates.DeadVectors templates.DeadBytes
        )
        |> ignore

        StackPanel.create
            [ StackPanel.children
                  [ TextBlock.create
                        [ TextBlock.background "white"
                          TextBlock.foreground "black"
                          TextBlock.height statusRowHeight
                          TextBlock.text $"#Press Q to quit. Board: {state.Board.Column} x {state.Board.Row}" ]
                    TextBlock.create
                        [ TextBlock.background "white"
                          TextBlock.foreground "black"
                          TextBlock.height statusRowHeight
                          TextBlock.text $"#Generation: {state.Board.Generation, 10} Living: {state.Board.Lives, 10}" ]
                    Canvas.create
                        [ Canvas.width (float width)
                          Canvas.height (float height)
                          Canvas.children (
                              [ Image.create [ Image.source wb; Image.width (float width); Image.height (float height) ]
#if DEBUG || SHOW_FPS
                                // NOTE: Debug overlay shows FPS.
                                TextBlock.create
                                    [ TextBlock.text $"FPS: %.2f{FpsCounter.get ()}"
                                      TextBlock.background "#80000000"
                                      TextBlock.foreground "yellow"
                                      Canvas.top 0.0
                                      Canvas.right 0.0
                                      TextBlock.zIndex 100 ]
#endif
                                ]
                          ) ] ] ]

type MainWindow(board: Board, cts: Threading.CancellationTokenSource) as __ =
    inherit HostWindow()

    let cellSize = 10

    do
        base.Title <- "PSGameOfLife"
        base.Width <- board.Column * cellSize |> float
        base.Height <- board.Row * cellSize |> float |> (+) <| Main.statusRowHeight * 2.0
        base.CanResize <- false

#if DEBUG
        printfn "Starting PSGameOfLife with board size %d x %d" board.Column board.Row
#endif
        let init () : Main.State * Cmd<_> = { Board = board }, Cmd.ofMsg Main.Next
        let template = Main.initCellTemplates cellSize

        Program.mkProgram init (Main.update cts) (Main.view cellSize template)
        |> Program.withHost __
        |> Program.run

    override __.OnClosed(e: EventArgs) : unit =
        cts.Cancel()
        base.OnClosed(e)

    override __.OnKeyDown(e: Input.KeyEventArgs) : unit =
        match e.Key with
        | Input.Key.Q ->
#if DEBUG
            printfn "Quitting PSGameOfLife."
#endif
            __.Close(e)
        | _ -> ()

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
#if DEBUG
            printfn "Disposing Screen with size %d x %d" col row
#endif
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

[<Literal>]
let defaultInterval = 100<ms>

let inline game (screen: Screen) (board: Board) =

    let app =
        screen.App.Instance
        |> function
            | :? App as app -> app
            | _ -> failwith "Application instance is not of type App."

    let cts = new Threading.CancellationTokenSource()
    let mainWindow = new MainWindow(board, cts)
    app.mainWindow <- mainWindow
    mainWindow.WindowStartupLocation <- WindowStartupLocation.CenterScreen

    match app.desktopLifetime with
    | null -> failwith "Application lifetime is not set."
    | desktopLifetime ->
        desktopLifetime.MainWindow <- mainWindow
        desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose

    mainWindow.Show()
    app.Run(cts.Token)
