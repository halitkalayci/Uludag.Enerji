using System.Reflection;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using ECommerce.WebAPI.Controllers;
using NetArchTest.Rules;
using ArchTestResult = NetArchTest.Rules.TestResult;
using Uludag.Core.Mediation;

namespace ECommerce.ArchitectureTests
{
    // docs/architecture.md — F-1…F-5 yasak bağımlılıklar.
    // Her test önce değerlendirdiği tip kümesinin boş olmadığını doğrular: yanlış bir derleme /
    // isim alanı filtresi kuralı "boşuna" yeşil yapamaz (spec 0001, AC-6).
    public class LayerDependencyTests
    {
        private static readonly Assembly domainAssembly = typeof(Product).Assembly;
        private static readonly Assembly applicationAssembly = typeof(ProductBusinessRules).Assembly;
        private static readonly Assembly persistenceAssembly = typeof(UludagDbContext).Assembly;
        private static readonly Assembly webApiAssembly = typeof(ProductsController).Assembly;
        private static readonly Assembly coreAssembly = typeof(IMediator).Assembly;

        private const string ControllersNamespace = "ECommerce.WebAPI.Controllers";

        // F-1: Domain yalnızca .NET temel kütüphanelerine referans verir.
        // "Temel kütüphane" = çalışan runtime'ın paylaşılan framework derlemeleri (TRUSTED_PLATFORM_ASSEMBLIES).
        // Build-time analizörler derlenmiş referanslarda görünmez, bu yüzden sayılmaz.
        [Fact]
        public void DomainLayer_Dependencies_ReferenceOnlyBaseLibraries()
        {
            AssertNotEmpty(AllTypes(domainAssembly).GetTypes(), "ECommerce.Domain tipleri");

            HashSet<string> platformAssemblyNames = GetPlatformAssemblyNames();
            AssemblyName[] referencedAssemblies = domainAssembly.GetReferencedAssemblies();
            Assert.NotEmpty(referencedAssemblies);

            string[] nonPlatformReferences = referencedAssemblies
                .Select(reference => reference.Name!)
                .Where(name => !platformAssemblyNames.Contains(name))
                .ToArray();

            Assert.True(nonPlatformReferences.Length == 0,
                $"F-1 ihlali — ECommerce.Domain temel kütüphane dışı derlemelere referans veriyor: {string.Join(", ", nonPlatformReferences)}");
        }

        // F-2: Application yalnızca Domain ve Uludag.Core'a bağlıdır.
        [Fact]
        public void ApplicationLayer_Dependencies_DoNotReferenceOuterLayersOrEfCore()
        {
            AssertRule(
                AllTypes(applicationAssembly),
                "F-2", "ECommerce.Application tipleri",
                "ECommerce.Persistence", "ECommerce.Infrastructure", "ECommerce.WebAPI", "Microsoft.EntityFrameworkCore");
        }

        // F-3: Uludag.Core hiçbir ECommerce tipine referans vermez.
        [Fact]
        public void UludagCore_Dependencies_DoNotReferenceECommerce()
        {
            AssertRule(
                AllTypes(coreAssembly),
                "F-3", "Uludag.Core tipleri",
                "ECommerce");
        }

        // F-4: Persistence, WebAPI'ye referans vermez.
        [Fact]
        public void PersistenceLayer_Dependencies_DoNotReferenceWebApi()
        {
            AssertRule(
                AllTypes(persistenceAssembly),
                "F-4", "ECommerce.Persistence tipleri",
                "ECommerce.WebAPI");
        }

        // F-5: Controller'lar yalnızca IMediator kullanır; repository / servis arayüzleri, Persistence ve EF Core'a bağlanmaz.
        [Fact]
        public void Controllers_Dependencies_DoNotReferenceServicesPersistenceOrEfCore()
        {
            AssertRule(
                Types.InAssembly(webApiAssembly).That().ResideInNamespace(ControllersNamespace),
                "F-5", "WebAPI controller'ları",
                "ECommerce.Application.Services", "ECommerce.Persistence", "Microsoft.EntityFrameworkCore");
        }

        private static void AssertRule(PredicateList types, string ruleId, string subject, params string[] forbiddenNamespaces)
        {
            AssertNotEmpty(types.GetTypes(), subject);

            ArchTestResult result = types.Should().NotHaveDependencyOnAny(forbiddenNamespaces).GetResult();

            Assert.True(result.IsSuccessful,
                $"{ruleId} ihlali — {subject} yasak isim alanlarına ({string.Join(", ", forbiddenNamespaces)}) bağımlı: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }

        // Derlemedeki tüm tipler (public + internal), NetArchTest'in PredicateList'i olarak.
        private static PredicateList AllTypes(Assembly assembly) =>
            Types.InAssembly(assembly).That().ArePublic().Or().AreNotPublic();

        private static void AssertNotEmpty(IEnumerable<Type> types, string subject)
        {
            Assert.True(types.Any(), $"{subject} için değerlendirilecek tip bulunamadı — kural boş küme üzerinde geçemez.");
        }

        private static HashSet<string> GetPlatformAssemblyNames()
        {
            string trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
                ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES okunamadı.");

            // Yalnızca paylaşılan framework (Microsoft.NETCore.App) derlemeleri: uygulamanın kendi bin klasöründeki
            // derlemeler (çözüm projeleri, NuGet paketleri) de bu listede yer aldığından dizine göre süzülür.
            string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            return trustedPlatformAssemblies
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(path => string.Equals(Path.GetDirectoryName(path), runtimeDirectory, StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension)
                .ToHashSet(StringComparer.Ordinal)!;
        }
    }
}
