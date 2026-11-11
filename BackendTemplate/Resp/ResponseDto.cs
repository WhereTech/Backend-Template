namespace BackendTemplate.Resp;

public class ResponseDto
{
    public required string errCode { get; set; }
    public string? errMsg { get; set; }
    public object? data { get; set; }

    public static ResponseDto Ok(object? data = null) {
        return new ResponseDto() {
            errCode = ErrorCode.Ok.ToString(),
            data = data
        };
    }

    public static ResponseDto Err(ErrorCode errcode, string? errmsg) {
        return new ResponseDto() {
            errCode = errcode.ToString(),
            errMsg = errmsg ?? ""
        };
    }
}