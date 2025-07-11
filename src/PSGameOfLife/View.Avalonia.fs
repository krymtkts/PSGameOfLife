module PSGameOfLife.View.Avalonia

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Platform
open Avalonia.Themes.Fluent
open Avalonia.Threading
open Microsoft.FSharp.NativeInterop
open System
open System.Collections.Concurrent
open System.Runtime.InteropServices
open System.Threading.Tasks

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

    let resolver (ptrCache: ConcurrentDictionary<string, nativeint>) moduleDir extension =
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
        let cache = new ConcurrentDictionary<string, nativeint>()
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

    let statusRowHeight = 20.0
    let vectorSize = Vector<byte>.Count

    let createCellTemplate (cellSize: int) (color: byte * byte * byte * byte) : byte array * Vector<byte> array =
        let b, g, r, a = color
        let byteLength = cellSize * 4
        let bytes = Array.zeroCreate<byte> byteLength

        for x in 0 .. cellSize - 1 do
            let idx = x * 4
            bytes.[idx] <- b
            bytes.[idx + 1] <- g
            bytes.[idx + 2] <- r
            bytes.[idx + 3] <- a

        let nvec = byteLength / vectorSize
        let vectors = Array.init nvec (fun i -> Vector<byte>(bytes, i * vectorSize))
        let offset = nvec * vectorSize
        let rem = if byteLength > offset then bytes.[offset..] else [||]
        rem, vectors

    [<Struct>]
    type Templates =
        { LiveBytes: byte array
          LiveVectors: Vector<byte> array
          DeadBytes: byte array
          DeadVectors: Vector<byte> array }

    let initCellTemplates cellSize : Templates =
        let liveBytes, liveVectors = createCellTemplate cellSize (0uy, 0uy, 0uy, 255uy)

        let deadBytes, deadVectors =
            createCellTemplate cellSize (255uy, 255uy, 255uy, 255uy)

        { LiveBytes = liveBytes
          LiveVectors = liveVectors
          DeadBytes = deadBytes
          DeadVectors = deadVectors }

    let writeTemplateSIMD (dst: nativeptr<byte>) (vectors: Vector<byte> array) (rem: byte array) =
        let baseAddr = NativePtr.toNativeInt dst

        for i = 0 to vectors.Length - 1 do
            Unsafe.WriteUnaligned((baseAddr + nativeint (i * vectorSize)).ToPointer(), vectors.[i])

        let offset = vectors.Length * vectorSize

        if rem.Length > 0 then
            let dstRemPtr = NativePtr.add dst offset
            // NOTE: pinning the template array to avoid GC moving it.
            use ptr = fixed &rem.[0]

            Unsafe.CopyBlockUnaligned(
                NativePtr.toVoidPtr dstRemPtr,
                NativePtr.toVoidPtr (NativePtr.ofNativeInt<byte> (NativePtr.toNativeInt ptr)),
                uint32 rem.Length
            )

