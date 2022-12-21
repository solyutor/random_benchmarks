using BenchmarkDotNet.Attributes;

namespace StringComparisonBenchmarks;


public class IndexOfBenchmark
{
    private readonly string _target;
    private readonly string _goal;

    public IndexOfBenchmark()
    {
        _target = "/api/v1/blogs/posts";
        _goal = "/blogs";
    }

    [Benchmark(Baseline = true)]
    public int DefaultSearch()
    {
        return _target.IndexOf(_goal);
    }

    [Benchmark]
    public int Ordinal()
    {
        return _target.IndexOf(_goal, StringComparison.Ordinal);
    }

    [Benchmark]
    public int OrdinalIgnoreCase()
    {
        return _target.IndexOf(_goal, StringComparison.OrdinalIgnoreCase);
    }

    [Benchmark]
    public int InvariantCulture()
    {
        return _target.IndexOf(_goal, StringComparison.InvariantCulture);
    }

    [Benchmark]
    public int InvariantCultureIgnoreCase()
    {
        return _target.IndexOf(_goal, StringComparison.InvariantCultureIgnoreCase);
    }

    [Benchmark]
    public int SpanOrdinalIgnoreCase()
    {
        var span = _target.AsSpan();
        return span.IndexOf(_goal.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}
