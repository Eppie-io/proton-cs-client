using Tuvi.Toolkit.Cli;

namespace Tuvi.Proton.Client.Sample
{
    internal static class Program
    {
        private static string Hello { get; } = "Hello, Proton!";

        private static void Main()
        {
            ConsoleExtension.WriteLine(Hello, ConsoleColor.Green);
        }
    }
}
