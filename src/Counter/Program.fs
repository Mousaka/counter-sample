// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System

module Counter =
    
    type Cleared = { value : int }
    type Event = 
        | Incremented
        | Decremented
        | Cleared of Cleared
        interface TypeShape.UnionContract.IUnionContract
    (* Kind of DDD aggregate ID *)
    let streamName (id : string) = FsCodec.StreamName.create "Counter" id

    type State = State of int
    let initial : State = State 0
    (* Evolve takes the present state and one event and figures out the next state*)
    let evolve state event =
        match event, state with
        | Incremented, State s -> State (s + 1)
        | Decremented, State s -> State (s - 1)
        | Cleared { value = x }, _ -> State x

    (* Fold is folding the evolve function over all events to get the current state
       It's equivalent to LINQ's Aggregate function *)
    let fold state events = Seq.fold evolve state events

    (* Commands are the things we intend to happen, though they may not*)
    type Command = 
        | Increment
        | Decrement
        | Clear of int

    (* Decide consumes a command and the current state to decide what events actually happened.
       This particular counter allows numbers from 0 to 100. *)

    let decide command (State state) =
        match command with
        | Increment -> 
            if state > 100 then [] else [Incremented]
        | Decrement -> 
            if state <= 0 then [] else [Decremented]
        | Clear i -> 
            if state = i then [] else [Cleared {value = i}]

    type Service internal (resolve : string -> Equinox.Decider<Event, State>) =

        member __.Execute(instanceId, command) : Async<unit> =
            let decider = resolve instanceId
            decider.Transact(decide command)

        member __.Reset(instanceId, value) : Async<unit> =
            __.Execute(instanceId, Clear value)

        member __.Read instanceId : Async<int> =
            let decider = resolve instanceId
            decider.Query(fun (State value) -> value)

    (* Out of the box, logging is via Serilog (can be wired to anything imaginable).
       We wire up logging for demo purposes using MemoryStore.VolatileStore's Committed event
       MemoryStore itself, by design, has no intrinsic logging
       (other store bindings have rich relevant logging about roundtrips to physical stores etc) *)

    open Serilog
    let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
    let logEvents stream (events : FsCodec.ITimelineEvent<_>[]) =
        log.Information("Committed to {stream}, events: {@events}", stream, seq { for x in events -> x.EventType })

    (* We can integration test using an in-memory store
       See other examples such as Cosmos.fsx to see how we integrate with CosmosDB and/or other concrete stores *)

    let store = Equinox.MemoryStore.VolatileStore()
    let _ = store.Committed.Subscribe(fun (s, xs) -> logEvents s xs)
    let codec = FsCodec.Box.Codec.Create()
    let cat = Equinox.MemoryStore.MemoryStoreCategory(store, codec, fold, initial)
    let resolve instanceId = Equinox.Decider(log, streamName instanceId |> cat.Resolve, maxAttempts = 3)
    let service = Service(resolve)




[<EntryPoint>]
let main argv =
    let clientId = "ClientA"
    printfn "Hello world" 
    Counter.service.Read(clientId) |> Async.RunSynchronously
    Counter.service.Execute(clientId, Counter.Increment) |> Async.RunSynchronously
    Counter.service.Read(clientId) |> Async.RunSynchronously
    Counter.service.Reset(clientId, 5) |> Async.RunSynchronously 
    Counter.service.Read(clientId) |> Async.RunSynchronously
    0 // return an integer exit code