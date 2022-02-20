open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

[<Literal>]
let DEFAULT_BUFFER_SIZE = 4096

type Async with
    static member AwaitPlainTask (task : Task) =
        task.ContinueWith(ignore) |> Async.AwaitTask

let copyToAsync source dest =
    async {
        printfn "Copying %s to %s" source dest
        use sourceFile = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, DEFAULT_BUFFER_SIZE, true);
        use destFile = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, DEFAULT_BUFFER_SIZE, true);
        do! sourceFile.CopyToAsync(destFile) |> Async.AwaitPlainTask
    }

let rec RetryRun count (work:Async<'T>) = 
   async { 
       try 
         // Try to run the work
         return! work
       with e ->
         // Retry if the count is larger than 0, otherwise fail
         if count > 0 then return! RetryRun (count - 1) work 
         else return raise e 
    }

let makeDest(s:FileInfo,d) =
    async {
        try 
            RetryRun 15, (copyToAsync s.FullName d) |> Async.StartAsTask |> ignore
        with e ->
            printfn "failed to upload %s" s.FullName |> ignore
    }

let rec directoryCopy srcPath dstPath copySubDirs =
    if not <| System.IO.Directory.Exists(srcPath) then
        let msg = System.String.Format("Source directory does not exist or could not be found: {0}", srcPath)
        raise (System.IO.DirectoryNotFoundException(msg))

    if not <| System.IO.Directory.Exists(dstPath) then
        System.IO.Directory.CreateDirectory(dstPath) |> ignore

    let srcDir = new System.IO.DirectoryInfo(srcPath)
    
    let filList = new Dictionary<FileInfo, string>()

    for file in srcDir.GetFiles() do
        let temppath = System.IO.Path.Combine(dstPath, file.Name)
        filList.Add(file, temppath)
        |> ignore

    filList |> Seq.map (fun (KeyValue(k,v)) -> makeDest(k,v)) |> Async.Parallel |> Async.RunSynchronously |>  ignore

    if copySubDirs then
        for subdir in srcDir.GetDirectories() do
            let dstSubDir = System.IO.Path.Combine(dstPath, subdir.Name)
            directoryCopy subdir.FullName dstSubDir copySubDirs
        

[<EntryPoint>]
let main(args) =
    printfn "Hello from File Copier built using F#"
   
    if args.Length < 2 || args.Length > 2 then
        printfn "To Run please type \"filecp \"d:\Dir1\" \"c:\Dir2\""
        |> ignore
    else
        try
            directoryCopy args[0] args[1] true
            |> ignore
        with e -> 
            printfn "Error: %s" e.Message
    0
