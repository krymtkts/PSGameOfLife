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
                  { Width = 1
                    Height = 1
                    Cells = array2D [| [| Dead |] |] }

              board
              |> nextGeneration
              |> Expect.equal
                  "should stay dead"
                  { board with
                      Cells = array2D [| [| Dead |] |] }
          }

          test "When a live cell without neighbors" {
              let board =
                  { Width = 1
                    Height = 1
                    Cells = array2D [| [| Live |] |] }

              board
              |> nextGeneration
              |> Expect.equal
                  "should die"
                  { board with
                      Cells = array2D [| [| Dead |] |] }
          }

          test "When 3 live neighbors in a 2x2 board" {
              let board =
                  { Width = 2
                    Height = 2
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
                      Cells =
                          array2D
                              [|

                                 [| Live; Live |]
                                 [| Live; Live |]

                                 |] }
          }

          test "When Block" {
              let board =
                  { Width = 2
                    Height = 2
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
                      Cells =
                          array2D
                              [|

                                 [| Live; Live |]
                                 [| Live; Live |]

                                 |] }
          }


          test "when Blinker is vertical" {
              let board =
                  { Width = 3
                    Height = 3
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
                      Cells =
                          array2D
                              [|

                                 [| Dead; Dead; Dead |]
                                 [| Live; Live; Live |]
                                 [| Dead; Dead; Dead |]

                                 |] }
          }

          ]
