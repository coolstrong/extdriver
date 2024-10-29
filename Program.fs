module ExtDriver.Program

open System
open CommandLine
open ExtDriver.Handlers


[<Verb("mount", HelpText = "Mount external drive.")>]
type MountArguments() =
    inherit BaseMountArguments()

[<Verb("unmount", HelpText = "Unmount external drive.")>]
type UnmountArguments() =
    inherit BaseMountArguments()

[<Verb("list", IsDefault = true, HelpText = "list all external drives.")>]
type ListArguments() =
    [<Option('s', "simple", HelpText = "Output in simple format (no decorations).")>]
    member val Simple = false with get, set


[<EntryPoint>]
let main args =
    let result =
        Parser.Default.ParseArguments<MountArguments, UnmountArguments, ListArguments> args

    match result with
    | :? Parsed<obj> as cmd ->
        match cmd.Value with
        | :? ListArguments as args ->
            printDrives (if args.Simple then Simple else Decorated)
            0
        | :? MountArguments as args ->
            Console.WriteLine (String.Join(", ", args.Devices))
            0
        | _ -> 0

    | _ -> 1
