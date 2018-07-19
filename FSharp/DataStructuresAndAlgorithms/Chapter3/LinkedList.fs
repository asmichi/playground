module DataStructuresAndAlgorithms.Chapter3.LinkedList
open Xunit
open Swensen.Unquote

type LinkedListNode<'a> = { Value : 'a; Next: LinkedListNode<'a> option; } with
    member public this.AddLast v =
        { this with
            Next =
                match this.Next with
                | Some n -> Some(n.AddLast v)
                | None -> Some({ Value = v; Next = None; })
        }

    member public this.RemoveLast = 
        match this.Next with
        | Some n -> Some({ this with Next = n.RemoveLast; })
        | None -> None

    member public this.InsertAt i v =
        if i = 0 then
            { this with Next = Some({Value = v; Next = this.Next; }) }
        else
            match this.Next with
            | Some n -> { this with Next = Some(n.InsertAt (i - 1) v) }
            | None -> failwith "element not found"

    member public this.RemoveAt i =
        if i = 0 then
            this.Next
        else
            match this.Next with
            | Some n -> Some({ this with Next = n.RemoveAt (i - 1) })
            | None -> failwith "element not found"

module LinkedListNode =
    let create<'a>(v : 'a, next) : LinkedListNode<'a> =
        { Value = v; Next = next; }

    let toList<'a> : LinkedListNode<'a> option -> 'a list =
        List.unfold(function
            | Some n -> Some(n.Value, n.Next)
            | None -> None)

    let rec fromList<'a> : 'a list -> LinkedListNode<'a> option =
        function
        | h :: t -> Some(create(h, (fromList t)))
        | [] -> None

type LinkedList<'a>(head : LinkedListNode<'a> option) = 
    new () = LinkedList(None)
    new (head : LinkedListNode<'a>) = LinkedList(Some(head))

    member public this.ToList =
        head
        |> List.unfold(function
            | Some n -> Some(n.Value, n.Next)
            | None -> None)

    member public this.Head =
        head

    member public this.Tail =
        match head with
        | Some h -> LinkedList(h.Next)
        | None -> failwith "List is empty."

    member public this.AddFirst v =
        Some(LinkedListNode.create(v, head))
        |> LinkedList

    member public this.AddLast v =
        match head with
        | Some h -> LinkedList(h.AddLast v)
        | None -> LinkedList(LinkedListNode.create(v, None))

    member public this.RemoveFirst() = 
        match head with
        | Some h -> LinkedList(h.Next)
        | None -> failwith "list is empty"

    member public this.RemoveLast() = 
        match head with
        | Some h -> LinkedList(h.RemoveLast)
        | None -> failwith "list is empty"

    member public this.InsertAt i v =
        if i = 0 then
            LinkedList(LinkedListNode.create(v, head))
        else
            match head with
            | Some h -> LinkedList(h.InsertAt (i - 1) v)
            | None -> failwith "element not found"

    member public this.RemoveAt i =
        match head with
        | Some h -> LinkedList(h.RemoveAt i)
        | None -> failwith "element not found"

module LinkedList =
    let head (linkedList : LinkedList<'a>) =
        linkedList.Head

    let toList<'a> = 
        head
        >> LinkedListNode.toList 

    let fromList<'a> =
        LinkedListNode.fromList<'a>
        >> LinkedList<'a>

[<Fact>]
let ``Can construct from list``() = 
    let sut = LinkedList.fromList [ 1; 2 ]

    (LinkedList.toList sut) =! [ 1; 2 ]

[<Fact>]
let ``Can add element at the front``() = 
    let sut = LinkedList.fromList [ 1 ]

    let sut = sut.AddFirst(2)

    (LinkedList.toList sut) =! [ 2; 1 ]

[<Fact>]
let ``Can add element at the end``() = 
    let sut = LinkedList.fromList [ 1 ]

    let sut = sut.AddLast(2)

    (LinkedList.toList sut) =! [ 1; 2 ]

[<Fact>]
let ``Can remove element at the front``() = 
    let sut = LinkedList.fromList [ 1; 2 ]

    let sut = sut.RemoveFirst()
    (LinkedList.toList sut) =! [ 2 ]

    let sut = sut.RemoveFirst()
    (LinkedList.toList sut) =! [ ]

[<Fact>]
let ``Can remove element at the end``() = 
    let sut = LinkedList.fromList [ 1; 2 ]

    let sut = sut.RemoveLast()
    (LinkedList.toList sut) =! [ 1 ]

    let sut = sut.RemoveLast()
    (LinkedList.toList sut) =! [ ]

[<Fact>]
let ``Can insert element in the middle``() = 
    let sut = LinkedList.fromList [ 1; 2; 3; ]

    let sut = sut.InsertAt 0 4
    let sut = sut.InsertAt 2 5
    (LinkedList.toList sut) =! [ 4; 1; 5; 2; 3; ]

[<Fact>]
let ``Can remove element in the middle``() = 
    let sut = LinkedList.fromList [ 1; 2; 3; 4; 5 ]

    let sut = sut.RemoveAt 2
    let sut = sut.RemoveAt 0
    (LinkedList.toList sut) =! [ 2; 4; 5; ]

