namespace ECommerce.Application.Features.Category.Constants
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Kullanıcıya dönen mesajların anahtarları; metinler Locales/CategoryLocale.{tr,en}.yaml içinde (docs/conventions.md).
    public static class CategoryConstants
    {
        public const string CategoryNotFound = "CategoryNotFound";
        public const string CategoryNameCannotBeEmpty = "CategoryNameCannotBeEmpty";
        public const string CategoryNameTooLong = "CategoryNameTooLong";
        public const string CategoryNameAlreadyExists = "CategoryNameAlreadyExists";
        public const string CategoryHasProducts = "CategoryHasProducts";
    }
}
