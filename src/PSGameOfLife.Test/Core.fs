module PSGameOfLife.Tests.Core

open System
open System.Threading
open System.Threading.Tasks

open Avalonia
open Avalonia.Headless
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Threading
open Expecto
open Expecto.Flip

open PSGameOfLife.Core
open PSGameOfLife.View.Avalonia
open System.Collections.Concurrent

type HeadlessAppBuilder =
    static member BuildAvaloniaApp() =
        let options = AvaloniaHeadlessPlatformOptions()
        options.UseHeadlessDrawing <- false

        AppBuilder.Configure<App>().UseSkia().UseHeadless(options)

let private headlessBoard =
    { Column = 1<col>
      Row = 1<row>
      Lives = 0
      Generation = 0
      Interval = 1<ms>
      Cells = array2D [| [| Dead |] |] }

let private startHeadlessSession () =
    HeadlessUnitTestSession.StartNew(typeof<HeadlessAppBuilder>, AvaloniaTestIsolationLevel.PerTest)

let private dispatch (session: HeadlessUnitTestSession) (action: unit -> 'T) =
    session.Dispatch(Func<'T>(action), CancellationToken.None).GetAwaiter().GetResult()

let private dispatchAsync (session: HeadlessUnitTestSession) (action: unit -> Task<'T>) =
    session.Dispatch<'T>(Func<Task<'T>>(action), CancellationToken.None).GetAwaiter().GetResult()

let private runPendingJobs () =
    Dispatcher.UIThread.RunJobs(Nullable<DispatcherPriority>(DispatcherPriority.Input))

let private headlessTest name body =
    test name {
        use session = startHeadlessSession ()
        body session
    }

[<Tests>]
let testsCore =
    testList
        "Core"
        [

          test "When a dead cell without neighbors" {
              let origin =
                  { Column = 1<col>
                    Row = 1<row>
                    Lives = 0
                    Generation = 0
                    Interval = 0<ms>
                    Cells = array2D [| [| Dead |] |] }

              let partitioner = Partitioner.Create(0, int origin.Row)
              let mutable board = origin
              let mutable buffer = Array2D.copy board.Cells

              nextGeneration partitioner &buffer &board

              board
              |> Expect.equal
                  "should stay dead"
                  { origin with
                      Generation = 1
                      Cells = array2D [| [| Dead |] |] }
          }

          test "When a live cell without neighbors" {
              let origin =
                  { Column = 1<col>
                    Row = 1<row>
                    Lives = 1
                    Generation = 0
                    Interval = 0<ms>
                    Cells = array2D [| [| Live |] |] }

              let partitioner = Partitioner.Create(0, int origin.Row)
              let mutable board = origin
              let mutable buffer = Array2D.copy board.Cells

              nextGeneration partitioner &buffer &board

              board
              |> Expect.equal
                  "should die"
                  { board with
                      Lives = 0
                      Generation = 1
                      Cells = array2D [| [| Dead |] |] }
          }

          test "When 3 live neighbors in a 2x2 board" {
              let origin =
                  { Column = 2<col>
                    Row = 2<row>
                    Lives = 3
                    Generation = 0
                    Interval = 0<ms>
                    Cells =
                      array2D
                          [|

                             [| Dead; Live |]
                             [| Live; Live |]

                             |] }

              let partitioner = Partitioner.Create(0, int origin.Row)
              let mutable board = origin
              let mutable buffer = Array2D.copy board.Cells

              nextGeneration partitioner &buffer &board

              board
              |> Expect.equal
                  "should become Block"
                  { board with
                      Lives = 4
                      Generation = 1
                      Cells =
                          array2D
                              [|

                                 [| Live; Live |]
                                 [| Live; Live |]

                                 |] }
          }

          test "When Block" {
              let origin =
                  { Column = 2<col>
                    Row = 2<row>
                    Lives = 4
                    Generation = 0
                    Interval = 0<ms>
                    Cells =
                      array2D
                          [|

                             [| Live; Live |]
                             [| Live; Live |]

                             |] }

              let partitioner = Partitioner.Create(0, int origin.Row)
              let mutable board = origin
              let mutable buffer = Array2D.copy board.Cells

              nextGeneration partitioner &buffer &board

              board
              |> Expect.equal
                  "should stay alive"
                  { board with
                      Generation = 1
                      Cells =
                          array2D
                              [|

                                 [| Live; Live |]
                                 [| Live; Live |]

                                 |] }
          }


          test "when Blinker is vertical" {
              let origin =
                  { Column = 3<col>
                    Row = 3<row>
                    Lives = 3
                    Generation = 0
                    Interval = 0<ms>
                    Cells =
                      array2D
                          [|

                             [| Dead; Live; Dead |]
                             [| Dead; Live; Dead |]
                             [| Dead; Live; Dead |]

                             |] }

              let partitioner = Partitioner.Create(0, int origin.Row)
              let mutable board = origin
              let mutable buffer = Array2D.copy board.Cells

              nextGeneration partitioner &buffer &board

              board
              |> Expect.equal
                  "should become a horizontal line"
                  { board with
                      Generation = 1
                      Cells =
                          array2D
                              [|

                                 [| Dead; Dead; Dead |]
                                 [| Live; Live; Live |]
                                 [| Dead; Dead; Dead |]

                                 |] }
          }

          ]

[<Tests>]
let testsAvalonia =
    testSequenced (
        testList
            "Avalonia"
            [

              headlessTest "window close raises Closed and cancels the token" (fun session ->
                  let closed, cancelled =
                      dispatch session (fun () ->
                          let mutable closed = false
                          use cts = new CancellationTokenSource()
                          let window = new MainWindow(1, headlessBoard, cts, fun () -> ())

                          window.Closed.Add(fun _ -> closed <- true)
                          window.Show()
                          window.Close()

                          closed, cts.IsCancellationRequested)

                  Expect.isTrue "window close should raise Closed" closed
                  Expect.isTrue "window close should cancel the token" cancelled)

              headlessTest "Q requests shutdown once and handles the key" (fun session ->
                  let closedBeforeJobs, requests, handled =
                      dispatch session (fun () ->
                          let mutable requests = 0
                          let mutable closed = false
                          let mutable handled = false
                          use cts = new CancellationTokenSource()

                          let window =
                              new MainWindow(1, headlessBoard, cts, fun () -> requests <- requests + 1)

                          window.Closed.Add(fun _ -> closed <- true)

                          window.AddHandler(
                              InputElement.KeyDownEvent,
                              EventHandler<KeyEventArgs>(fun _ e -> handled <- e.Handled),
                              RoutingStrategies.Bubble,
                              true
                          )

                          window.Show()
                          window.KeyPressQwerty(PhysicalKey.Q, RawInputModifiers.None)
                          let closedBeforeJobs = closed
                          runPendingJobs ()
                          window.Close()
                          closedBeforeJobs, requests, handled)

                  Expect.isFalse "Q should not close the window during key handling" closedBeforeJobs
                  Expect.equal "Q should request shutdown once" 1 requests
                  Expect.isTrue "Q should be handled" handled)

              headlessTest "Q shutdown callback can close the window" (fun session ->
                  let requests, closed, cancelled =
                      dispatch session (fun () ->
                          let mutable requests = 0
                          let mutable closed = false
                          let mutable closeWindow = fun () -> ()
                          use cts = new CancellationTokenSource()

                          let window =
                              new MainWindow(
                                  1,
                                  headlessBoard,
                                  cts,
                                  fun () ->
                                      requests <- requests + 1
                                      closeWindow ()
                              )

                          closeWindow <- window.Close
                          window.Closed.Add(fun _ -> closed <- true)
                          window.Show()
                          window.KeyPressQwerty(PhysicalKey.Q, RawInputModifiers.None)
                          runPendingJobs ()

                          let closedByQ = closed

                          if not closed then
                              window.Close()

                          requests, closedByQ, cts.IsCancellationRequested)

                  Expect.equal "Q should invoke the shutdown callback once" 1 requests
                  Expect.isTrue "Q shutdown should close the window" closed
                  Expect.isTrue "Q shutdown should cancel the token" cancelled)

              headlessTest "game loop task completes after window close" (fun session ->
                  let cancelled =
                      dispatchAsync session (fun () ->
                          task {
                              use cts = new CancellationTokenSource()
                              let window = new MainWindow(1, headlessBoard, cts, fun () -> ())
                              let keepAlive = new Avalonia.Controls.Window()
                              window.Show()
                              keepAlive.Show()
                              let loopTask: Task = window.StartLoop()
                              window.Close()
                              do! loopTask
                              keepAlive.Close()
                              return cts.IsCancellationRequested
                          })

                  Expect.isTrue "window close should cancel the game loop" cancelled)

              ]
    )
