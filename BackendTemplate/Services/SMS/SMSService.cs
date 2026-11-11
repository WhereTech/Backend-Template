using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BackendTemplate.Options;
using BackendTemplate.Resp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Tea;
using Tea.Utils;
using SMS = AlibabaCloud.SDK.Dysmsapi20170525;

namespace BackendTemplate.Services;

public class SMSService
{
    private SMSConfig smsConfig;
    private SMS.Client client;
    private ILogger<SMSService> logger;

    public SMSService(IOptions<SMSConfig> smsOptions, ILogger<SMSService> logger) {
        this.smsConfig = smsOptions.Value;
        this.logger = logger;
        client = new(new AlibabaCloud.OpenApiClient.Models.Config {
            AccessKeyId = smsConfig.AccessKeyId,
            AccessKeySecret = smsConfig.AccessKeySecret,
            Endpoint = "dysmsapi.aliyuncs.com",
            RegionId = smsConfig.SdkRegion,
        });
    }

    public async Task SendSMS(string mobile, string templateKey, object parameters) {
        if(mobile.StartsWith("1") && mobile.Length == 11) {
            mobile = "86" + mobile;
        }
        else if(!mobile.StartsWith("86") || mobile.Length != 13) {
            throw new ArgumentException("Invalid mobile number");
        }

        SMS.Models.SendSmsRequest request = new() {
            SignName = smsConfig.SignName,
            PhoneNumbers = mobile,
            TemplateCode = smsConfig.TemplateCode[templateKey],
            TemplateParam = JsonConvert.SerializeObject(parameters),
        };
        AlibabaCloud.TeaUtil.Models.RuntimeOptions options = new() {
            Autoretry = true,
            MaxAttempts = 3,
        };

        // 复制代码运行请自行打印 API 的返回值
        SMS.Models.SendSmsResponse resp;
        try {
            resp = await client.SendSmsWithOptionsAsync(request, options);
        }
        catch(TeaException e) {
            logger.LogError(e, $"Failed to send SMS, status={e.StatusCode} code={e.Code}");

            var errCode = e.Code;
            if(errCode == "InvalidParam.PhoneNumber" || errCode == "InvalidParam.phoneNumber" || errCode == "InvalidParameter.To" || errCode == "isv.MOBILE_NUMBER_ILLEGAL") {
                throw new ServiceError(ErrorCode.BadParam, "手机号错误");
            }

            if(errCode == "OutOfService") {
                throw new ServiceError(ErrorCode.BadParam, "手机号不可接收短信");
            }

            if(errCode == "isv.BUSINESS_LIMIT_CONTROL" || errCode == "isv.DAY_LIMIT_CONTROL" || errCode.StartsWith("VALVE")) {
                throw new ServiceError(ErrorCode.BadParam, "发送过于频繁，请稍后重试");
            }

            throw new ServiceError(ErrorCode.InternalError, "短信发送失败，请稍后重试" + $"({errCode})");
        }
        catch(Exception e) {
            logger.LogError(e, "Exception when sending SMS");
            throw new ServiceError(ErrorCode.InternalError, "短信发送失败，请稍后重试");
        }

        if(resp.StatusCode != 200 || resp.Body.Code != "OK") {
            logger.LogError("Failed to send SMS, response: " + JsonConvert.SerializeObject(resp));
            throw new ServiceError(ErrorCode.InternalError, "短信发送失败，请稍后重试");
        }
    }
}
