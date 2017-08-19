[<AutoOpen>]
module Inc2

let Inc2Value = 2
let private SourceDir = __SOURCE_DIRECTORY__

printfn "Inc2: %s" SourceDir

for x in [1 ; 2; 3; 4 ] do
    printfn "Inc2: %d" x


