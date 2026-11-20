using ECommerce.Application.Features.Category.Commands.Create;
using ECommerce.Application.Features.Category.Commands.Update;
using ECommerce.Application.Features.Category.Queries;

namespace ECommerce.Application.Features.Category.Mappers
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Category feature'ının tüm mapping'leri burada yazılır; handler'lar mapping yapmaz, bu metotları çağırır (F-7).
    public static class CategoryMapper
    {
        public static Domain.Entities.Category MapCreateCommandToCategory(CreateCategoryCommand command)
        {
            // Baştaki/sondaki boşluklar kırpılarak saklanır (spec 0009, R-2).
            return new Domain.Entities.Category
            {
                Name = command.Name.Trim()
            };
        }

        // Var olan (DB'den gelen, EF tarafından takip edilen) entity'nin üzerine yazar; yeni instance oluşturmaz.
        public static void MapUpdateCommandToCategory(UpdateCategoryCommand command, Domain.Entities.Category category)
        {
            category.Name = command.Name.Trim();
        }

        public static CategoryResponse MapCategoryToResponse(Domain.Entities.Category category)
        {
            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        public static List<CategoryResponse> MapCategoriesToResponses(List<Domain.Entities.Category> categories)
        {
            return categories.Select(MapCategoryToResponse).ToList();
        }
    }
}
