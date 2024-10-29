module ExtDriver.Disks

open FSharp.Json
open Microsoft.FSharp.Collections

type PartitionEntry = {
    name: string
    fstype: string
    label: string option
    uuid: string
    mountpoints: string option list
}
    with
        member this.Mountpoint =
            this.mountpoints
            |> List.tryHead
            |> Option.flatten
            
        member this.Unmount () =
            execResult $"udisksctl unmount --block-device /dev/{this.name}" 
        
        member this.Mount () =
            execResult $"udisksctl mount --block-device /dev/{this.name}"
        
            
type DiskEntry = {
    name: string
    children: PartitionEntry list
}
    with
        member this.IsExternal =
            (exec $"realpath /sys/block/{this.name}" |? "").Contains "usb"
        
        
type _LsblkJson = {
    blockdevices: DiskEntry list
}

let fetchExternalDrives () = 
     exec "lsblk -f --json"
     |> Option.get
     |> Json.deserialize<_LsblkJson>
     |> _.blockdevices
     |> List.filter _.IsExternal
    
let fetchExternalPartitions () =
    fetchExternalDrives ()
    |> List.collect _.children