type MainWindow(cellSize: int, board: Board, cts: Threading.CancellationTokenSource) as this =
    inherit Window()
    let templates = Main.initCellTemplates cellSize
    let width = int board.Column * cellSize
    let height = int board.Row * cellSize

    let bufferSize = width * height <<< 2
    let tempBuffer: byte array = Array.zeroCreate bufferSize
    let partitioner = Partitioner.Create(0, int board.Row)
    let lenX = int board.Column - 1

    let renderBoard (board: Board) (wb: WriteableBitmap) =
        use tempPtr = fixed &tempBuffer.[0]

        Parallel.ForEach(
            partitioner,
            fun (startIdx, endIdx) ->
                for y = startIdx to endIdx - 1 do
                    let yc = y * cellSize

                    for x = 0 to lenX do
                        let xc = x * cellSize

                        let vectors, bytes =
                            match board.Cells.[y, x] with
                            | Live -> templates.LiveVectors, templates.LiveBytes
                            | Dead -> templates.DeadVectors, templates.DeadBytes

                        for dy = 0 to cellSize - 1 do
                            let dstOffset = ((yc + dy) * width + xc) <<< 2

                            let dstLinePtr =
                                NativePtr.add (NativePtr.ofNativeInt<byte> (NativePtr.toNativeInt tempPtr)) dstOffset

                            Main.writeTemplateSIMD dstLinePtr vectors bytes
        )
        |> ignore

        // NOTE: Parallel write to WriteableBitmap cause a deadlock. so avoid it by using a temporary buffer.
        use fb = wb.Lock()
        Runtime.InteropServices.Marshal.Copy(tempBuffer, 0, fb.Address, bufferSize)

    let stack, updateUI =
        let status1 =
            TextBlock(Background = Brushes.White, Foreground = Brushes.Black, Height = Main.statusRowHeight)

        let status2 =
            TextBlock(Background = Brushes.White, Foreground = Brushes.Black, Height = Main.statusRowHeight)

        let image = Image(Width = float width, Height = float height)

        let wb =
            new WriteableBitmap(PixelSize(width, height), Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque)

        image.Source <- wb

#if DEBUG || SHOW_FPS
        let fpsText =
            let tb =
                TextBlock(Foreground = Brushes.Yellow, Background = SolidColorBrush(Color.Parse("#80000000")))

            Canvas.SetTop(tb, 0.0)
            Canvas.SetRight(tb, 0.0)
            tb.SetValue(Canvas.ZIndexProperty, 100) |> ignore
            tb
#else
        let fpsText = null
#endif
        let canvas = Canvas(Width = float width, Height = float height)
        let stack = StackPanel()

        status1 |> stack.Children.Add
        status2 |> stack.Children.Add
        image |> canvas.Children.Add
        canvas |> stack.Children.Add
#if DEBUG || SHOW_FPS
        fpsText |> canvas.Children.Add
#endif

        let updateUI board =
            status1.Text <- $"#Press Q to quit. Board: {board.Column} x {board.Row}"
            status2.Text <- $"#Generation: {board.Generation, 10} Living: {board.Lives, 10}"
            renderBoard board wb
            image.InvalidateVisual()
#if DEBUG || SHOW_FPS
            fpsText.Text <- $"FPS: %.2f{FpsCounter.get ()}"
#endif
        stack, updateUI

    [<TailCall>]
    let rec loop board =
        async {
            if cts.IsCancellationRequested then
                return ()

            do!
                Dispatcher.UIThread.InvokeAsync(fun () -> updateUI board).GetTask()
                |> Async.AwaitTask

            do! Async.Sleep(int board.Interval)
            let currentBoard = nextGeneration board
            return! loop currentBoard
        }

    do
        this.Title <- "PSGameOfLife"
        this.Width <- float width
        this.Height <- float height + Main.statusRowHeight * 2.0
        this.CanResize <- false
        this.Content <- stack
#if DEBUG
        printfn "Starting PSGameOfLife with board size %d x %d" (int board.Column) (int board.Row)
#endif

        Async.StartImmediate(loop board, cts.Token)

    override __.OnClosed(e: EventArgs) =
        cts.Cancel()
        base.OnClosed(e)

    override __.OnKeyDown(e: Avalonia.Input.KeyEventArgs) =
        if e.Key = Avalonia.Input.Key.Q then
#if DEBUG
            printfn "Quitting PSGameOfLife."
#endif
            __.Close()

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


type Screen(cellSize: int, col: int, row: int) =
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

    member __.CellSize = cellSize
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
    let mainWindow = new MainWindow(screen.CellSize, board, cts)
    app.mainWindow <- mainWindow
    mainWindow.WindowStartupLocation <- WindowStartupLocation.CenterScreen

    match app.desktopLifetime with
    | null -> failwith "Application lifetime is not set."
    | desktopLifetime ->
        desktopLifetime.MainWindow <- mainWindow
        desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose

    mainWindow.Show()
    app.Run(cts.Token)
