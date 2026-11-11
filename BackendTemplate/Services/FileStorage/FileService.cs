using System.Text.RegularExpressions;
using BackendTemplate.Resp;

namespace BackendTemplate.Services;

public record UploadFileResult(string path, string fileName, string fullPath)
{
    public UploadFileResult(string path, string fileName) : this(path, fileName, $"{path}/{fileName}") { }
}

public class FileService
{
    public readonly FileStorageService fileStorageService;

    public FileService(FileStorageService fileStorageService) {
        this.fileStorageService = fileStorageService;
    }

    public Task<UploadFileResult> UploadFileByDataUrl(string dataUrl, string path, string fileName)
        => UploadFileByDataUrl(dataUrl, $"{path}/{fileName}");

    public Task<UploadFileResult> UploadFileByDataUrl(string dataUrl, string filePath) {
        var match = Regex.Match(dataUrl, "^data:(?<content_type>.*?);base64,(?<data>.*)$", RegexOptions.IgnoreCase);
        if(!match.Success)
            throw new ServiceError(ErrorCode.BadParam, "invalid data url");

        string contentType = match.Groups["content_type"].Value;
        string base64 = match.Groups["data"].Value;
        return UploadFileByBase64(base64, contentType, filePath);
    }

    public async Task<UploadFileResult> UploadFileByBase64(string base64Str, string contentType, string path, string fileName)
        => await UploadFileByBase64(base64Str, contentType, $"{path}/{fileName}");

    public async Task<UploadFileResult> UploadFileByBase64(string base64Str, string contentType, string filePath) {
        byte[] data;
        try {
            data = Convert.FromBase64String(base64Str);
        }
        catch {
            throw new ServiceError(ErrorCode.BadParam, "invalid base64 data");
        }

        MemoryStream stream = new MemoryStream(data);
        return await UploadFile(stream, contentType, filePath);
    }

    public async Task<UploadFileResult> UploadFile(Stream data, string contentType, string path, string fileName)
        => await UploadFile(data, contentType, $"{path}/{fileName}");

    public async Task<UploadFileResult> UploadFile(Stream data, string contentType, string filePath) {
        if(filePath.Contains('*')) {
            var id = Guid.NewGuid().ToString();
            filePath = filePath.Replace("*", id);
        }

        GetFileResult result = new GetFileResult(contentType, data);
        await fileStorageService.PutFile(filePath, result.Stream, result.ContentType);

        var lastSlash = filePath.LastIndexOf('/');
        var path = lastSlash >= 0 ? filePath.Substring(0, lastSlash) : "";
        var fileName = lastSlash >= 0 ? filePath.Substring(lastSlash + 1) : filePath;

        return new UploadFileResult(path, fileName);
    }

    public async Task<GetFileResult> GetFile(string path, string fileName)
        => await GetFile($"{path}/{fileName}");

    public async Task<GetFileResult> GetFile(string filePath) {
        GetFileResult result = await fileStorageService.GetFile(filePath);
        return result;
    }

    public async Task DeleteFile(string path, string fileName)
        => await DeleteFile($"{path}/{fileName}");

    public async Task DeleteFile(string filePath) {
        await fileStorageService.DeleteFile(filePath);
    }

    /// <summary>
    /// Router 返回文件数据
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="dispFileName">下载时显示的文件名</param>
    /// <param name="redirect">控制是重定向到OSS临时链接还是本地直接 Serve Data</param>
    /// <returns></returns>
    public async Task<IResult> ServeFile(string filePath, string? dispFileName = null, bool redirect = true) {
        var fileName = filePath.Substring(filePath.LastIndexOf('/') + 1);
        dispFileName ??= fileName;
        try {
            if(!redirect) {
                var file = await fileStorageService.GetFile(filePath);
                return Results.File(file.Stream, file.ContentType, dispFileName);
            }
            else {
                var url = await fileStorageService.GetTmpUrl(filePath, dispFileName);
                var result = Results.Redirect(url);
                return result;
            }
        }
        catch(Exception e) {
            return Results.NotFound(e.ToString());
        }
    }

}
