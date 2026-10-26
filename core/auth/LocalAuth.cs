using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LongTuong;

public class LocalAccount
{
    public string Username { get; set; } = "";
    public string Salt { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

/// <summary>
/// Local-only accounts for Phase 2.1 (no server yet).
/// Passwords are NEVER stored in plaintext: each account has a random salt and
/// only SHA-256(salt + password) is stored. Accounts live in user data, not in the repo.
/// </summary>
public static class LocalAuth
{
    public static string HashPassword(string salt, string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string NewSalt()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool IsValidUsername(string username) =>
        username.Length >= 3 && username.Length <= 20 && username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

    public static bool IsValidPassword(string password) => password.Length >= 4;

    public static List<LocalAccount> LoadAccounts(string path)
    {
        if (!File.Exists(path)) return new List<LocalAccount>();
        try { return JsonSerializer.Deserialize<List<LocalAccount>>(File.ReadAllText(path)) ?? new List<LocalAccount>(); }
        catch { return new List<LocalAccount>(); }
    }

    public static void SaveAccounts(string path, List<LocalAccount> accounts)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static bool Register(string path, string username, string password, out string error)
    {
        error = "";
        if (!IsValidUsername(username)) { error = "Tên tài khoản phải 3-20 ký tự, chỉ chữ/số/_/-."; return false; }
        if (!IsValidPassword(password)) { error = "Mật khẩu phải từ 4 ký tự trở lên."; return false; }
        var accounts = LoadAccounts(path);
        if (accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) { error = "Tài khoản đã tồn tại."; return false; }
        var salt = NewSalt();
        accounts.Add(new LocalAccount { Username = username, Salt = salt, PasswordHash = HashPassword(salt, password) });
        SaveAccounts(path, accounts);
        return true;
    }

    public static bool Login(string path, string username, string password)
    {
        var account = LoadAccounts(path).FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (account == null) return false;
        return HashPassword(account.Salt, password) == account.PasswordHash;
    }
}
