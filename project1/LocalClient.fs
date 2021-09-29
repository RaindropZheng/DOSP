
open System
open System.Threading
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.IO
open System.Security.Cryptography
open System.Text
open System.Collections.Generic
open System.Diagnostics


//client config
let configuration =
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                deployment {
                    /remoteecho {
                        remote = ""akka.tcp://ServerSystem@localhost:2001""
                    }
                }
            }
            remote.helios.tcp {
                hostname = localhost
                port = 0
            }
        }")

let system = ActorSystem.Create("ClientSystem",configuration)
let address = Console.ReadLine()//localClient localhost
let input = address.Split ' '
let getServer = system.ActorSelection("akka.tcp://ServerSystem@" + input.[1] + ":2001/user/localServer") //get actor from server

//This function is used for calculating hash_value
let cal_hash (name:string) : string = 
    let data = Encoding.ASCII.GetBytes(name)
    
    let sha256Hash = SHA256Managed.Create()
    let result = sha256Hash.ComputeHash(data)

    let fpEncode (bytes:byte[]): string =
        let byteToHex (b:byte) = b.ToString("x2")
        Array.map byteToHex bytes |> String.concat ""

    let answer = fpEncode result
    answer


//This function is for generate random string
let ranStr n =
    let r = Random()
    let random = Random()
    let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
    let sz = Array.length chars in
    String(Array.init n (fun _ -> chars.[r.Next sz]))

//create multiple actors and make them work parallelly
let actorList = [
    for i in 1 .. 32 do
        let name = "HashActor" + i.ToString()
        let temp =
            spawn system name
            <| fun mailbox ->
                actor {
                    let! message = mailbox.Receive()
                    //let sender = mailbox.Sender()
                   match box message with
                   | :? int as zero_num ->
                       let suffix_num = 10 //can be changed
                       let name = "name;idNum; "
                       let mutable continueLooping = 1000000
                       while continueLooping > 0 do
                           for i = 1 to suffix_num do
                               let suffix = ranStr i
                               let data = name + suffix
                               let result = cal_hash data
                               let mutable count = 0
                               for j = 0 to zero_num - 1 do
                                   if result.[j] = '0' then count <- count + 1
                                   else count <- 0
                                   if count = zero_num
                                   then
                                       let answer = name + result
                                       if result.[j+1] <> '0' then getServer <! answer
                           continueLooping <- continueLooping - 1
                   | _ -> failwith "unknow message"
                    
                }
        yield temp
    ]



//mailing actor
let localClient =
    spawn system "localClient"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()
                match box message with
                | :? int as zero_num->
                    //printfn "get data from server ******* %d *********" zero_num
                    for i in 0 .. 31 do
                        //calculate a certain period of one actor's work
                        let proc = Process.GetCurrentProcess()
                        let cpu_time_stamp = proc.TotalProcessorTime
                        let timer = new Stopwatch()
                        timer.Start()

                        actorList.Item(i) <! zero_num

                        System.Threading.Thread.Sleep(5000)
                        let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
                        printfn "Actor %d is activated" (i+1)
                        printfn "CPU time = %dms" (int64 cpu_time)
                        printfn "Absolute time = %dms" timer.ElapsedMilliseconds
                    //getServer <! "end"
                | :? ActorSelection as useActor ->
                    useActor <! input.[0]
                | _ ->  failwith "Unknown message"
                return! loop()
                }
        loop()


localClient <! getServer //use client acotr to receive and send message from/to server 



Console.ReadLine() |> ignore //to avoid program terminate



