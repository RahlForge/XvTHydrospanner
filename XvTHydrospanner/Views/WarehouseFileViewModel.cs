using XvTHydrospanner.Models;

namespace XvTHydrospanner.Views
{
    /// <summary>
    /// View model for displaying warehouse files with additional display properties
    /// </summary>
    public class WarehouseFileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public ModCategory Category { get; set; }
        public string TargetRelativePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string? ModPackageId { get; set; }
        public string ModPackageName { get; set; } = string.Empty;
        
        public static WarehouseFileViewModel FromWarehouseFile(WarehouseFile file, string? modPackageName)
        {
            return new WarehouseFileViewModel
            {
                Id = file.Id,
                Name = file.Name,
                Description = file.Description,
                StoragePath = file.StoragePath,
                OriginalFileName = file.OriginalFileName,
                FileExtension = file.FileExtension,
                Category = file.Category,
                TargetRelativePath = file.TargetRelativePath,
                FileSizeBytes = file.FileSizeBytes,
                Author = file.Author,
                Version = file.Version,
                ModPackageId = file.ModPackageId,
                ModPackageName = modPackageName ?? "(Standalone)"
            };
        }
    }
}
