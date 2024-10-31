module ExtDriver.Disks

open FSharp.Json
open Microsoft.FSharp.Collections

type PartitionEntry =
    { name: string
      fstype: string
      label: string option
      uuid: string
      mountpoints: string option list }

    member this.Mountpoint = this.mountpoints |> List.tryHead |> Option.flatten

    member this.Unmount() =
        execResult $"udisksctl unmount --block-device /dev/{this.name}"

    member this.Mount() =
        execResult $"udisksctl mount --block-device /dev/{this.name}"


type DiskEntry =
    { name: string
      children: PartitionEntry list }

    member private this._isExternal =
        lazy ((exec $"realpath /sys/block/{this.name}" |? "").Contains "usb")

    member this.IsExternal = this._isExternal.Value


type _LsblkJson = { blockdevices: DiskEntry list }

let fetchExternalDrives () =
    execResult "lsblk -f --json"
    |> Result.mapError (fun _ -> "lsblk cannot be executed")
    |> Result.map (Json.deserialize<_LsblkJson> >> _.blockdevices >> List.filter _.IsExternal)

let fetchExternalPartitions =
    fetchExternalDrives >> Result.map (List.collect _.children)
