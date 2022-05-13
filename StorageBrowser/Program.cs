using Microsoft.Extensions.Configuration;
using Xcelerate.Cloud.DocumentService;
using Xcelerate.Cloud.DocumentService.CloudStorageManager;
using Xcelerate.Common;
using Xcelerate.Common.DocumentService.Blobs.CloudStorageManager;
using Xcelerate.Data;


Console.WriteLine("Manage your Azure Storage Container");
while (true)
{
    HandleUserInput();
}

static IOperationContext GetOperationContext() => new OperationContext() { BlobContainerName = GetContainerName() };

static ICloudStorageManager GetBlobStorageManager() => new BlobStorageManager(GetOperationContext(), GetConnectionString());

static void HandleUserInput()
{
    Console.WriteLine("Choose action:");
    Console.WriteLine("1. Display info about container");
    Console.WriteLine("2. View files");
    Console.WriteLine("3. Upload file");
    Console.WriteLine("0. Exit");

    var action = Console.ReadLine();
    Console.WriteLine(Environment.NewLine);
    switch (action)
    {
        case "1":
            Console.WriteLine("Here is the container info");
            Console.WriteLine("Connection string: " + GetConnectionString());
            Console.WriteLine("Container name: " + GetContainerName());
            break;
        case "2":
            Console.WriteLine("Here is the list of files");
            break;
        case "3":
            try
            {
                Console.WriteLine(@"Choose file to upload. Enter the full path to the file. E.g: C:\xcelerate\version.txt");
                var path = Console.ReadLine();

                if (!File.Exists(path))
                {
                    Console.WriteLine($"File at path {path} does not exists in your system");
                    break;
                }
                Console.WriteLine(@"Enter path to this file in the container. E.g: Blocks/Reports/");
                var containerPath = Console.ReadLine();


                Console.WriteLine(@"Starting to upload");
                GetBlobStorageManager().UploadFile(containerPath, File.ReadAllBytes(path));
                Console.WriteLine($"File at path {path} was uploaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading the file to Blob container: {ex}");
            }
            break;
        case "0":
            Environment.Exit(0);
            break;
        default:
            Console.WriteLine(action);
            break;
    }
    Console.WriteLine(Environment.NewLine);
}

static string GetConnectionString()
{
    IConfiguration Config = new ConfigurationBuilder()
        .AddJsonFile("appSettings.json")
        .Build();

    return Config.GetSection("blobConnectionString").Value;
}

static string GetContainerName()
{
    IConfiguration Config = new ConfigurationBuilder()
        .AddJsonFile("appSettings.json")
        .Build();

    return Config.GetSection("blobContainerName").Value;
}
