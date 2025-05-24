module PSGameOfLife.Algorithm

open System

let defaultFateRoll = 0.3

let random (fateRoll: float) =
    let random = Random()

    fun _ _ ->
        if random.NextDouble() <= fateRoll then
            Core.Live
        else
            Core.Dead
