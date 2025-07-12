module PSGameOfLife.Diagnostics

open System.Diagnostics

module FpsCounter =
    let mutable lastTime = Stopwatch.GetTimestamp()
    let mutable frameCount = 0
    let mutable fps = 0.0

    let tick () =
        let now = Stopwatch.GetTimestamp()
        frameCount <- frameCount + 1
        let elapsed = (now - lastTime |> float) / float Stopwatch.Frequency

        if elapsed >= 1.0 then
            fps <- float frameCount / elapsed
            frameCount <- 0
            lastTime <- now

    let get () =
        tick ()
        fps
