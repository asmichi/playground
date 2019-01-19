module DataStructuresAndAlgorithms.Chapter3.MutableLinkedList
open Xunit
open Swensen.Unquote
open System.Collections
open System.Collections.Generic

// Properties: https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/members/properties

//type MutableLinkedListNode0<'a> = 
//    val mutable private value : 'a
//    val mutable private next : MutableLinkedListNode0<'a> option

//    new (value, next) =
//        {value = value; next = next}
//    member this.Value 
//        with get() = this.value
//        and  set v = this.value <- v

//    member this.Next 
//        with get() = this.next
//        and  set v = this.next <- v

type MutableLinkedListNode<'a>(value : 'a, next : MutableLinkedListNode<'a> option) =
    member val public Value = value with get, set
    member val public Next = next with get, set

    member public this.LastNode =
        match this.Next with
        | Some n -> n.LastNode
        | None -> this

    member public this.InsertAfterThis v =
        let newNext = MutableLinkedListNode<'a>(v, this.Next)
        this.Next <- Some(newNext)
        this

    member public this.RemoveLast() =
        match this.Next with
        | Some n ->
            match n.Next with
            | Some nn -> nn.RemoveLast()
            | None -> this.Next <- None
        | None -> failwith "No successor."

    member public this.RemoveNode n =
        match this.Next with
        | Some nextNode ->
            if nextNode = n then
                this.Next <- nextNode.Next
            else
                nextNode.RemoveNode n
        | None -> failwith "element not found"

    member public this.Nth i =
        if i > 0 then
            match this.Next with
            | Some n -> n.Nth (i - 1)
            | None -> failwith "element not found"
        else
            this

type MutableLinkedList<'a> =
    interface IEnumerable<'a> with
        member this.GetEnumerator(): IEnumerator<'a> = 
            this.ToSeq.GetEnumerator()
        member this.GetEnumerator(): IEnumerator = 
            (this.ToSeq :> IEnumerable).GetEnumerator()
        
    val mutable private head : MutableLinkedListNode<'a> option
    new () = MutableLinkedList(None)
    new (head) = { head = head }

    member public this.ToSeq = 
        this.head
        |> Seq.unfold (fun n -> 
            match n with
            | Some n -> Some(n.Value, n.Next)
            | None -> None)

    member private this.FirstNode =
        this.head

    member private this.LastNode = 
        match this.head with
        | Some n -> Some(n.LastNode)
        | None -> None

    member public this.Tail =
        match this.head with
        | Some x -> MutableLinkedList(x.Next)
        | None -> failwith "List is empty."

    member public this.AddFirst v =
        this.head <- Some(MutableLinkedListNode(v, this.head))
        this

    member public this.AddLast v =
        let next = Some(MutableLinkedListNode(v, None))
        match this.head with
            | Some n -> n.LastNode.Next <- next
            | None -> this.head <- next
        this

    member public this.RemoveFirst() =
        this.head <- 
            match this.head with
            | Some n -> n.Next
            | None -> failwith "List is empty."
        this

    member public this.RemoveLast() =
        match this.head with
        | Some n ->
            match n.Next with
            | Some nn -> n.RemoveLast |> ignore
            | None -> this.head <- None
        | None -> failwith "List is empty."
        this

    member public this.RemoveNode n =
        match this.head with
        | Some h ->
            if h = n then
                this.head <- h.Next
            else
                h.RemoveNode n
        | None -> failwith "List is empty."
        this

    member public this.NthNode i =
        match this.head with
        | Some n -> n.Nth i
        | None -> failwith "List is empty."

    member public this.AddAfter (n : MutableLinkedListNode<'a>) v =
        n.InsertAfterThis v |> ignore
        this

module MutableLinkedList =
    let toSeq (list : MutableLinkedList<'a>) = list.ToSeq
    let fromSeq<'a> : ('a seq -> MutableLinkedList<'a>) =
        MutableLinkedList<'a>()
        |> Seq.fold (fun list x -> list.AddLast(x))

[<Fact>]
let ``Can construct from seq``() = 
    let sut = MutableLinkedList.fromSeq [ 1; 2 ]

    (Seq.toList sut) =! [ 1; 2 ]

[<Fact>]
let ``Can add element at the front``() = 
    let sut = MutableLinkedList.fromSeq [ 1 ]

    sut.AddFirst(2) |> ignore

    (Seq.toList sut) =! [ 2; 1 ]


[<Fact>]
let ``Can add element at the end``() = 
    let sut = MutableLinkedList.fromSeq [ 1 ]

    sut.AddLast(2) |> ignore

    (Seq.toList sut) =! [ 1; 2 ]

[<Fact>]
let ``Can remove element at the front``() = 
    let sut = MutableLinkedList.fromSeq [ 1; 2 ]

    sut.RemoveFirst() |> ignore
    (Seq.toList sut) =! [ 2 ]

    sut.RemoveFirst() |> ignore
    (Seq.toList sut) =! [ ]

[<Fact>]
let ``Can remove element at the end``() = 
    let sut = MutableLinkedList.fromSeq [ 1; 2 ]

    sut.RemoveLast() |> ignore
    (Seq.toList sut) =! [ 1 ]

    sut.RemoveLast() |> ignore
    (Seq.toList sut) =! [ ]

[<Fact>]
let ``Can remove element in the middle``() = 
    let sut = MutableLinkedList.fromSeq [ 1; 2; 3 ]

    sut.RemoveNode(sut.NthNode 1) |> ignore
    (Seq.toList sut) =! [ 1; 3 ]

