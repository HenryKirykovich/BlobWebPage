using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobWebPage.Models;
using Microsoft.AspNetCore.Mvc;

namespace BlobWebPage.Controllers
{
    public class BlobController : Controller
    {
        // Client for the Azure Storage account.
        // It is created in Program.cs and passed in through the constructor.
        private readonly BlobServiceClient blobServiceClient;

        // Container name. The container stays PRIVATE (we never set public access).
        private const string ContainerName = "files";

        // The two allowed categories. Each one is used as a blob name prefix:
        // "notes/week5.txt" and "examples/sample.txt"
        private static readonly string[] Categories = { "notes", "examples" };

        // Constructor: receives the ready-made client and saves it
        public BlobController(BlobServiceClient blobServiceClient)
        {
            this.blobServiceClient = blobServiceClient;
        }

        // Small helper: gets the container client
        private BlobContainerClient GetContainer()
        {
            return blobServiceClient.GetBlobContainerClient(ContainerName);
        }

        // Small helper: only allow blob names that start with "notes/" or "examples/"
        private static bool IsAllowedBlobName(string? name)
        {
            return !string.IsNullOrEmpty(name)
                && !name.Contains("..")
                && Categories.Any(c => name.StartsWith(c + "/", StringComparison.Ordinal));
        }

        // ---------------------------------------------------------
        // Index: shows the upload form and the list of files.
        // The list is read from Blob Storage on every request,
        // so it still works after the page is refreshed.
        // URL examples: /Blob  /Blob?filter=notes  /Blob?filter=examples
        // ---------------------------------------------------------
        public async Task<IActionResult> Index(string? filter = "all")
        {
            // Accept only known filter values
            filter = (filter ?? "all").ToLowerInvariant();
            if (filter != "notes" && filter != "examples")
            {
                filter = "all";
            }

            var container = GetContainer();

            // Create the container if it does not exist yet (private by default)
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            // "all" = both categories, otherwise only the selected one
            var categoriesToList = filter == "all" ? Categories : new[] { filter };

            var names = new List<string>();
            foreach (var category in categoriesToList)
            {
                // Ask Blob Storage only for blobs whose name starts with "<category>/"
                await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, category + "/", CancellationToken.None))
                {
                    names.Add(blob.Name);
                }
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);

            return View(new ResourceListViewModel { Filter = filter, Files = names });
        }

        // ---------------------------------------------------------
        // UploadFile: uploads a file into the selected category.
        // The antiforgery token is added to the form automatically
        // by the asp-action tag helper and checked here.
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile? file, string? category)
        {
            // Check that the category is one of the allowed values
            category = (category ?? "").ToLowerInvariant();
            if (!Categories.Contains(category))
            {
                TempData["Message"] = "Please choose a category (Notes or Examples).";
                return RedirectToAction(nameof(Index));
            }

            // Check that a file was selected
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please choose a file first.";
                return RedirectToAction(nameof(Index));
            }

            var container = GetContainer();
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            // Blob name = category prefix + file name, for example "notes/week5.txt".
            // Path.GetFileName removes any folder part from the uploaded name.
            var blobName = category + "/" + Path.GetFileName(file.FileName);
            var blobClient = container.GetBlobClient(blobName);

            // Upload the file content.
            // ContentType tells the browser what kind of file it is (image, pdf, ...).
            // If a blob with the same name exists, it is replaced.
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });
            }

            TempData["Message"] = "Uploaded: " + blobName;
            return RedirectToAction(nameof(Index), new { filter = "all" });
        }

        // ---------------------------------------------------------
        // DownloadFile: downloads a file from Azure
        // ---------------------------------------------------------
        public async Task<IActionResult> DownloadFile(string? fileName)
        {
            if (!IsAllowedBlobName(fileName))
            {
                return BadRequest("Invalid file name.");
            }

            var blobClient = GetContainer().GetBlobClient(fileName!);

            // Check that the file exists
            if (!await blobClient.ExistsAsync())
            {
                return NotFound($"File {fileName} not found!");
            }

            var response = await blobClient.DownloadAsync();

            // Return the file to the browser (content, type, name without the prefix)
            return File(response.Value.Content, response.Value.ContentType, Path.GetFileName(fileName!));
        }

        // ---------------------------------------------------------
        // DeleteFile: deletes a file from Azure
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(string? fileName, string? filter)
        {
            if (!IsAllowedBlobName(fileName))
            {
                return BadRequest("Invalid file name.");
            }

            var blobClient = GetContainer().GetBlobClient(fileName!);

            // Delete the blob if it exists
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteAsync();
            }

            TempData["Message"] = "Deleted: " + fileName;
            return RedirectToAction(nameof(Index), new { filter });
        }
    }
}
