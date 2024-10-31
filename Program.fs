module ExtDriver.Program

open CommandLine
open ExtDriver.Handlers
open Spectre.Console


[<Verb("mount", aliases = [| "m" |], HelpText = "Mount external drive.")>]
type MountArguments() =
    inherit BaseMountArguments()

[<Verb("unmount", aliases = [| "u"; "un" |], HelpText = "Unmount external drive.")>]
type UnmountArguments() =
    inherit BaseMountArguments()

[<Verb("list", isDefault = true, aliases = [| "l"; "ls" |], HelpText = "list all external drives.")>]
type ListArguments() =
    [<Option('s', "simple", HelpText = "Output in simple format (no decorations).")>]
    member val Simple = false with get, set


[<EntryPoint>]
let main args =
    let parsedArgs =
        Parser.Default.ParseArguments<ListArguments, MountArguments, UnmountArguments> args

    let result =
        match parsedArgs with
        | :? Parsed<obj> as cmd ->
            match cmd.Value with
            | :? ListArguments as args -> printDrives (if args.Simple then Simple else Decorated)
            | :? MountArguments as args -> driveAction Mount args
            | :? UnmountArguments as args -> driveAction Unmount args
            | _ -> failwith "Unexpected verb encountered"

        | _ -> Result.Error ""

    match result with
    | Error e ->
        AnsiConsole.MarkupLine $"[red]{e}[/]"
        1
    | Ok _ -> 0
