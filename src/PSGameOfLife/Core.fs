module PSGameOfLife.Core

[<Struct>]
type Cell =
    | Dead
    | Live

[<Measure>]
type ms

[<Struct>]
type Board =
    { Width: int
      Height: int
      Lives: int
      Generation: int
      Interval: int<ms>
      Cells: Cell[,] }

let neighborOffsets =
    Array.allPairs [| -1; 0; 1 |] [| -1; 0; 1 |]
    |> Array.filter (fun (dx, dy) -> dx <> 0 || dy <> 0)

let getNeighborsRange (width: int) (height: int) (x: int) (y: int) =
    neighborOffsets
    |> Array.choose (fun (dx, dy) ->
        let nx, ny = x + dx, y + dy

        if nx >= 0 && nx < width && ny >= 0 && ny < height then
            Some(nx, ny)
        else
            None)

let countLiveNeighbors (cells: Cell[,]) (neighborsRange: (int * int) array) =
    neighborsRange
    |> Array.sumBy (fun (x, y) -> if cells.[y, x].IsLive then 1 else 0)

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

let nextGeneration (board: Board) =
    let nextGeneration y x cell =
        getNeighborsRange board.Width board.Height x y
        |> countLiveNeighbors board.Cells
        |> nextCellState cell

    let cells = board.Cells |> Array2D.mapi nextGeneration

    { board with
        Generation = board.Generation + 1
        Lives = cells |> countLiveCells
        Cells = cells }

let createBoard initializer width height (interval: int<ms>) =
    let cells = Array2D.init height width initializer

    { Width = width
      Height = height
      Lives = cells |> countLiveCells
      Generation = 0
      Interval = interval
      Cells = cells }
