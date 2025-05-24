module PSGameOfLife.Tests.Core

open Expecto
open Expecto.Flip

open PSGameOfLife.Core


[<Tests>]
let testsCore =
    testList
        "Core"
        [

          test "When a dead cell without neighbors" {
              let board =
                  { Width = 1<width>
                    Height = 1<height>
                    Lives = 0
                    Generation = 0
                    Interval = 0<ms>
                    Cells = array2D [| [| Dead |] |] }

              board
              |> nextGeneration
              |> Expect.equal
                  "should stay dead"
                  { board with
                      Generation = 1
                      Cells = array2D [| [| Dead |] |] }
          }

          test "When a live cell without neighbors" {
              let board =
                  { Width = 1<width>
                    Height = 1<height>
                    Lives = 1
                    Generation = 0
                    Interval = 0<ms>
                    Cells = array2D [| [| Live |] |] }

              board
              |> nextGeneration
              |> Expect.equal
                  "should die"
                  { board with
                      Lives = 0
                      Generation = 1
                      Cells = array2D [| [| Dead |] |] }
          }

          test "When 3 live neighbors in a 2x2 board" {
              let board =
                  { Width = 2<width>
                    Height = 2<height>
                    Lives = 3
                    Generation = 0
                    Interval = 0<ms>
                    Cells =
                      array2D
                          [|

                             [| Dead; Live |]
                             [| Live; Live |]

                             |] }

              board
              |> nextGeneration
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
              let board =
                  { Width = 2<width>
                    Height = 2<height>
                    Lives = 4
                    Generation = 0
                    Interval = 0<ms>
                    Cells =
                      array2D
                          [|

                             [| Live; Live |]
                             [| Live; Live |]

                             |] }

              board
              |> nextGeneration
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
              let board =
                  { Width = 3<width>
                    Height = 3<height>
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

              board
              |> nextGeneration
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
