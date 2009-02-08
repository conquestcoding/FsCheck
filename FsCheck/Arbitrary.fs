﻿#light

namespace FsCheck

module Arbitrary

open TypeClass
open Generator
open System
open ReflectArbitrary
open Functions

let private fraction (a:int) (b:int) (c:int) = 
    double a + double b / (abs (double c) + 1.0) 

///A collection of default generators.
type Arbitrary() =
    ///Generates (), of the unit type.
    static member Unit() = 
        { new Arbitrary<unit>() with
            override x.Arbitrary = gen { return () } 
            override x.CoArbitrary _ = variant 0
        }
    ///Generates arbitrary bools.
    static member Bool() = 
        { new Arbitrary<bool>() with
            override x.Arbitrary = elements [true; false] 
            override x.CoArbitrary b = if b then variant 0 else variant 1
        }
    ///Generate arbitrary int that is between -size and size.
    static member Int() = 
        { new Arbitrary<int>() with
            override x.Arbitrary = sized <| fun n -> choose (-n,n) 
            override x.CoArbitrary n = variant (if n >= 0 then 2*n else 2*(-n) + 1)
            override x.Shrink n = 
                let (|>|) x y = abs x > abs y 
                seq {   if n < 0 then yield -n 
                        yield! Seq.unfold (fun st -> let st = st / 2 in Some (n-st, st)) n 
                                |> Seq.cons 0 
                                |> Seq.take_while ((|>|) n) }
                |> Seq.distinct
        }
    ///Generates arbitrary floats, NaN, NegativeInfinity, PositiveInfinity, Maxvalue, MinValue, Epsilon included fairly frequently.
    static member Float() = 
        { new Arbitrary<float>() with
            override x.Arbitrary = 
                frequency   [(6, liftGen3 fraction arbitrary arbitrary arbitrary)
                            ;(1, elements [ Double.NaN; Double.NegativeInfinity; Double.PositiveInfinity])
                            ;(1, elements [ Double.MaxValue; Double.MinValue; Double.Epsilon])]
            override x.CoArbitrary fl = 
                let d1 = sprintf "%g" fl
                let spl = d1.Split([|'.'|])
                let m = if (spl.Length > 1) then spl.[1].Length else 0
                let decodeFloat = (fl * float m |> int, m )
                coarbitrary <| decodeFloat
            override x.Shrink fl =
                let (|<|) x y = abs x < abs y
                seq {   if Double.IsInfinity fl || Double.IsNaN fl then 
                            yield 0.
                        else
                            if fl < 0.0 then yield -fl
                            let truncated = truncate fl
                            if truncated |<| fl then yield truncated }
                |> Seq.distinct
        }
    ///Generates arbitrary chars, between ASCII codes Char.MinValue and 127.
    static member Char() = 
        { new Arbitrary<char>() with
            override x.Arbitrary = fmapGen char (choose (int Char.MinValue, 127))
            override x.CoArbitrary c = coarbitrary (int c)
            override x.Shrink c =
                seq { for c' in ['a';'b';'c'] do if c' < c || not (Char.IsLower c) then yield c' }
        }
    ///Generates arbitrary strings, which are lists of chars generated by Char.
    static member String() = 
        { new Arbitrary<string>() with
            override x.Arbitrary = fmapGen (fun chars -> new String(List.to_array chars)) arbitrary
            override x.CoArbitrary s = s.ToCharArray() |> Array.to_list |> coarbitrary
            override x.Shrink s = s.ToCharArray() |> Array.to_list |> shrink |> Seq.map (fun chars -> new String(List.to_array chars))
        }
    ///Genereate a 2-tuple.
    static member Tuple2() = 
        { new Arbitrary<'a*'b>() with
            override x.Arbitrary = liftGen2 (fun x y -> (x,y)) arbitrary arbitrary
            //extra paranthesis are needed here, otherwise F# gets confused about the number of arguments
            //and doesn't correctly see that this really overriddes the right method
            override x.CoArbitrary ((a,b)) = coarbitrary a >> coarbitrary b
            override x.Shrink ((x,y)) = 
                seq {   for x' in shrink x -> (x',y ) 
                        for y' in shrink y -> (x ,y') }
        }
    ///Genereate a 3-tuple.
    static member Tuple3() = 
        { new Arbitrary<'a*'b*'c>() with
            override x.Arbitrary = liftGen3 (fun x y z -> (x,y,z)) arbitrary arbitrary arbitrary
            override x.CoArbitrary ((a,b,c)) = coarbitrary a >> coarbitrary b >> coarbitrary c
            override x.Shrink ((x,y,z)) = 
                seq {   for x' in shrink x -> (x',y ,z ) 
                        for y' in shrink y -> (x ,y',z ) 
                        for z' in shrink z -> (x ,y ,z') }
        }
    ///Genereate a 4-tuple.
    static member Tuple4() = 
        { new Arbitrary<'a*'b*'c*'d>() with
            override x.Arbitrary = liftGen4 (fun x y z u-> (x,y,z,u)) arbitrary arbitrary arbitrary arbitrary
            override x.CoArbitrary ((a,b,c,d)) = coarbitrary a >> coarbitrary b >> coarbitrary c >> coarbitrary d
            override x.Shrink ((x,y,z,u)) = 
                seq {   for x' in shrink x -> (x',y ,z ,u ) 
                        for y' in shrink y -> (x ,y',z ,u ) 
                        for z' in shrink z -> (x ,y ,z',u ) 
                        for u' in shrink u -> (x ,y ,z ,u')}
        }
    ///Genereate a 5-tuple.
    static member Tuple5() = 
        { new Arbitrary<'a*'b*'c*'d*'e>() with
            override x.Arbitrary = liftGen5 (fun x y z u v-> (x,y,z,u,v)) arbitrary arbitrary arbitrary arbitrary arbitrary
            override x.CoArbitrary ((a,b,c,d,e)) = coarbitrary a >> coarbitrary b >> coarbitrary c >> coarbitrary d >> coarbitrary e
            override x.Shrink ((x,y,z,u,v)) = 
                seq {   for x' in shrink x -> (x',y ,z ,u ,v ) 
                        for y' in shrink y -> (x ,y',z ,u ,v ) 
                        for z' in shrink z -> (x ,y ,z',u ,v ) 
                        for u' in shrink u -> (x ,y ,z ,u',v )
                        for v' in shrink v -> (x ,y ,z ,u ,v') }
        }
    ///Genereate a 6-tuple.
    static member Tuple6() = 
        { new Arbitrary<'a*'b*'c*'d*'e*'f>() with
            override x.Arbitrary = 
                liftGen6 (fun x y z u v w-> (x,y,z,u,v,w)) arbitrary arbitrary arbitrary arbitrary arbitrary arbitrary
            override x.CoArbitrary ((a,b,c,d,e,f)) = 
                coarbitrary a >> coarbitrary b >> coarbitrary c >> coarbitrary d >> coarbitrary e >> coarbitrary f
            override x.Shrink ((x,y,z,u,v,w)) = 
                seq {   for x' in shrink x -> (x',y ,z ,u ,v ,w ) 
                        for y' in shrink y -> (x ,y',z ,u ,v ,w ) 
                        for z' in shrink z -> (x ,y ,z',u ,v ,w ) 
                        for u' in shrink u -> (x ,y ,z ,u',v ,w )
                        for v' in shrink v -> (x ,y ,z ,u ,v',w )
                        for w' in shrink w -> (x ,y ,z ,u ,v ,w') }
        }
    ///Generate an option value that is 'None' 1/4 of the time.
    static member Option() = 
        { new Arbitrary<option<'a>>() with
            override x.Arbitrary = frequency [(1, gen { return None }); (3, liftGen Some arbitrary)]
            override x.CoArbitrary o = 
                match o with 
                | None -> variant 0
                | Some y -> variant 1 >> coarbitrary y
            override x.Shrink o =
                match o with
                | Some x -> seq { yield None; for x' in shrink x -> Some x' }
                | None  -> Seq.empty
        }
    ///Generate a list of values. The size of the list is between 0 and the test size + 1.
    static member FsList() = 
        { new Arbitrary<list<'a>>() with
            override x.Arbitrary = sized (fun n -> gen.Bind(choose(0,n+1 (*avoid empties*)), vector arbitrary))
            override x.CoArbitrary l = 
                match l with
                | [] -> variant 0
                | x::xs -> coarbitrary x << variant 1 << coarbitrary xs
            override x.Shrink l =
                match l with
                | [] ->         Seq.empty
                | (x::xs) ->    seq { yield xs
                                      for xs' in shrink xs -> x::xs'
                                      for x' in shrink x -> x'::xs }
        }
    ///Generate an object.
    static member Object() =
        { new Arbitrary<obj>() with
            override x.Arbitrary = 
                oneof [ fmapGen box <| arbitrary<char>; fmapGen box <| arbitrary<string>; fmapGen box <| arbitrary<bool> ]
            override x.CoArbitrary o = 
                match o with
                | :? char as c -> variant 0 >> coarbitrary c
                | :? string as s -> variant 1 >> coarbitrary s
                | :? bool as b -> variant 2 >> coarbitrary b
                | _ -> failwith "Unknown domain type in coarbitrary of obj"
            override x.Shrink o =
                seq {
                    match o with
                    | :? char as c -> yield box true; yield box false; yield! shrink c |> Seq.map box
                    | :? string as s -> yield box true; yield box false; yield! shrink s |> Seq.map box
                    | :? bool as b -> yield! Seq.empty
                    | _ -> failwith "Unknown type in shrink of obj"
                }
        }
    //Generate a rank 1 array.
    static member Array() =
        { new Arbitrary<'a[]>() with
            override x.Arbitrary = arbitrary |> fmapGen List.to_array
            override x.CoArbitrary a = a |> Array.to_list |> coarbitrary
            override x.Shrink a = a |> Array.to_list |> shrink |> Seq.map List.to_array
        }
     ///Generate a function value.
    static member Arrow() = 
        { new Arbitrary<'a->'b>() with
            override x.Arbitrary = promote (fun a -> coarbitrary a arbitrary)
            override x.CoArbitrary f gen = 
                gen {   let x = arbitrary
                        return! coarbitrary (fmapGen f x) gen } 
        }
    static member Function() =
        { new Arbitrary<Function<'a,'b>>() with
            override x.Arbitrary = fmapGen toFunction arbitrary
            override x.Shrink f = 
                let update x' y' f x = if x = x' then y' else f x
                seq { for (x,y) in f.Table do 
                        for y' in shrink y do 
                            yield toFunction (update x y' f.Value) }
        }
    static member CatchAll() =
        { new Arbitrary<'a>() with
            override x.Arbitrary = reflectGen
            override x.Shrink a = reflectShrink a
        }
