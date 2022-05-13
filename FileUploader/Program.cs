using Azure.Storage.Blobs;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Xcelerate.Cloud.DocumentService.CloudStorageManager;
using Xcelerate.Common;
using Xcelerate.Common.DocumentService.Blobs.CloudStorageManager;
using Xcelerate.Data;

namespace FileUploader
{
    internal class Program
    {
        private static ICloudStorageManager _cloudStorageManager = GetBlobStorageManager();
        static void Main(string[] args)
        {
            Console.WriteLine("Manage your Azure Storage Container");


            while (true)
            {
                HandleUserInput();
            }
        }

        static void HandleUserInput()
        {
            Console.WriteLine("Choose action:");
            Console.WriteLine("1. Display info about container");
            Console.WriteLine("2. View blobs");
            Console.WriteLine("3. Upload file");
            Console.WriteLine("4. View blob versions");
            Console.WriteLine("5. Download blob with specific version");
            Console.WriteLine("6. Restore the specific version of a blob");
            Console.WriteLine("7. Delete specific version of a blob");
            Console.WriteLine("8. Delete blob (Deletes current version only)");
            Console.WriteLine("9. Delete all blob versions");


            Console.WriteLine("0. Exit");

            var action = Console.ReadLine();
            Console.WriteLine(Environment.NewLine);

            try
            {
                PerformAction(action);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can not perform this action: {e}");
            }

            Console.WriteLine(Environment.NewLine);
        }

        private static void PerformAction(string action)
        {
            switch (action)
            {
                case "1":
                    Console.WriteLine("Here is the container info");
                    Console.WriteLine("Connection string: " + GetConnectionString());
                    Console.WriteLine("Container name: " + GetContainerName());
                    break;

                case "2":
                    Console.WriteLine("Here is the list of files");
                    var allBlobs = _cloudStorageManager.ListBlobItems("");
                    foreach (var cloudBlockBlob in allBlobs)
                    {
                        Console.WriteLine(cloudBlockBlob);
                    }

                    break;

                case "3":

                    Console.WriteLine(
                        @"Choose file to upload. Enter the full path to the file. Press enter to use C:\xcelerate\version.txt");
                    var path = GetUserInputOrDefault(@"C:\xcelerate\version.txt");

                    if (!File.Exists(path))
                    {
                        Console.WriteLine($"File at path {path} does not exists in your system");
                        break;
                    }

                    Console.WriteLine(@"Enter path to this file in the container. Press enter to use Blocks/Reports/");
                    var containerPath = GetUserInputOrDefault(@"Blocks/Reports/");


                    Console.WriteLine(@"Starting to upload");
                    var fullPathInTheContainer = Path.Combine(containerPath, Path.GetFileName(path));
                    _cloudStorageManager.UploadFile(fullPathInTheContainer, File.ReadAllBytes(path));
                    Console.WriteLine($"File at path {path} was uploaded");


                    break;

                case "4":
                    {
                        if (!GetBlobNameFromUserInput(out var blobPath)) break;

                        PrintBlobVersions(blobPath);

                        break;
                    }

                case "5":
                    {
                        if (!GetBlobNameFromUserInput(out var blobPath)) break;

                        if (!GetBlobVersionFromUserInput(blobPath, out var version)) break;

                        Console.WriteLine(
                            @"Enter path to folder where you want to download the file. Press enter to use C:\xcelerate\Downloads");
                        var downloadPath = GetUserInputOrDefault(@"C:\xcelerate\Downloads");

                        var blobStream = _cloudStorageManager.ReadToStream(blobPath, version);
                        var fullDownloadPath = Path.Combine(downloadPath, blobPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(fullDownloadPath));

                        File.WriteAllBytes(fullDownloadPath, blobStream.ToArray());

                        Console.WriteLine($"Blob {blobPath} with version {version} was downloaded to {fullDownloadPath}");
                        break;
                    }

                case "6":
                    {
                        if (!GetBlobNameFromUserInput(out var blobPath)) break;

                        if (!GetBlobVersionFromUserInput(blobPath, out var version)) break;

                        _cloudStorageManager.RestoreFileToSpecificVersion(blobPath, version);

                        Console.WriteLine($"Blob {blobPath} with version {version} was restored. Here are there blob versions after the change:");

                        PrintBlobVersions(blobPath);

                        break;
                    }

                case "7":
                    {
                        if (!GetBlobNameFromUserInput(out var blobPath)) break;

                        if (!GetBlobVersionFromUserInput(blobPath, out var version)) break;

                        _cloudStorageManager.DeleteSpecificVersion(blobPath, version);

                        Console.WriteLine($"Blob version {version} was deleted. Remaining versions:");

                        PrintBlobVersions(blobPath);

                        break;
                    }

                case "8":
                    {
                        if (!GetBlobNameFromUserInput(out var blobPath)) break;

                        _cloudStorageManager.DeleteFile(blobPath);

                        Console.WriteLine($"Blob {blobPath} was deleted. Remaining versions:");

                        PrintBlobVersions(blobPath);

                        break;
                    }
                
                case "9":
                    {
                        if (!GetBlobNameFromUserInput(out var blobPath, false)) break;

                        DeleteBlobVersions(blobPath);

                        Console.WriteLine($"All versions of blob {blobPath} were deleted");

                        PrintBlobVersions(blobPath);

                        break;
                    }

                case "0":
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine(action);
                    break;
            }
        }

