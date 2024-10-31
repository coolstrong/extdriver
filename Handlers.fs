module ExtDriver.Handlers

open System
open FsToolkit.ErrorHandling
open CommandLine
open Microsoft.FSharp.Collections
open Spectre.Console
open ExtDriver.Disks

[<AbstractClass>]
type BaseMountArguments() =
    [<Option('a', "all", HelpText = "Mount/unmount all appropriate external drives.")>]
    member val All = false with get, set

    [<Value(0, MetaName = "devices", HelpText = "Block devices to mount/unmount (e.g. sda1).")>]
    member val Devices: string seq = [] with get, set

type MountAction =
    | Mount
    | Unmount

let driveAction (action: MountAction) (args: BaseMountArguments) = result {
    let! allPartitions = fetchExternalPartitions ()

    let execAction (part: PartitionEntry) =
        match action with
        | Mount -> part.Mount()
        | Unmount -> part.Unmount()
    
    let suitablePartitions  =
        allPartitions
        |> List.filter (
            match action with
            | Mount -> _.Mountpoint.IsNone
            | Unmount -> _.Mountpoint.IsSome)
    
    let! targetPartitions =
        match args.All with
        | true -> Ok(suitablePartitions)
        | false ->
            match Seq.toList args.Devices with
            | [] ->
                match suitablePartitions with
                | [] -> Result.Error "No suitable external drives found"
                | [ drive ] -> Ok [ drive ]
                | _ -> Result.Error "Ambiguous command: more than one available disk"
            | devices ->
                devices
                |> List.traverseResultM (fun x ->
                    suitablePartitions
                    |> List.tryFind (fun p -> p.name = x.Trim())
                    |> Option.toResult $"Drive \"{x}\" not found or already mounted/unmounted")

    let! _ = targetPartitions |> List.traverseResultM execAction

    return ()
}

type PrintFormat =
    | Decorated
    | Simple

let printDrives (format: PrintFormat) = result {
    let! partitions = fetchExternalPartitions ()

    match format with
    | Simple ->
        for part in partitions do
            Console.WriteLine(
                String.Join("\t", part.label |? "", part.Mountpoint |? "", part.name, part.uuid, part.fstype)
            )

    | Decorated ->
        let table =
            Table()
                .AddColumns(
                    TableColumn("Label"),
                    TableColumn("Mount Point"),
                    TableColumn("Block Device"),
                    TableColumn("UUID"),
                    TableColumn("FS Type")
                )

        for part in partitions do
            ignore (
                table.AddRow(
                    part.label |? "<no label>",
                    part.Mountpoint |? "<not mounted>",
                    part.name,
                    part.uuid,
                    part.fstype
                )
            )

        AnsiConsole.Write table
}