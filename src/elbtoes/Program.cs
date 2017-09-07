using System;
using CommandLine;

namespace elbtoes
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ProgramOptions>(args)
                .WithNotParsed(error =>
                {
                    Console.Error.WriteLine("Invalid args.");
                    Environment.Exit(1);
                })
                .WithParsed(options =>
                {
                    new ExportPipeline(
                        options,
                        Console.Out,
                        Console.Error
                    ).RunAsync().Wait();
                });
        }
    }
}
