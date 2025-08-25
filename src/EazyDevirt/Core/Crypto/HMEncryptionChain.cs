using System.Security.Cryptography;

namespace EazyDevirt.Core.Crypto;

internal abstract class HMEncryptionChain
{
    private readonly SymmetricAlgorithm[] _algorithmChains;

    protected HMEncryptionChain(byte[] password, long salt)
        : this(password, ConvertLongToLittleEndian(salt))
    {
    }

    protected HMEncryptionChain(byte[] password, byte[] salt)
    {
        var pbkdf = new PBKDF2(password, salt, 1);
        var array = new SymmetricAlgorithm[5];
        for (int i = 0; i < 5; i++)
        {
            var chain = new SymmetricAlgorithmChain(new Skip32Cipher());
            chain.Key = pbkdf.GetBytes(chain.KeySize / 8);
            chain.IV = pbkdf.GetBytes(chain.GetIVSize() / 8);
            array[i] = chain;
        }

        _algorithmChains = array;
    }

    protected static int AlignToMultipleOf4(int value) => (value + 3) / 4 * 4;

    public static int MinAlignToMultipleOf4(int value) => AlignToMultipleOf4(value + 4);

    protected static byte[] ConvertLongToLittleEndian(long value)
    {
        var output = new byte[8];
        ConvertLongToLittleEndian(value, output, 0);
        return output;
    }

    protected static void ConvertLongToLittleEndian(long value, byte[] output, int startIndex)
    {
        output[startIndex] = (byte)value;
        output[startIndex + 1] = (byte)(value >> 8);
        output[startIndex + 2] = (byte)(value >> 16);
        output[startIndex + 3] = (byte)(value >> 24);
        output[startIndex + 4] = (byte)(value >> 32);
        output[startIndex + 5] = (byte)(value >> 40);
        output[startIndex + 6] = (byte)(value >> 48);
        output[startIndex + 7] = (byte)(value >> 56);
    }

    protected static int ConvertInt32BytesToLittleEndian(byte[] bytes, int startIndex)
    {
        return bytes[startIndex]
             | (bytes[startIndex + 1] << 8)
             | (bytes[startIndex + 2] << 16)
             | (bytes[startIndex + 3] << 24);
    }

    protected static void ConvertInt32ToLittleEndian(int value, byte[] output, int startIndex)
    {
        output[startIndex] = (byte)value;
        output[startIndex + 1] = (byte)(value >> 8);
        output[startIndex + 2] = (byte)(value >> 16);
        output[startIndex + 3] = (byte)(value >> 24);
    }

    protected byte[] DecryptBytes(byte[] input, bool startWithEncrypt)
    {
        if (startWithEncrypt)
        {
            foreach (var alg in _algorithmChains)
            {
                if (startWithEncrypt)
                {
                    using var enc = alg.CreateEncryptor();
                    input = enc.TransformFinalBlock(input, 0, input.Length);
                }
                else
                {
                    using var dec = alg.CreateDecryptor();
                    input = dec.TransformFinalBlock(input, 0, input.Length);
                }
                startWithEncrypt = !startWithEncrypt;
            }
        }
        else
        {
            for (int i = _algorithmChains.Length - 1; i >= 0; i--)
            {
                var alg = _algorithmChains[i];
                if (startWithEncrypt)
                {
                    using var enc = alg.CreateEncryptor();
                    input = enc.TransformFinalBlock(input, 0, input.Length);
                }
                else
                {
                    using var dec = alg.CreateDecryptor();
                    input = dec.TransformFinalBlock(input, 0, input.Length);
                }
                startWithEncrypt = !startWithEncrypt;
            }
        }

        return input;
    }
}
