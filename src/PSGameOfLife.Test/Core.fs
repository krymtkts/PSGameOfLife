module PSGameOfLife.Tests.Core

open Expecto
open Expecto.Flip

open PSGameOfLife.Core
open System.Collections.Concurrent

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
