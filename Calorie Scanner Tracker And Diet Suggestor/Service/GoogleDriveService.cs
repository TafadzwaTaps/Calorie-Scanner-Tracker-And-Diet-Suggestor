using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class GoogleDriveService
{
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
    private readonly DriveService _driveService;

    public GoogleDriveService()
    {
        string credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "credentials.json");

        using var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read);
        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            Scopes,
            "user",
            CancellationToken.None,
            new FileDataStore("Drive.Auth.Store")
        ).Result;

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Calorie Tracker"
        });
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = new[] { "YOUR_GOOGLE_DRIVE_FOLDER_ID" } // Change to your Drive folder ID
        };

        var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
        request.Fields = "id";
        await request.UploadAsync();

        var file = request.ResponseBody;
        return file.Id; // Returns the file ID
    }

    public string GetFileUrl(string fileId)
    {
        return $"https://drive.google.com/uc?id={fileId}";
    }
}
