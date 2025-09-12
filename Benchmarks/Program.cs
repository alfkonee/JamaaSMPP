using BenchmarkDotNet.Running;

namespace JamaaTech.Smpp.Net.Lib.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ResponseHandlerBenchmarks>();
    }
}