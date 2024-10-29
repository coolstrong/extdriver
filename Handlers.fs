module ExtDriver.Handlers

open System
open FsToolkit.ErrorHandling
open CommandLine
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

let driveAction (action: MountAction) (args: BaseMountArguments) =
    let allPartitions = fetchExternalPartitions ()

    result {
        let! parts =
            match args.All with
            | true -> Ok(allPartitions |> List.toSeq)
            | false ->
                args.Devices
                |> Seq.traverseResultM (fun x ->
                    allPartitions
                    |> List.tryFind (fun p -> p.name = x.Trim())
                    |> Option.toResult $"Drive \"{x}\" not found")

        let! _ =
            parts
            |> Seq.traverseResultM (fun x ->
                match action with
                | Mount -> x.Mount()
                | Unmount -> x.Unmount())

        return ()
    }

type PrintFormat =
    | Decorated
    | Simple

let printDrives (format: PrintFormat) =
    let partitions = fetchExternalPartitions ()

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
