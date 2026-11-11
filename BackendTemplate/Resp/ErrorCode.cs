namespace BackendTemplate.Resp;

public enum ErrorCode
{
    Ok,
    InternalError,
    Unauthenticated, // 未登录
    Unauthorized, // 授权失败
    BadParam,
    ResourceNotFound,
    InvalidOperation,
    TooManyRequests,
}