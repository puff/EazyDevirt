using EazyDevirt.Core.Abstractions;

namespace EazyDevirt.Core.IO;

/// <summary>
///     Stream that performs cryptographic operations using version 3 of the encryption scheme.
/// </summary>
public class CryptoStreamV3 : CryptoStreamBase
{
    private const uint CONSTANT = 0xDEADDE4D;

    /// <summary>
    ///     Initializes a new instance of the CryptoStreamV3 class with the specified stream, encryption key, and leaveOpen
    ///     flag.
    /// </summary>
    /// <param name="stream">The stream to perform cryptographic operations on.</param>
    /// <param name="key">The encryption key to use.</param>
    /// <param name="leaveOpen">True to leave the stream open after the CryptoStreamV3 object is disposed; otherwise, false.</param>
    public CryptoStreamV3(Stream stream, int key, bool leaveOpen = false) : base(stream, key, leaveOpen)
    {
        CryptoKey = (int)(key ^ CONSTANT); // 0xDEADDE4D in signed two's complements. This is consistent across every sample I've seen.
    }

    private int CryptoKey { get; }

    /// <summary>
    ///     Overrides the abstract method Crypt to perform the cryptographic operation using the version 3 encryption scheme.
    /// </summary>
    /// <param name="inputByte">The byte to encrypt/decrypt.</param>
    /// <param name="inputKey">The key to use for encryption/decryption.</param>
    /// <returns>The encrypted/decrypted byte.</returns>
    protected override byte Crypt(byte inputByte, uint inputKey)
    {
        var xoredKey = (byte)(CryptoKey ^ (int)inputKey);
        return (byte)(inputByte ^ xoredKey);
    }
}