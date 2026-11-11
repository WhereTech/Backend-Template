using Microsoft.AspNetCore.Mvc;

namespace BackendTemplate.Resp;

public class ServerResponse : JsonResult
{
    ServerResponse(ResponseDto data) : base(data) {
        this.model = data;
    }

    public readonly ResponseDto model;

    public static ServerResponse Ok(object? data = null) {
        return new ServerResponse(ResponseDto.Ok(data));
    }

    public static ServerResponse Err(ErrorCode errcode, string? errmsg = null) {
        return new ServerResponse(ResponseDto.Err(errcode, errmsg));
    }
}