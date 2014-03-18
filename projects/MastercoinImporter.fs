module MastercoinImporter

open FSharp.Data
open System 
open System.IO
open System.Net
open System.Text.RegularExpressions

let ePoch = new DateTime(1970, 1, 1, 0, 0, 0)

type Currency = MSC = 0 | TMSC = 1

type tx = {
            sender : String
            receiver : String
            amount: Decimal
            block : int
            currencyId : int
            details : String
            icon : String
            index : int
            invalid : bool
            method_ : String
            transactionType : int
            transactionVersion : int
            txHash : String
            txTime : DateTime
            txType : String
         }


// msc  balances: https://masterchain.info/mastercoin_verify/addresses/0
// tmsc balances: https://masterchain.info/mastercoin_verify/addresses/1
 
// samples:
// address tx:     https://masterchain.info/addr/1ACqY4wjo1hMehDpD8jipWfmX7yvuZpUn1.json
// tx:             https://masterchain.info/tx/12db8ac3c80cc09083a84d66bfdf2e0f73c54f44a11ffec164dd6dd05e488629.json
// tx per address: https://masterchain.info/mastercoin_verify/transactions/1zobh1SwsVdk3uMjEHeGV86hjNZeJ5CwC
 

type Addr =     JsonProvider<"addr.json">
type Balance =  JsonProvider<"balances.json">

let lineSequence(file) = 
        let reader = File.OpenText(file) 
        Seq.unfold(fun line -> 
            if line = null then 
                reader.Close() 
                None 
            else 
                Some(line,reader.ReadLine())) (reader.ReadLine())

// Export balances for all addresses for a specified currency
let ExportBalances currency = 
    let uri = "https://masterchain.info/mastercoin_verify/addresses/" + ((int)currency).ToString()
    let req = WebRequest.Create(uri)

    let resp = req.GetResponse()
    let stream = resp.GetResponseStream()
    use reader = new IO.StreamReader(stream)

    let balances = Balance.Parse(reader.ReadToEnd())

    use writer = new StreamWriter(String.Format("balances{0}.csv", currency), false, System.Text.Encoding.ASCII)
    for b in balances do
        writer.WriteLine(String.Join(",", b.Address, b.Balance))

// declare transaction sequences
let mutable txMSC = Seq.empty<tx>
let mutable txTMSC = Seq.empty<tx>

// Send webrequest and get transaction for Address 'a
let RequestTransactions (a:String) = 
    let req = WebRequest.Create(String.Format("https://masterchain.info/addr/{0}.json", a))
    let resp = req.GetResponse()
    let stream = resp.GetResponseStream()
    use reader = new IO.StreamReader(stream)

    let root = Addr.Parse(reader.ReadToEnd())

    let sentMSC = seq {for tx in root.``0``.SentTransactions do
                            yield { sender = tx.FromAddress
                                    receiver = tx.ToAddress
                                    amount = tx.FormattedAmount
                                    block = tx.Block
                                    currencyId = tx.CurrencyId
                                    details = tx.Details
                                    icon = tx.Icon
                                    index = tx.Index
                                    invalid = tx.Invalid
                                    method_ = tx.Method
                                    transactionType = tx.TransactionType
                                    transactionVersion = -1//tx.TransactionVersion  <- throws exception on some addresses
                                    txHash = tx.TxHash
                                    txTime = ePoch.AddTicks(tx.TxTime)
                                    txType = tx.TxTypeStr}} |> Seq.toList

    txMSC <- Seq.append txMSC sentMSC

    let sentTMSC = seq {for tx in root.``1``.SentTransactions do
                            yield { sender = tx.FromAddress
                                    receiver = tx.ToAddress
                                    amount = tx.FormattedAmount
                                    block = tx.Block
                                    currencyId = tx.CurrencyId
                                    details = tx.Details
                                    icon = tx.Icon
                                    index = tx.Index
                                    invalid = tx.Invalid
                                    method_ = tx.Method
                                    transactionType = tx.TransactionType
                                    transactionVersion = -1//tx.TransactionVersion  <- throws exception on some addresses
                                    txHash = tx.TxHash
                                    txTime = ePoch.AddTicks(tx.TxTime)
                                    txType = tx.TxTypeStr}} |> Seq.toList

    txTMSC <- Seq.append txTMSC sentTMSC
    
    Console.WriteLine(String.Format("https://masterchain.info/addr/{0}.json {1} {2}", a, sentMSC.Length, sentTMSC.Length))

[<EntryPoint>]
let Main args =

    let mutable addresses = Seq.empty
    if args.Length = 0 then
        ExportBalances Currency.MSC
        ExportBalances Currency.TMSC
        use reader = new StreamReader("balancesMSC.csv")
        addresses <- seq {while not reader.EndOfStream do
                            yield reader.ReadLine().Split(',').[0]
                        }
    elif args.[0] = "f" then
        use reader = new StreamReader("faucet.txt")
        addresses <- seq {while not reader.EndOfStream do
                            let addr = reader.ReadLine()
                            if String.IsNullOrEmpty(addr) = false then
                                yield addr
                         } |> Seq.toArray

    addresses |>  Seq.iter (fun a -> RequestTransactions a)
 
    let header = String.Join(",", "From", "To", "Amount", "Block", "CurrencyId", "Details", "Icon", "Index", "Invalid", "Method", "TransactionType", "TransactionVersion", "Hash", "DateTime", "Type")
    
    use writer = new StreamWriter("txMSC.csv", false, System.Text.Encoding.ASCII)
    writer.WriteLine(header)
    Seq.iter (fun (t:tx) -> writer.WriteLine(String.Join(",", t.sender, t.receiver, t.amount, t.block, t.currencyId, t.details, t.icon, t.index, t.invalid, t.method_, t.transactionType, t.transactionVersion, t.txHash, t.txTime, t.txType))) 
                txMSC


    use writer = new StreamWriter("txTMSC.csv", false, System.Text.Encoding.ASCII)
    writer.WriteLine(header)
    Seq.iter (fun (t:tx) -> writer.WriteLine(String.Join(",", t.sender, t.receiver, t.amount, t.block, t.currencyId, t.details, t.icon, t.index, t.invalid, t.method_, t.transactionType, t.transactionVersion, t.txHash, t.txTime, t.txType)))
                txTMSC

    0 // return OK


