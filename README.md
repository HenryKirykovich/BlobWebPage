# Class Resource Library (Azure Blob Storage + App Service)

ASP.NET Core MVC web app (.NET 10) that uploads class resources to Azure Blob Storage,
sorts them into two categories (Notes, Examples) and lists them with an All / Notes / Examples filter.
It is a modified version of the Module 5 starter project (BlobWebApp).

## What it does
- Upload form with a file input and a category selector (Notes or Examples).
- Each file is saved as a blob with a category prefix, for example `notes/week5.txt` or `examples/sample.txt`.
- The list is read from Blob Storage on every request, so it still works after a refresh.
- Filter: All, Notes or Examples. A short message is shown when the selected category has no files.
- Download and Delete buttons for each file.
- The blob container is private (no public access). Forms use ASP.NET Core antiforgery tokens.

## Requirements
- Visual Studio 2022 or newer with the ASP.NET workload, .NET 10 SDK
- An Azure Storage account (StorageV2) with a private blob container named `files`
  (the app creates the container, as private, if it does not exist)

## Configuration setting names
| Where | Name | Value |
|---|---|---|
| Local run (User Secrets) | `BlobStorage:ConnectionString` | connection string of your storage account |
| Azure App Service (Settings > Environment variables > App settings) | `BlobStorage__ConnectionString` | same connection string |

The container name `files` is a constant in `Controllers/BlobController.cs`.
No keys or connection strings are stored in the code or in `appsettings.json`
(the `BlobStorage:ConnectionString` value there is intentionally empty).

## Run locally
1. Open `BlobWebPage.slnx` in Visual Studio.
2. Right-click the project > **Manage User Secrets** and add:
   ```json
   {
     "BlobStorage": {
       "ConnectionString": "<your storage account connection string>"
     }
   }
   ```
   (Azure portal > Storage account > Security + networking > Access keys.)
3. Press F5 (https profile). The page opens at `/Blob`.

## Publish to Azure App Service
1. In the Azure portal, open the App Service > Settings > Environment variables > App settings
   and add `BlobStorage__ConnectionString` with the connection string. Save and confirm the restart.
2. In Visual Studio: right-click the project > **Publish** > Azure > Azure App Service, choose the app and click **Publish**.
3. Open the HTTPS URL of the App Service and test the upload, the filters and the empty-category message.

## How to test
1. Open the deployed page. With empty categories, select each filter and check the empty message.
2. Upload one small test file in Notes and one in Examples.
3. In the Azure portal (Storage account > Storage browser > Blob containers > files) check the names `notes/...` and `examples/...`.
4. Refresh the page: All shows both files, Notes shows only its file, Examples shows only its file.
