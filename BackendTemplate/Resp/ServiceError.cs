namespace BackendTemplate.Resp;

public class ServiceError : Exception
{
    public readonly ErrorCode ErrCode;
    public readonly string ErrMsg;

    public ServiceError(ErrorCode code, string? msg, Exception? innerException = null)
        : base($"{code.ToString()}({(int)code}): {msg}", innerException) {
        this.ErrCode = code;
        this.ErrMsg = msg ?? code.ToString();
    }
}