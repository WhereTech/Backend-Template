using Amazon.S3;
using Amazon.S3.Model;
using BackendTemplate.Options;
using Microsoft.Extensions.Options;

namespace BackendTemplate.Services;

public record GetFileResult(string ContentType, Stream Stream);

public class FileStorageService
{
    private readonly AmazonS3Client s3Client;
    private readonly string bucketName;
    private readonly ILogger<FileStorageService> logger;

    public FileStorageService(IOptions<FileStorageConfig> fileStorageOptions, ILogger<FileStorageService> logger) {
        var minioConfig = fileStorageOptions.Value;
        var s3Cfg = new AmazonS3Config() {
            ServiceURL = minioConfig.Endpoint,
            ForcePathStyle = true,
        };
        bucketName = minioConfig.Bucket;
        s3Client = new AmazonS3Client(minioConfig.AccessKey, minioConfig.SecretKey, s3Cfg);

        this.logger = logger;
    }

    public async Task PutFile(string path, string fileName, byte[] data, string contentType)
        => await PutFile($"{path}/{fileName}", data, contentType);

    public async Task PutFile(string filePath, byte[] data, string contentType)
        => await PutFile(filePath, new MemoryStream(data), contentType);

    public async Task PutFile(string path, string fileName, Stream stream, string contentType)
        => await PutFile($"{path}/{fileName}", stream, contentType);

    public async Task PutFile(string filePath, Stream stream, string contentType) {
        var args = new PutObjectRequest {
            BucketName = bucketName,
            Key = filePath,
            ContentType = contentType,
            InputStream = stream,
            DisableDefaultChecksumValidation = true
        };
        try {
            await s3Client.PutObjectAsync(args);
            logger.LogInformation($"Put file {filePath}");
        }
        catch(Exception e) {
            logger.LogError(e, $"Failed to put file {filePath}");
            throw;
        }
    }

    public async Task<GetFileResult> GetFile(string path, string fileName)
        => await GetFile($"{path}/{fileName}");

    public async Task<GetFileResult> GetFile(string filePath) {
        var args = new GetObjectRequest {
            BucketName = bucketName,
            Key = filePath,
        };

        try {
            var resp = await s3Client.GetObjectAsync(args);
            logger.LogInformation($"Get file {filePath}");
            return new GetFileResult(resp.Headers.ContentType, resp.ResponseStream);
        }
        catch(Exception e) {
            logger.LogError(e, $"Failed to get file {filePath}");
            throw;
        }
    }

    public async Task<string> GetTmpUrl(string filePath, string? dispFileName = null, int expireSeconds = 3600) {
        if(dispFileName == null) {
            var lastSlash = filePath.LastIndexOf('/');
            dispFileName = lastSlash >= 0 ? filePath.Substring(lastSlash + 1) : filePath;
        }

        var request = new GetPreSignedUrlRequest {
            BucketName = bucketName,
            Key = filePath,
            Expires = DateTime.UtcNow.Add(TimeSpan.FromSeconds(expireSeconds)),
            ResponseHeaderOverrides = new ResponseHeaderOverrides() {
                ContentDisposition = "attachment; filename*=UTF-8''" + dispFileName
            }
        };
        try {
            var url = await s3Client.GetPreSignedURLAsync(request);
            logger.LogDebug($"Get presigned URL for file {filePath}");
            return url;
        }
        catch(Exception e) {
            logger.LogError(e, $"Failed to get presigned URL for file {filePath}");
            throw;
        }
    }


    public async Task DeleteFile(string path, string fileName)
        => await DeleteFile($"{path}/{fileName}");

    public async Task DeleteFile(string filePath) {
        var args = new DeleteObjectRequest {
            BucketName = bucketName,
            Key = filePath,
        };
        try {
            await s3Client.DeleteObjectAsync(args);
            logger.LogInformation($"Deleted file {filePath}");
        }
        catch(Exception e) {
            logger.LogError(e, $"Failed to delete file {filePath}");
            throw;
        }
    }
}
