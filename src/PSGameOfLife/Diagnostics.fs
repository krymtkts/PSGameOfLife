module PSGameOfLife.Diagnostics

open System

module FpsCounter =
    let mutable lastTime = DateTime.UtcNow
    let mutable frameCount = 0
    let mutable fps = 0.0

    let tick () =
        let now = DateTime.UtcNow
        frameCount <- frameCount + 1
        let elapsed = (now - lastTime).TotalSeconds

        if elapsed >= 1.0 then
            fps <- float frameCount / elapsed
            frameCount <- 0
            lastTime <- now

    let get () =
        tick ()
        fps
