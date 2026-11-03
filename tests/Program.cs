using System;
using System.Diagnostics;
using System.Globalization;
using ExactFertilityGrowingZones;

internal static class Program
{
    private static int checks;
    private static string benchmarkSink;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Main()
    {
        Check(FertilityValues.Matches(1.4f, 1.4f), "Rich soil must match 140%.");
        Check(!FertilityValues.Matches(0.7f, 1f), "Gravel must not match 100%.");
        Check(!FertilityValues.Matches(1.4f, 1f), "Rich soil must not match 100%.");
        Check(FertilityValues.Matches(1.400001f, 1.4f), "Representation noise must be accepted.");
        Check(!FertilityValues.Matches(1.40002f, 1.4f), "Different custom fertility must be rejected.");
        Check(FertilityValues.Matches(0f, FertilityValues.Tolerance), "Tolerance boundary is inclusive.");
        Check(!FertilityValues.Matches(float.NaN, 1f), "Invalid fertility must not match.");

        var choices = FertilityValues.SortedDistinctPositive(new[]
        {
            1.4f, 0f, 1f, 0.7f, 1f, 1.400001f, -1f, float.NaN, float.PositiveInfinity, 1.125f
        });
        Check(choices.Count == 4, "Choices must remove zero, negatives, invalid values, duplicates and float noise.");
        Check(choices[0] == 0.7f && choices[1] == 1f && choices[2] == 1.125f && choices[3] == 1.4f,
            "Custom fertility must be preserved in ascending order.");
        Check(FertilityValues.SortedDistinctPositive(Array.Empty<float>()).Count == 0,
            "A map without positive revealed fertility must have no choices.");

        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
        Check(FertilityValues.Label(1.4f) == "140%", "Normal fertility must have a readable label.");
        Check(FertilityValues.Label(1.125f) == "112.5%", "Fractional fertility must not round to whole percent.");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Check(FertilityValues.Label(1.125f) == "112,5%", "Labels must respect the current culture.");
        TestMessageCache();
        BenchmarkMessageCache();
        Console.WriteLine("PASS: " + checks + " fertility/cache checks.");
    }

    private static void TestMessageCache()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
        int builds = 0;
        var cache = new FertilityMessageCache(target =>
        {
            builds++;
            return "Fertility: " + FertilityValues.Label(target);
        });
        var language = new object();
        string first = cache.Get(1.4f, language);
        Check(first == "Fertility: 140%" && builds == 1, "First request must generate the selected target's message.");
        for (int i = 0; i < 65536; i++)
            CheckReference(cache.Get(1.4f, language), first);
        Check(builds == 1, "A large selection must translate and format once, not once per cell.");

        Check(cache.Get(0.7f, language) == "Fertility: 70%" && builds == 2,
            "Changing target must invalidate the message immediately.");
        var otherLanguage = new object();
        cache.Get(0.7f, otherLanguage);
        Check(builds == 3, "Changing game language must regenerate the message.");

        cache.Get(1.125f, otherLanguage);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Check(cache.Get(1.125f, otherLanguage) == "Fertility: 112,5%" && builds == 5,
            "Changing number culture must regenerate the message.");
        Check(cache.Get(1.4f, null) == "Fertility: 140%", "An unset language must be a valid cache key.");

        int attempts = 0;
        var retryCache = new FertilityMessageCache(target =>
        {
            if (++attempts == 1)
                throw new InvalidOperationException("Simulated translation failure");
            return "recovered";
        });
        try { retryCache.Get(1f, language); }
        catch (InvalidOperationException) { }
        Check(retryCache.Get(1f, language) == "recovered" && attempts == 2,
            "A failed translation must not commit a stale or missing message.");
    }

    private static void CheckReference(string actual, string expected)
    {
        if (!ReferenceEquals(actual, expected))
            throw new InvalidOperationException("Repeated cells must reuse the same message string.");
    }

    private static string FormatBenchmarkMessage(float target)
    {
        // Formatting-only lower bound for the old path; does not include RimWorld translation work.
        return string.Format(CultureInfo.CurrentCulture, "Requires fertility {0}.", FertilityValues.Label(target));
    }

    private static void BenchmarkMessageCache()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
        const int iterations = 250000;
        var language = new object();
        var cache = new FertilityMessageCache(FormatBenchmarkMessage);
        for (int i = 0; i < 10000; i++)
        {
            benchmarkSink = FormatBenchmarkMessage(1.4f);
            benchmarkSink = cache.Get(1.4f, language);
        }

        var watch = new Stopwatch();
        long before = GC.GetAllocatedBytesForCurrentThread();
        watch.Start();
        for (int i = 0; i < iterations; i++)
            benchmarkSink = FormatBenchmarkMessage(1.4f);
        watch.Stop();
        long oldBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        double oldMs = watch.Elapsed.TotalMilliseconds;

        watch.Reset();
        before = GC.GetAllocatedBytesForCurrentThread();
        watch.Start();
        for (int i = 0; i < iterations; i++)
            benchmarkSink = cache.Get(1.4f, language);
        watch.Stop();
        long newBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(newBytes == 0, "Repeated cached rejection text must allocate zero bytes after warmup.");
        Check(oldBytes > 0, "The benchmark baseline must exercise actual per-cell string allocation.");
        Console.WriteLine("Formatting-only benchmark (not game FPS), " + iterations + " rejected-cell messages:");
        Console.WriteLine("  old: " + oldMs.ToString("F2") + " ms, " + oldBytes + " allocated bytes");
        Console.WriteLine("  new: " + watch.Elapsed.TotalMilliseconds.ToString("F2") + " ms, " + newBytes + " allocated bytes");
        GC.KeepAlive(benchmarkSink);
    }
}
