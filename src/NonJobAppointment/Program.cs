// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<BoxingBenchmark>();

[MemoryDiagnoser]
public class BoxingBenchmark
{
    private readonly Guid[] values;
    private static readonly EqualityComparer<Guid> comparer = EqualityComparer<Guid>.Default;

    public BoxingBenchmark()
    {
        this.values = Enumerable.Range(1, 100_000).Select(_ => Guid.NewGuid()).ToArray();
    }

    [Benchmark]
    public int UsingGetHashCode()
    {
        int hash = 0;

        for (int i = 0; i < 100_000; i++)
        {
            ref Guid value = ref this.values[i];
            hash ^= value.GetHashCode();
        }

        return hash;
    }

    [Benchmark]
    public int UsingEqualityComparer()
    {
         int hash = 0;

        for (int i = 0; i < 100_000; i++)
        {
            ref Guid value = ref this.values[i];
            hash ^= comparer.GetHashCode(value);
        }

        return hash;
    }
}