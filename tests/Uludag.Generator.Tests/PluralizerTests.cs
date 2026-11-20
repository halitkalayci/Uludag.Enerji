using Uludag.Generator.Naming;

namespace Uludag.Generator.Tests
{
    // Spec R-2 / G-2: basit İngilizce çoğul kuralları.
    public class PluralizerTests
    {
        [Theory]
        [InlineData("Product", "Products")]
        [InlineData("Order", "Orders")]
        [InlineData("Category", "Categories")]
        [InlineData("Key", "Keys")]         // ünlü + y
        [InlineData("Address", "Addresses")]
        [InlineData("Box", "Boxes")]
        [InlineData("Branch", "Branches")]
        [InlineData("Wish", "Wishes")]
        [InlineData("Quiz", "Quizes")]       // düzensiz çoğul: --plural Quizzes ile verilir
        public void Pluralize_Singular_ReturnsPlural(string singular, string expected)
        {
            Assert.Equal(expected, Pluralizer.Pluralize(singular));
        }
    }
}
