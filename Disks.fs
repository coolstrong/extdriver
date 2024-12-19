module ExtDriver.Disks

open System
open System.Text.RegularExpressions
open FSharp.Json
open Microsoft.FSharp.Collections
open FsToolkit.ErrorHandling

type PartitionEntry =
    { name: string
      fstype: string
      label: string option
      uuid: string
      mountpoints: string option list }

    member this.Mountpoint = this.mountpoints |> List.tryHead |> Option.flatten

    member this.GetInhibitors() =
        match this.Mountpoint with
        | None -> Ok []
        | Some mountpoint ->
            result {
                let! output = (execResult $"lsof {mountpoint}")

                let! processes =
                    output.Split('\n')
                    |> Seq.skip 1
                    |> Seq.traverseResultM (fun x ->
                        let parts = Regex.Split(x, @"\s+")

                        if parts.Length < 2 then
                            Result.Error "Unexpected lsof output"
                        else
                            Ok {| command = parts[0]; pid = parts[1] |})

                return
                    processes
                    |> Seq.filter (fun x -> x.command <> "lsof")
                    |> Seq.distinct
                    |> Seq.toList


            }

    member this.Mount() =
        execResult $"udisksctl mount --block-device /dev/{this.name}"

    member this.Unmount() =
        execResult $"udisksctl unmount --block-device /dev/{this.name}"
        |> Result.mapError (fun x ->
            if x.EndsWith("target is busy") then
                match this.GetInhibitors() with
                | Error _ -> x
                | Ok list ->
                    let inhibitorsList =
                        list |> List.map (fun x -> $"{x.command} (pid:{x.pid})") |> String.concat ", "

                    $"Following processes prevent unmounting: {inhibitorsList}"
            else
                x)

type DiskEntry =
    { name: string
      children: PartitionEntry list }

    member private this._isExternal =
        lazy ((exec $"realpath /sys/block/{this.name}" |? "").Contains "usb")

    member this.IsExternal = this._isExternal.Value


type _LsblkJson = { blockdevices: DiskEntry list }

let fetchExternalDrives () =
    execResult "lsblk -f --json"
    |> Result.mapError (always "lsblk cannot be executed")
    |> Result.map (Json.deserialize<_LsblkJson> >> _.blockdevices >> List.filter _.IsExternal)

let fetchExternalPartitions =
    fetchExternalDrives >> Result.map (List.collect _.children)
