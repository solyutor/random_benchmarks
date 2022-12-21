using System.Globalization;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace StringComparisonBenchmarks;

[MemoryDiagnoser]
public class StringInterpolationBenchmark
{
    private const int CachedSize = 10;
    private readonly string[] _cache;

    public StringInterpolationBenchmark()
    {
        _cache = new string[CachedSize];
        for (int i = 0; i < CachedSize; i++)
        {
            _cache[i] = i.ToString();
        }
    }
    
    [Params(7, 1568, 1_940_645, 1_423_631_293)]
    public int Value { get; set; }

    [Benchmark(Baseline = true)]
    public string Default() => $"/strategies/{Value}";
    
    [Benchmark]
    public string InvariantCulture() => FormattableString.Invariant($"/strategies/{Value}");
    
    [Benchmark]
    public string CustomFormat() => $"/strategies/{Value:D}";
    
    [Benchmark]
    public string ValueToString() => $"/strategies/{Value.ToString()}";
    
    [Benchmark]
    public string ValueToStringInvariant() => $"/strategies/{Value.ToString(CultureInfo.InvariantCulture)}";
    
    [Benchmark]
    // ReSharper disable once SimplifyStringInterpolation
    public string ValueToStringFormat() => $"/strategies/{Value.ToString("D")}";
    
    [Benchmark]
    public string AsString() => $"/strategies/{AsString(Value)}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string AsString(int value)
    {
        return value < CachedSize ? _cache[value] : value.ToString(CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public string HardCoreFormatter()
    {
        //https://devblogs.microsoft.com/dotnet/string-interpolation-in-c-10-and-net-6/
        return FormatCrazy(Value, null);
    }    [Benchmark]
    public string HardCoreFormatterInvariantCulture()
    {
        //https://devblogs.microsoft.com/dotnet/string-interpolation-in-c-10-and-net-6/
        return FormatCrazy(Value, NumberFormatInfo.InvariantInfo);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string FormatCrazy(int value, NumberFormatInfo? numberFormatInfo) 
        => String.Create(numberFormatInfo, stackalloc char[32], $"/strategies/{value}");
}