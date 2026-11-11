using System.Security.Cryptography;
using BackendTemplate.Data;
using BackendTemplate.Model;
using BackendTemplate.Options;
using BackendTemplate.Resp;
using BackendTemplate.Services.utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BackendTemplate.Services;

[InjectableService]
public class AuthService(
    AppDbContext dbContext,
    IOptions<AuthConfig> authOptions,
//-:cnd:noEmit
#if SMS
    SMSService smsService,
#endif
//+:cnd:noEmit
    ILogger<AuthService> logger)
{
    private readonly AuthConfig authConfig = authOptions.Value;

    public User? CurrentUser { get; private set; }
    public Session? Session { get; private set; }

    public async Task LoadToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var session = await dbContext.Sessions
            .Include(s => s.user)
            .FirstOrDefaultAsync(s => s.token == token);

        var expiredAt = DateTime.UtcNow.AddDays(-authConfig.SessionExpireDays);
        if (session == null || (!session.is_apikey && session.last_active_at < expiredAt))
            throw new ServiceError(ErrorCode.Unauthenticated, "登录已过期");
        if (!session.user.is_enabled)
            throw new ServiceError(ErrorCode.Unauthorized, "用户已禁用");

        session.last_active_at = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        CurrentUser = session.user;
        Session = session;
    }

    public async Task SendSms(string mobile, SmsOperation operation)
    {
        var lastSms = await dbContext.SmsCodes
            .Where(s => s.mobile == mobile)
            .OrderByDescending(s => s.send_at)
            .FirstOrDefaultAsync();
        if (lastSms != null && lastSms.send_at > DateTime.UtcNow.AddMinutes(-1))
            throw new ServiceError(ErrorCode.TooManyRequests, "请勿频繁发送验证码");

        if (operation == SmsOperation.Login && !await dbContext.Users.AnyAsync(u => u.mobile == mobile))
            throw new ServiceError(ErrorCode.ResourceNotFound, "用户不存在");
        if (operation == SmsOperation.Register && await dbContext.Users.AnyAsync(u => u.mobile == mobile))
            throw new ServiceError(ErrorCode.InvalidOperation, "手机号已注册");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
//-:cnd:noEmit
#if SMS
        await smsService.SendSMS(mobile, "VerifyCode", new { code });
#else
        logger.LogWarning("SMS is not configured. Verification code for {Mobile}: {Code}", mobile, code);
#endif
//+:cnd:noEmit

        dbContext.SmsCodes.Add(new SmsCode { mobile = mobile, code = code, operation = operation });
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Verification code created for {Mobile}, operation {Operation}", mobile, operation);
    }

    public async Task<User> Register(string name, string mobile, string code)
    {
        if (await dbContext.Users.AnyAsync(u => u.mobile == mobile))
            throw new ServiceError(ErrorCode.InvalidOperation, "手机号已注册");
        await VerifySmsCode(mobile, code, SmsOperation.Register);

        var user = new User { name = name, mobile = mobile };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        await CreateSession(user);
        return user;
    }

    public async Task Login(string mobile, string code)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.mobile == mobile)
            ?? throw new ServiceError(ErrorCode.ResourceNotFound, "用户不存在");
        if (!user.is_enabled)
            throw new ServiceError(ErrorCode.Unauthorized, "用户已禁用");
        await VerifySmsCode(mobile, code, SmsOperation.Login);
        await CreateSession(user);
    }

    private async Task CreateSession(User user)
    {
        var session = new Session {
            user = user,
            user_id = user.id,
            token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
        };
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();
        CurrentUser = user;
        Session = session;
    }

    private async Task VerifySmsCode(string mobile, string code, SmsOperation operation)
    {
        var smsCode = await dbContext.SmsCodes
            .Where(s => s.mobile == mobile && s.operation == operation)
            .OrderByDescending(s => s.send_at)
            .FirstOrDefaultAsync();
        var expiredAt = DateTime.UtcNow.AddMinutes(-authConfig.SmsCodeExpireMinutes);
        if (smsCode == null || smsCode.send_at < expiredAt || smsCode.code != code)
            throw new ServiceError(ErrorCode.ResourceNotFound, "验证码无效");

        await dbContext.SmsCodes
            .Where(s => s.mobile == mobile && s.operation == operation)
            .ExecuteDeleteAsync();
    }

    public void RequireLogin()
    {
        if (CurrentUser == null) throw new ServiceError(ErrorCode.Unauthenticated, "未登录");
    }

    public void RequireRole(params UserRole[] roles)
    {
        RequireLogin();
        if (!roles.Contains(CurrentUser!.role))
            throw new ServiceError(ErrorCode.Unauthorized, "没有权限访问");
    }

    public async Task Logout()
    {
        if (Session == null) return;
        dbContext.Sessions.Remove(Session);
        await dbContext.SaveChangesAsync();
        Session = null;
        CurrentUser = null;
    }
}
