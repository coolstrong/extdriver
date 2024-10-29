[<AutoOpen>]
module ExtDriver.Utils

open Fli

let tap f x =
    ignore (f x)
    x

let always x = fun () -> x

module Option =
    let toResult errValue option =
        match option with
        | Some x -> Ok x
        | None -> Error errValue

let (|?) option defaultValue =
    option |> Option.defaultValue defaultValue

module Result =
    let fromAction f =
        try
            Ok(f ())
        with e ->
            Error e


let exec (cmd: string) =
    Command.execute (
        cli {
            Shell BASH
            Command cmd
        }
    )
    |> _.Text

let execResult (cmd: string) =
    let output =
        Command.execute (
            cli {
                Shell BASH
                Command cmd
            }
        )

    if output.Error.IsSome then
        Error(output.Error |? "")
    else
        Ok(output.Text |? "")
