module PSGameOfLife.Core

[<Struct>]
type Cell =
    | Dead
    | Live

[<Struct>]
type Board =
    { Width: int
      Height: int
      Lives: int
      Generation: int
      Cells: Cell[,] }

let neighborOffsets =
    Array.allPairs [| -1; 0; 1 |] [| -1; 0; 1 |]
    |> Array.filter (fun (dx, dy) -> dx <> 0 || dy <> 0)

let getNeighborsRange (width: int) (height: int) (x: int) (y: int) =
    neighborOffsets
    |> Array.map (fun (dx, dy) -> x + dx, y + dy)
    |> Array.filter (fun (x, y) -> x >= 0 && x < width && y >= 0 && y < height)

let countLiveNeighbors (cells: Cell[,]) (neighborsRange: (int * int) array) =
    neighborsRange
    |> Array.sumBy (fun (x, y) -> if cells.[y, x].IsLive then 1 else 0)

let inline (|Survive|_|) (cell: Cell, lives) = cell.IsLive && (lives = 2 || lives = 3)
let inline (|Birth|_|) (cell: Cell, lives) = cell.IsDead && lives = 3

let nextCellState (cell: Cell) (lives: int) =
    match cell, lives with
    | Survive
    | Birth -> Live
    | _ -> Dead

let countLiveCells (cells: Cell[,]) =
    cells |> Seq.cast<Cell> |> Seq.sumBy (fun cell -> if cell.IsLive then 1 else 0)

let nextGeneration (board: Board) =
    let nextGeneration =
        fun y x cell ->
            getNeighborsRange board.Width board.Height x y
            |> countLiveNeighbors board.Cells
            |> nextCellState cell

    let cells = board.Cells |> Array2D.mapi nextGeneration

    { board with
        Generation = board.Generation + 1
        Lives = cells |> countLiveCells
        Cells = cells }

let createBoard initializer width height =
    let cells = Array2D.init height width initializer

    { Width = width
      Height = height
      Lives = cells |> countLiveCells
      Generation = 0
      Cells = cells }
