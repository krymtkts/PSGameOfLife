module PSGameOfLife.Core

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

    cells
    |> Array2D.iter (fun cell ->
        if cell.IsLive then
            alive <- alive + 1)

    alive

let nextGeneration (buffer: outref<Cell[,]>) (board: outref<Board>) =
    let columns = int board.Column
    let rows = int board.Row
    let cells = board.Cells

    let nextGeneration y x cell =
        let mutable lives = 0

        neighborOffsets
        |> Array.iter (fun (dx, dy) ->
            let nx, ny = x + dx, y + dy

            if nx >= 0 && nx < columns && ny >= 0 && ny < rows && cells.[ny, nx].IsLive then
                lives <- lives + 1)

        nextCellState cell lives

    let tmp = buffer

    board.Cells
    |> Array2D.iteri (fun y x cell -> tmp.[y, x] <- nextGeneration y x cell)

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
