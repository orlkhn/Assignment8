using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var transactions = new List<Transaction>
{
    new("Card", 200m, 4m),     new("Transfer", 500m, 5m), new("Card", 50m, 1m),
    new("Card", 1200m, 24m),   new("Transfer", 150m, 5m), new("Wallet", 80m, 0.8m),
};

// ---------- Task 1: fee report with LINQ ----------
Console.WriteLine("=== Task 1: LINQ report ===");

var feeByChannel = transactions
    .GroupBy(t => t.Channel)
    .Select(g => new { Channel = g.Key, TotalFee = g.Sum(t => t.Fee) });

foreach (var row in feeByChannel)
    Console.WriteLine($"{row.Channel}: {row.TotalFee:F2}");

var top3 = transactions.OrderByDescending(t => t.Amount).Take(3);
Console.WriteLine("Top 3 largest transactions:");
foreach (var t in top3)
    Console.WriteLine($"  {t.Channel} {t.Amount:F2}");

var cardAverage = transactions
    .Where(t => t.Channel == "Card")
    .Average(t => t.Amount);
Console.WriteLine($"Average Card amount: {cardAverage:F2}");

// ---------- Task 2: empty sequence ----------
Console.WriteLine();
Console.WriteLine("=== Task 2: Crypto average ===");

try
{
    var cryptoAverage = transactions
        .Where(t => t.Channel == "Crypto")
        .Average(t => t.Amount);          // throws on an empty sequence
    Console.WriteLine($"Average Crypto amount: {cryptoAverage:F2}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Original code throws: {ex.GetType().Name}: {ex.Message}");
}

// Fix: Average over decimal? returns null for an empty sequence (no exception),
// and the report prints "no data" instead of a misleading 0.00.
decimal? safeAverage = transactions
    .Where(t => t.Channel == "Crypto")
    .Select(t => (decimal?)t.Amount)
    .Average();
Console.WriteLine($"Average Crypto amount: {(safeAverage.HasValue ? safeAverage.Value.ToString("F2") : "no data")}");

// ---------- Task 3: own lazy operators ----------
Console.WriteLine();
Console.WriteLine("=== Task 3: Filter / Transform ===");

var feesOver100 = transactions
    .Filter(t => t.Amount > 100m)
    .Transform(t => t.Fee);
Console.WriteLine("Fees of transactions over 100: " + string.Join(", ", feesOver100.Select(f => f.ToString("F2"))));

// ---------- Task 4: domain extension methods ----------
Console.WriteLine();
Console.WriteLine("=== Task 4: ForChannel / TotalFees ===");

Console.WriteLine($"Total fees of Card transactions: {transactions.ForChannel("Card").TotalFees():F2}");

// ---------- Task 5: deferred execution ----------
Console.WriteLine();
Console.WriteLine("=== Task 5: deferred execution probe ===");

int calls = 0;
var big = transactions.Filter(t => { calls++; return t.Amount > 100m; });
Console.WriteLine($"After building big: calls = {calls}");

int count = big.Count();
Console.WriteLine($"Print 1: Count = {count}, calls = {calls}");

transactions.Add(new("Card", 300m, 6m));
Console.WriteLine("(added Card 300.00 to the list)");

count = big.Count();
Console.WriteLine($"Print 2: Count = {count}, calls = {calls}");

count = big.Count();
Console.WriteLine($"Print 3: Count = {count}, calls = {calls}");

var snapshot = big.ToList();
Console.WriteLine($"After ToList(): snapshot.Count = {snapshot.Count}, calls = {calls}");
Console.WriteLine($"Read snapshot twice: {snapshot.Count} and {snapshot.Count}, calls = {calls}");

transactions.Add(new("Transfer", 999m, 9m));
Console.WriteLine($"After one more Add: snapshot.Count = {snapshot.Count} (unchanged), big.Count() = {big.Count()}");

public record Transaction(string Channel, decimal Amount, decimal Fee);

public static class EnumerableExtensions
{
    // Lazy filter: nothing runs until the caller enumerates the result.
    public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
            if (predicate(item))
                yield return item;
    }

    // Lazy projection.
    public static IEnumerable<R> Transform<T, R>(this IEnumerable<T> source, Func<T, R> selector)
    {
        foreach (var item in source)
            yield return selector(item);
    }
}

public static class TransactionExtensions
{
    public static IEnumerable<Transaction> ForChannel(this IEnumerable<Transaction> source, string channel)
        => source.Filter(t => t.Channel == channel);

    public static decimal TotalFees(this IEnumerable<Transaction> source)
        => source.Sum(t => t.Fee);
}
