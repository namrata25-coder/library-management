namespace CrmLeadManagement.Models;

/// <summary>
/// Handles saving and deleting project files and expense receipts under wwwroot/uploads/work/{folder}/.
/// </summary>
public static class WorkAttachmentService
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    /// <summary>
    /// Saves the uploaded file under wwwroot/uploads/work/{folder}/
    /// Returns (relativePath, originalFileName, fileSizeBytes, fileType, error).
    /// </summary>
    public static async Task<(string Path, string FileName, long Size, string Type, string? Error)> SaveAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0)
        {
            return ("", "", 0, "", null);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return ("", "", 0, "", "File is too large. Maximum allowed size is 25 MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "work", folder);
        Directory.CreateDirectory(uploadsFolder);

        var storedName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsFolder, storedName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/work/{folder}/{storedName}";
        var fileType = ext.TrimStart('.').ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(fileType)) fileType = "FILE";

        return (relativePath, file.FileName, file.Length, fileType, null);
    }

    /// <summary>Deletes the physical file for a relative attachment path, if it exists.</summary>
    public static void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        try 
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
