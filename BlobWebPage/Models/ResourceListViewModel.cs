namespace BlobWebPage.Models
{
    // Data that the Index page needs: which filter is selected
    // and the list of file names to show.
    public class ResourceListViewModel
    {
        // "all", "notes" or "examples"
        public string Filter { get; set; } = "all";

        // Full blob names, for example "notes/week5.txt"
        public List<string> Files { get; set; } = new();
    }
}
