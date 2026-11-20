using Uludag.Generator.Cli;

namespace Uludag.Generator.Tests
{
    // AC-8 (hatalı girdi) ve komut satırı sözleşmesi.
    public class CliArgumentsTests
    {
        [Fact]
        public void Parse_CrudWithAllOptions_ReturnsArguments()
        {
            CliParseResult result = CliParser.Parse(["crud", "Category", "--force", "--dry-run", "--plural", "Categoryz", "--root", "C:/repo"]);

            Assert.Null(result.Error);
            Assert.Equal(new CliArguments(CliCommand.Crud, "Category", true, true, "Categoryz", "C:/repo"), result.Arguments);
        }

        [Fact]
        public void Parse_List_ReturnsListCommand()
        {
            CliParseResult result = CliParser.Parse(["list"]);

            Assert.Equal(new CliArguments(CliCommand.List), result.Arguments);
        }

        [Theory]
        [InlineData]
        [InlineData("--help")]
        [InlineData("crud", "-h")]
        public void Parse_NoArgsOrHelp_ReturnsHelp(params string[] args)
        {
            Assert.Equal(CliCommand.Help, CliParser.Parse(args).Arguments?.Command);
        }

        [Fact]
        public void Parse_Version_ReturnsVersion()
        {
            Assert.Equal(CliCommand.Version, CliParser.Parse(["--version"]).Arguments?.Command);
        }

        [Theory]
        [InlineData("generate")]                          // bilinmeyen komut
        [InlineData("crud")]                              // entity yok
        [InlineData("crud", "A", "B")]                    // fazla argüman
        [InlineData("crud", "category")]                  // küçük harfle başlayan ad
        [InlineData("crud", "Cat-egory")]                 // geçersiz karakter
        [InlineData("crud", "Category", "--plural")]      // değersiz seçenek
        [InlineData("crud", "Category", "--plural", "--force")]
        [InlineData("crud", "Category", "--plural", "x")] // geçersiz çoğul
        [InlineData("crud", "Category", "--overwrite")]   // bilinmeyen seçenek
        [InlineData("list", "Category")]                  // list argüman almaz
        [InlineData("list", "--force")]                   // list yalnızca --root alır
        public void Parse_InvalidInput_ReturnsError(params string[] args)
        {
            CliParseResult result = CliParser.Parse(args);

            Assert.Null(result.Arguments);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public void Run_InvalidInput_ExitsWithUserErrorAndMessage()
        {
            var output = new StringWriter();
            var error = new StringWriter();

            int exitCode = App.Run(["crud"], Path.GetTempPath(), output, error);

            Assert.Equal(ExitCodes.UserError, exitCode);
            Assert.Contains("entity adı", error.ToString());
            Assert.Equal(string.Empty, output.ToString());
        }

        [Fact]
        public void Run_Version_PrintsVersion()
        {
            var output = new StringWriter();

            int exitCode = App.Run(["--version"], Path.GetTempPath(), output, new StringWriter());

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.StartsWith("uludag-gen 1.0.0", output.ToString());
        }
    }
}
