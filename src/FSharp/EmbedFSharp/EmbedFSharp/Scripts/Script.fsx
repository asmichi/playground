#r "System"
#r "System.Collections.Immutable"
#if !EMBEDFSHARP
#I "../bin/Debug"
#r "EmbedFSharp.exe"
#endif
#load "inc/Inc1.fs"
#load "inc/Inc2.fs"

// System.Diagnostics.Debugger.Launch()
#if EMBEDFSHARP_DEBUG
if System.Diagnostics.Debugger.IsAttached then
    System.Diagnostics.Debugger.Break()
#endif

printfn "%s" __SOURCE_DIRECTORY__
printfn "Inc1Value: %d" Inc1Value
printfn "Inc2Value: %d" Inc2Value

let xs = [1 ; 2; 3; 4 ]

for x in xs do
    let x2 = x * x
    printfn "%d" x2

printfn "ContextName: %s" MyAppAccessor.ContextName
MyAppAccessor.Call("hello, myapp")

