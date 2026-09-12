module PSGameOfLife.Algorithm

let defaultFateRoll = 0.3

let random (fateRoll: float) =
    let random = System.Random()

    fun _ _ ->
        if random.NextDouble() <= fateRoll then
            Core.Live
        else
            Core.Dead
