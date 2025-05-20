module PSGameOfLife.Core

[<Struct>]
type Cell =
    | Dead
    | Live

[<Struct>]
type Board =
    { Width: int
      Height: int
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
    |> Array.sumBy (fun (x, y) -> if cells.[x, y].IsLive then 1 else 0)

let inline (|Survive|_|) (cell: Cell, lives) = cell.IsLive && (lives = 2 || lives = 3)
let inline (|Birth|_|) (cell: Cell, lives) = cell.IsDead && lives = 3

let isCellAlive (cell: Cell) (lives: int) =
    match cell, lives with
    | Survive
    | Birth -> Live
    | _ -> Dead

let nextGeneration (board: Board) =
    let nextGeneration =
        fun x y cell ->
            getNeighborsRange board.Width board.Height x y
            |> countLiveNeighbors board.Cells
            |> isCellAlive cell

    { board with
        Cells = board.Cells |> Array2D.mapi nextGeneration }
