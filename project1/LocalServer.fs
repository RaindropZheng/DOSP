
open System.Threading
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.IO
open System.Security.Cryptography
open System.Text


let configuration =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 2001
            }
        }"

let system = System.create "ServerSystem" configuration

let localServer = 
    spawn system "localServer"
        (fun mailbox ->
            let rec loop() =
                actor {
                    let! message = mailbox.Receive()//receive message from client
                    let sender = mailbox.Sender()//send message to client

                    let length = String.length message
                
                    if message = "localClient"
                    then
                        //printfn "get output from client (fileName+ip)********* %s" message
                        sender <! 4
                            
                    elif length > 1
                    then
                        printfn "%s" message

                    else
                        failwith "Unknown message"

                    return! loop()
                    }
            loop()
        )

Console.ReadLine() |> ignore


