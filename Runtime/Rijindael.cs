using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
/// <summary>
/// 加密字符类型数据
/// </summary>
public class Rijindael
{
    // 默认加密参数
    private const CipherMode DefaultCipherMode = CipherMode.CBC;
    private const PaddingMode DefaultPaddingMode = PaddingMode.PKCS7;
    private const int DefaultKeySize = 256; // AES-256

    #region 公共接口
    /// <summary>
    /// 加密字符串（自动处理密钥长度）
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="key">密钥（任意长度字符串）</param>
    /// <param name="iv">初始化向量（至少16字节）</param>
    public static string Encrypt(string plainText, string key, string iv)
    {
        ValidateInput(plainText, key, iv);

        using var aes = Aes.Create();
        ConfigureAlgorithm(aes);

        // 使用PBKDF2派生安全密钥
        var derivedKeys = DeriveKeys(key, iv, aes.KeySize / 8, aes.BlockSize / 8);
        aes.Key = derivedKeys.Key;
        aes.IV = derivedKeys.IV;

        return EncryptCore(plainText, aes);
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    public static string Decrypt(string cipherText, string key, string iv)
    {
        if (!IsValidBase64(cipherText))
        {
            Debug.LogError("无效的Base64字符串！");
            return "";
        }
        ValidateInput(cipherText, key, iv);

        using var aes = Aes.Create();
        ConfigureAlgorithm(aes);

        var derivedKeys = DeriveKeys(key, iv, aes.KeySize / 8, aes.BlockSize / 8);
        aes.Key = derivedKeys.Key;
        aes.IV = derivedKeys.IV;

        return DecryptCore(cipherText, aes);
    }
    #endregion

    #region 核心
    private static string EncryptCore(string plainText, SymmetricAlgorithm algorithm)
    {
        try
        {
            using var encryptor = algorithm.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (CryptographicException ex)
        {
            throw new CryptoException("加密失败，请检查密钥和IV参数", ex);
        }
    }

    private static string DecryptCore(string cipherText, SymmetricAlgorithm algorithm)
    {
        try
        {
            using var decryptor = algorithm.CreateDecryptor();
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        catch (FormatException)
        {
            throw new ArgumentException("无效的Base64字符串");
        }
        catch (CryptographicException ex)
        {
            throw new CryptoException("解密失败，请检查密钥和IV是否正确", ex);
        }
    }
    #endregion

    private static void ValidateInput(string text, string key, string iv)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentNullException(nameof(text));

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrEmpty(iv) || iv.Length < 16)
            throw new ArgumentException("IV必须至少包含16个字符");
    }

    private static void ConfigureAlgorithm(SymmetricAlgorithm algorithm)
    {
        algorithm.Mode = DefaultCipherMode;
        algorithm.Padding = DefaultPaddingMode;
        algorithm.KeySize = DefaultKeySize;
    }

    private static (byte[] Key, byte[] IV) DeriveKeys(
        string password,
        string salt,
        int keyBytes,
        int ivBytes)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password: password,
            salt: Encoding.UTF8.GetBytes(salt),
            iterations: 10000,
            hashAlgorithm: HashAlgorithmName.SHA256);

        return (
            Key: pbkdf2.GetBytes(keyBytes),
            IV: pbkdf2.GetBytes(ivBytes)
        );
    }
    public static bool IsValidBase64(string str)
    {
        if (string.IsNullOrEmpty(str)) return false;
        try
        {
            Convert.FromBase64String(str);
            return true;
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// 生成随机IV（16字节，Base64编码）
    /// </summary>
    internal static string GenerateIV()
    {
        using var rng = new RNGCryptoServiceProvider();
        byte[] ivBytes = new byte[16]; // 16 bytes for AES
        rng.GetBytes(ivBytes);
        return Convert.ToBase64String(ivBytes);
    }
}
public class CryptoException : Exception
{
    public CryptoException(string message, Exception inner)
        : base(message, inner) { }
}

