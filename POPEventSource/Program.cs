using POPEventSource;

/// <summary>
/// Just deisdet to show this concept for event sourcing
/// </summary>
var whiteCardAccount =await WhiteCardAccount.OpenAsync("User1", 1000);

await whiteCardAccount.DepositAsync(500, "deposit");
await whiteCardAccount.WithdrawAsync(200, "withdrawal");
await whiteCardAccount.TransferToAsync(Guid.NewGuid(), 300, "Transfer to othe acc");
await whiteCardAccount.CloseAsync("Account closed by user");


Console.WriteLine($"Last balance: {whiteCardAccount.Balance}");

foreach (var @event in whiteCardAccount.Events)
{
    Console.WriteLine($"Event: {@event.GetType().Name} at {@event.Timestamp}");
}

foreach (var item in whiteCardAccount.Events.AsEnumerable().Reverse())
{
    // you can read data the way you want (for example the events that ocured with specific AccrualID)
}
for (int i = whiteCardAccount.Events.Count() - 1; i >= 0; i--)
{
    Console.WriteLine(whiteCardAccount.Events[i]);
    Console.WriteLine("Doing any recalcutarions mb !?");
}

