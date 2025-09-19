using System;
using System.Security.Cryptography;

namespace EazyDevirt.Core.Crypto;

internal sealed class Skip32Cipher : SymmetricAlgorithm
{
    private sealed class Skip32 : IDisposable, ICryptoTransform
    {
        private readonly byte[] _key;
        private readonly bool _isEncrypt;

        public int InputBlockSize => 4;
        public int OutputBlockSize => 4;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => true;

        public Skip32(byte[] key, bool isEncrypt)
        {
            _key = key;
            _isEncrypt = isEncrypt;
        }

        public void Dispose() { }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputCount % 4 != 0)
                throw new ArgumentOutOfRangeException(nameof(inputCount), "Input count must be multiple of 4.");
            for (int i = 0; i < inputCount; i += 4)
                TransformOne(_key, inputBuffer, inputOffset + i, outputBuffer, outputOffset + i, _isEncrypt);
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var output = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }
    }

    private static readonly byte[] F = new byte[256]
    {
        163,215,9,131,248,72,246,244,179,33,21,120,153,177,175,249,231,45,77,138,
        206,76,202,46,82,149,217,30,78,56,68,40,10,223,2,160,23,241,96,104,18,183,122,195,233,250,
        61,83,150,132,107,186,242,99,154,25,124,174,229,245,247,22,106,162,57,182,123,15,193,147,
        129,27,238,180,26,234,208,145,47,184,85,185,218,133,63,65,191,224,90,88,128,95,102,11,216,144,
        53,213,192,167,51,6,101,105,69,0,148,86,109,152,155,118,151,252,178,194,176,254,219,32,
        225,235,214,228,221,71,74,29,66,237,158,110,73,60,205,67,39,210,7,212,222,199,103,24,137,203,
        48,31,141,198,143,170,200,116,220,201,93,92,49,164,112,136,97,44,159,13,43,135,80,130,84,100,
        38,125,3,64,52,75,28,115,209,196,253,59,204,251,127,171,230,62,91,165,173,4,35,156,20,81,34,240,
        41,121,113,126,255,140,14,226,12,239,188,114,117,111,55,161,236,211,142,98,139,134,16,232,8,119,
        17,190,146,79,36,197,50,54,157,207,243,166,187,172,94,108,169,19,87,37,181,227,189,168,58,1,5,89,42,70
    };

    public Skip32Cipher()
    {
        LegalBlockSizesValue = new[] { new KeySizes(32, 32, 0) };
        LegalKeySizesValue = new[] { new KeySizes(80, 80, 0) };
        BlockSizeValue = 32;
        KeySizeValue = 80;
        ModeValue = CipherMode.ECB;
        PaddingValue = PaddingMode.None;
    }

    public Skip32Cipher(byte[] key) : this()
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        => new Skip32(rgbKey, false);

    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        => new Skip32(rgbKey, true);

    public override void GenerateIV() => throw new NotImplementedException();
    public override void GenerateKey() => throw new NotImplementedException();

    private static ushort G(byte[] key, int k, ushort w)
    {
        byte g1 = (byte)(w >> 8);
        byte g2 = (byte)w;
        byte g3 = (byte)(F[g2 ^ key[4 * k % 10]] ^ g1);
        byte g4 = (byte)(F[g3 ^ key[(4 * k + 1) % 10]] ^ g2);
        byte g5 = (byte)(F[g4 ^ key[(4 * k + 2) % 10]] ^ g3);
        byte g6 = (byte)(F[g5 ^ key[(4 * k + 3) % 10]] ^ g4);
        return (ushort)((g5 << 8) + g6);
    }

    private static void TransformOne(byte[] key, byte[] input, int start, byte[] output, int outputIndex, bool encrypt)
    {
        int step = encrypt ? 1 : -1;
        int k = encrypt ? 0 : 23;
        ushort wl = (ushort)((input[start] << 8) + input[start + 1]);
        ushort wr = (ushort)((input[start + 2] << 8) + input[start + 3]);
        for (int i = 0; i < 12; i++)
        {
            wr ^= (ushort)(G(key, k, wl) ^ k);
            k += step;
            wl ^= (ushort)(G(key, k, wr) ^ k);
            k += step;
        }
        output[outputIndex] = (byte)(wr >> 8);
        output[outputIndex + 1] = (byte)wr;
        output[outputIndex + 2] = (byte)(wl >> 8);
        output[outputIndex + 3] = (byte)wl;
    }
}
