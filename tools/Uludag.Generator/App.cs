using System.Reflection;
using Uludag.Generator.Analysis;
using Uludag.Generator.Cli;
using Uludag.Generator.Generation;
using Uludag.Generator.Workspace;

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
            }

            RepoLayout? layout = RepoLocator.Locate(workingDirectory, arguments.Root);
            if (layout is null)
            {
                error.WriteLine(arguments.Root is null
                    ? $"Hata: repo kökü bulunamadı. '{workingDirectory}' ve üst dizinlerinde '{RepoLayout.MarkerRelativePath}' yok. Repo içinden çalıştırın ya da --root verin."
                    : $"Hata: '{arguments.Root}' repo kökü değil ('{RepoLayout.MarkerRelativePath}' yok).");
                return ExitCodes.UserError;
            }

            DomainModel domain = EntityReader.Read(layout);
            return arguments.Command switch
            {
                CliCommand.List => List(domain, output),
                _ => Crud(arguments, layout, domain, output, error)
            };
        }

        private static int List(DomainModel domain, TextWriter output)
        {
            foreach (EntityModel entity in domain.Entities)
                output.WriteLine(entity.Name);
            return ExitCodes.Success;
        }

        private static int Crud(CliArguments arguments, RepoLayout layout, DomainModel domain, TextWriter output, TextWriter error)
        {
            PlanResult result = CrudGenerator.Plan(layout, domain, arguments.Entity!, arguments.Plural);
            if (result.Plan is null)
            {
                error.WriteLine($"Hata: {result.Error}");
                return ExitCodes.UserError;
            }

            GenerationPlan plan = result.Plan;
            if (plan.Conflicts.Count > 0 && !arguments.Force)
            {
                ReportWriter.WriteConflicts(plan, layout, error);
                return ExitCodes.Conflict;
            }

            if (!arguments.DryRun)
                CrudGenerator.Apply(plan);

            ReportWriter.Write(plan, layout, arguments.DryRun, output);
            return ExitCodes.Success;
        }

        public static string Version =>
            typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? typeof(App).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