        private static void PrintBlobVersions(string blobPath)
        {
            var versions = _cloudStorageManager.ListBlobVersions(blobPath);

            if (!versions.Any())
            {
                Console.WriteLine("No versions are remaining");
                return;
            }

            foreach (var version in versions)
            {
                Console.WriteLine(version);
            }

            // Construct the URI for each blob version.
            foreach (var version in versions)
            {

                var stateString = version.IsLatestVersion.GetValueOrDefault() ? "Current" : "Previous";
                Console.WriteLine($"\nVersion id: {version.VersionId}\nState: {stateString}\n");
               
            }
        }
        
        private static void DeleteBlobVersions(string blobPath)
        {
            var versions = _cloudStorageManager.ListBlobVersions(blobPath);

            foreach (var version in versions)
            {
                if (version.IsLatestVersion ?? false)
                {
                    _cloudStorageManager.DeleteFile(blobPath);
                    _cloudStorageManager.DeleteSpecificVersion(blobPath, version.VersionId);
                }
                else
                    _cloudStorageManager.DeleteSpecificVersion(blobPath, version.VersionId);
            }
        }

        private static bool GetBlobNameFromUserInput(out string blobPath, bool validateExistence = true)
        {
            Console.WriteLine(
                "Enter the full path to the blob in the container including its name. Press enter to use Blocks/Reports/version.txt");
            blobPath = GetUserInputOrDefault(@"Blocks/Reports/version.txt");

            if (!validateExistence) return true;

            var blobExists = _cloudStorageManager.FileExists(blobPath);
            if (!blobExists)
            {
                Console.WriteLine($"Blob at path {blobPath} does not exist");
                return false;
            }

            return true;
        }

        private static bool GetBlobVersionFromUserInput(string blobPath, out string version)
        {
            Console.WriteLine(@"Specify version to be used.  E.g: 2022-02-17T11:55:59.7204332Z");
            version = GetUserInputOrDefault("2022-02-17T11:55:59.7204332Z");

            var blobExists = _cloudStorageManager.FileExists(blobPath, version);
            if (!blobExists)
            {
                Console.WriteLine($"Blob at path {blobPath} with version {version} does not exist");
                return false;
            }

            return true;
        }

        private static string GetUserInputOrDefault(string defaultValue)
        {
            var userInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userInput)) return defaultValue;
            return userInput;
        }

        static IOperationContext GetOperationContext() => new OperationContext() { BlobContainerName = GetContainerName() };

        static ICloudStorageManager GetBlobStorageManager() => new BlobStorageManager(GetOperationContext(), GetConnectionString());


        static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["blobConnectionString"];
        }

        static string GetContainerName()
        {
            return ConfigurationManager.AppSettings["blobContainerName"];
        }
    }
}
