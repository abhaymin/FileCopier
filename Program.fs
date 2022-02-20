open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

[<Literal>]
let DEFAULT_BUFFER_SIZE = 4096

type Async with
    static member AwaitPlainTask (task : Task) =
        task.ContinueWith(ignore) |> Async.AwaitTask

type RetryAsyncBuilder() =
  member x.ReturnFrom(comp) = comp // Just return the computation
  member x.Return(v) = async { return v } // Return value inside async
  member x.Delay(f) = async { return! f() } // Wrap function inside async
  member x.Bind(work, f) =
    async { 
      try 
        // Try to call the input workflow
        let! v = work
        // If it succeeds, try to do the rest of the work
        return! f v
      with e ->
        // In case of exception, call Bind to try again
        return! x.Bind(work, f) }


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

    directoryCopy args[0] args[1] true
    0
