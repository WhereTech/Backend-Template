namespace BackendTemplate.Util;

public static class PasswordUtil
{
    public static string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public static bool Validate(string password, string hash) {
        try {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch(Exception e) {
            Console.Error.WriteLine(e);
            return false;
        }
    }
}