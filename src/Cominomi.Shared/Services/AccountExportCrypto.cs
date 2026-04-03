using System.Security.Cryptography;
using System.Text;

namespace Cominomi.Shared.Services;

/// <summary>
/// AES-256-GCM + PBKDF2 기반 계정 내보내기 파일 암호화/복호화.
///
/// 파일 레이아웃:
///   [4B]  magic "COMA"
///   [4B]  version int32 LE (현재 1)
///   [16B] PBKDF2 salt
///   [12B] AES-GCM nonce
///   [16B] AES-GCM auth tag
///   [N B] ciphertext (UTF-8 JSON)
/// </summary>
public static class AccountExportCrypto
{
    private static readonly byte[] Magic = "COMA"u8.ToArray();
    private const int Version = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;       // AES-256
    private const int Iterations = 600_000;

    public static byte[] Encrypt(string json, string password)
    {
        var plaintext = Encoding.UTF8.GetBytes(json);

        var salt = new byte[SaltSize];
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

        var key = DeriveKey(password, salt);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        using var ms = new MemoryStream();
        ms.Write(Magic);
        ms.Write(BitConverter.GetBytes(Version));  // 4B LE
        ms.Write(salt);
        ms.Write(nonce);
        ms.Write(tag);
        ms.Write(ciphertext);
        return ms.ToArray();
    }

    public static string Decrypt(byte[] data, string password)
    {
        using var ms = new MemoryStream(data);

        // magic
        var magic = new byte[4];
        ms.ReadExactly(magic);
        if (!magic.SequenceEqual(Magic))
            throw new InvalidDataException("유효하지 않은 파일 형식입니다.");

        // version
        var verBytes = new byte[4];
        ms.ReadExactly(verBytes);
        var version = BitConverter.ToInt32(verBytes);
        if (version > Version)
            throw new NotSupportedException("더 새로운 버전의 Cominomi에서 내보낸 파일입니다.");

        var salt = new byte[SaltSize];
        ms.ReadExactly(salt);

        var nonce = new byte[NonceSize];
        ms.ReadExactly(nonce);

        var tag = new byte[TagSize];
        ms.ReadExactly(tag);

        var ciphertext = new byte[data.Length - (4 + 4 + SaltSize + NonceSize + TagSize)];
        ms.ReadExactly(ciphertext);

        var key = DeriveKey(password, salt);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        // 잘못된 비밀번호 → CryptographicException (auth tag mismatch)
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var kdf = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256);
        return kdf.GetBytes(KeySize);
    }
}
