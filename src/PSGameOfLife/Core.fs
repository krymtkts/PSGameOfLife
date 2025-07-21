module PSGameOfLife.Core

open System.Collections.Concurrent

[<Struct>]
type Cell =
    | Dead
    | Live

[<Measure>]
type ms

[<Measure>]
type col

[<Measure>]
type row

[<Struct>]
type Board =
    { Column: int<col>
      Row: int<row>
      mutable Lives: int
      mutable Generation: int
      Interval: int<ms>
      mutable Cells: Cell[,] }

let neighborOffsets =
    Array.allPairs [| -1; 0; 1 |] [| -1; 0; 1 |]
    |> Array.filter (fun (dx, dy) -> dx <> 0 || dy <> 0)

let (|Survive|_|) (cell: Cell, lives) = cell.IsLive && (lives = 2 || lives = 3)
let (|Birth|_|) (cell: Cell, lives) = cell.IsDead && lives = 3

let nextCellState (cell: Cell) (lives: int) =
    match cell, lives with
    | Survive
    | Birth -> Live
    | _ -> Dead

let countLiveCells (cells: Cell[,]) =
    let mutable alive = 0

    for y in 0 .. Array2D.length1 cells - 1 do
        for x in 0 .. Array2D.length2 cells - 1 do
            if cells.[y, x].IsLive then
                alive <- alive + 1

    alive

let nextGeneration (partitioner: OrderablePartitioner<int * int>) (buffer: outref<Cell[,]>) (board: outref<Board>) =
    let columns = int board.Column
    let rows = int board.Row
    let b = board
    let tmp = buffer

    System.Threading.Tasks.Parallel.ForEach(
        partitioner,
        fun (startIdx, endIdx) ->
            for y in startIdx .. endIdx - 1 do
                for x in 0 .. columns - 1 do
                    let mutable lives = 0

                    for dx, dy in neighborOffsets do
                        let nx, ny = x + dx, y + dy

                        if nx >= 0 && nx < columns && ny >= 0 && ny < rows && b.Cells.[ny, nx].IsLive then
                            lives <- lives + 1

                    tmp.[y, x] <- nextCellState b.Cells.[y, x] lives
    )
    |> ignore

    buffer <- board.Cells
    board.Generation <- board.Generation + 1
    board.Lives <- tmp |> countLiveCells
    board.Cells <- tmp

let createBoard initializer (col: int<col>) (row: int<row>) (interval: int<ms>) =
    let cells = Array2D.init (int row) (int col) initializer

    { Column = col
      Row = row
      Lives = cells |> countLiveCells
      Generation = 0
      Interval = interval
      Cells = cells }
