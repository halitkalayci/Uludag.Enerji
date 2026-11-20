using System.Reflection;
using Uludag.Generator.Cli;

namespace Uludag.Generator
{
    internal static class ExitCodes
    {
        public const int Success = 0;
        public const int UserError = 1;
        public const int Conflict = 2;
    }

    // Komutları çalıştırır. Konsol yerine TextWriter alır: testler aynı yolu çıktıyı yakalayarak koşar.
    internal static class App
    {
        public static int Run(string[] args, string workingDirectory, TextWriter output, TextWriter error)
        {
            CliParseResult parsed = CliParser.Parse(args);
            if (parsed.Arguments is null)
            {
                error.WriteLine($"Hata: {parsed.Error}");
                error.WriteLine("Yardım için: uludag-gen --help");
                return ExitCodes.UserError;
            }

            CliArguments arguments = parsed.Arguments;
            switch (arguments.Command)
            {
                case CliCommand.Help:
                    output.WriteLine(CliParser.Usage);
                    return ExitCodes.Success;
                case CliCommand.Version:
                    output.WriteLine($"uludag-gen {Version}");
                    return ExitCodes.Success;
                default:
                    error.WriteLine($"Hata: '{arguments.Command}' henüz desteklenmiyor.");
                    return ExitCodes.UserError;
            }
        }

        public static string Version =>
            typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? typeof(App).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
