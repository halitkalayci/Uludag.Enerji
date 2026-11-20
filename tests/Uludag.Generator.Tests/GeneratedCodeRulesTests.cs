using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uludag.Generator.Tests.Infrastructure;
using static Uludag.Generator.Tests.Infrastructure.Generated;

namespace Uludag.Generator.Tests
{
    // AC-5 (F-7: handler'da dallanma ve mapping yok) ve AC-4'ün statik kısmı (yetki matrisi).
    public class GeneratedCodeRulesTests
    {
        public static TheoryData<string, string> Entities => new()
        {
            { "Topic", Fixtures.Topic },
            { "Coupon", Fixtures.Coupon },
            { "PriceAlert", Fixtures.PriceAlert },
        };

        private static readonly string[] RequestFiles =
        [
            "Commands/Create/Create{0}Command.cs",
            "Commands/Update/Update{0}Command.cs",
            "Commands/Delete/Delete{0}Command.cs",
            "Queries/GetAll/GetAll{0}Query.cs",
            "Queries/GetById/GetById{0}Query.cs",
        ];

        [Theory]
        [MemberData(nameof(Entities))]
        public void Handlers_HaveNoBranchingOrMapping(string entity, string source)
        {
            using var repo = TestRepo.Create();
            repo.AddEntity(entity, source);
            Assert.Equal(ExitCodes.Success, Run(repo, "crud", entity).ExitCode);

            foreach (string file in RequestFiles)
            {
                string path = FeatureFile(repo, entity, string.Format(file, entity));
                var handlers = Parse(path).DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text.EndsWith("Handler", StringComparison.Ordinal))
                    .ToList();
                Assert.Single(handlers);

                var forbidden = handlers[0].DescendantNodes().Where(IsBranchingOrMapping).Select(n => n.ToString()).ToList();
                Assert.True(forbidden.Count == 0, $"{Path.GetFileName(path)}: {string.Join(" | ", forbidden)}");
            }
        }

        // Dallanma: if, switch (deyim/ifade), ?:, ??, ?. · Mapping: nesne oluşturma ve üye ataması (x.Prop = ...).
        private static bool IsBranchingOrMapping(SyntaxNode node) => node switch
        {
            IfStatementSyntax or SwitchStatementSyntax or SwitchExpressionSyntax or ConditionalExpressionSyntax => true,
            ConditionalAccessExpressionSyntax => true,
            BinaryExpressionSyntax b when b.IsKind(SyntaxKind.CoalesceExpression) => true,
            BaseObjectCreationExpressionSyntax => true,
            AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax m } when m.Expression is not ThisExpressionSyntax => true,
            _ => false
        };

        [Theory]
        [MemberData(nameof(Entities))]
        public void Requests_AllSecured_WithRoleMatrix(string entity, string source)
        {
            using var repo = TestRepo.Create();
            repo.AddEntity(entity, source);
            Assert.Equal(ExitCodes.Success, Run(repo, "crud", entity).ExitCode);

            string[] write(string action) => [$"{entity}OperationClaims.Admin", $"{entity}OperationClaims.Write", $"{entity}OperationClaims.{action}"];
            string[] read = [$"{entity}OperationClaims.Admin", $"{entity}OperationClaims.Read"];
            var expected = new Dictionary<string, string[]>
            {
                [$"Create{entity}Command"] = write("Create"),
                [$"Update{entity}Command"] = write("Update"),
                [$"Delete{entity}Command"] = write("Delete"),
                [$"GetAll{entity}Query"] = read,
                [$"GetById{entity}Query"] = read,
            };

            foreach (string file in RequestFiles)
            {
                string path = FeatureFile(repo, entity, string.Format(file, entity));
                string className = Path.GetFileNameWithoutExtension(path);
                ClassDeclarationSyntax request = Class(path, className);

                Assert.Contains(request.BaseList!.Types, t => t.Type.ToString() == "ISecuredRequest");
                Assert.DoesNotContain(request.BaseList!.Types, t => t.Type.ToString() == "IAllowAnonymousRequest");
                PropertyDeclarationSyntax roles = request.Members.OfType<PropertyDeclarationSyntax>()
                    .Single(p => p.ExplicitInterfaceSpecifier?.Name.ToString() == "ISecuredRequest" && p.Identifier.Text == "Roles");
                var collection = (CollectionExpressionSyntax)roles.ExpressionBody!.Expression;
                Assert.Equal(expected[className], collection.Elements.Select(e => e.ToString()));
            }

            string claims = File.ReadAllText(FeatureFile(repo, entity, $"Constants/{entity}OperationClaims.cs"));
            foreach (string action in new[] { "Admin", "Write", "Read", "Create", "Update", "Delete" })
                Assert.Contains($"public const string {action} = \"{entity}.{action}\";", claims);
        }
    }
}
