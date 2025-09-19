using System;
using System.Security.Cryptography;

namespace EazyDevirt.Core.Crypto;

internal sealed class SymmetricAlgorithmChain : SymmetricAlgorithm
{
    private sealed class XorTransform : IDisposable, ICryptoTransform
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly SymmetricAlgorithm[] _algs;
        private ICryptoTransform[]? _transforms;
        private readonly bool _isEncryption;
        private readonly int _blockSize;

        public int InputBlockSize => _blockSize;
        public int OutputBlockSize => _blockSize;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => true;

        public XorTransform(SymmetricAlgorithm[] algorithms, byte[] key, byte[] iv, bool isEncryption)
        {
            _key = key;
            _iv = iv;
            _algs = algorithms;
            _isEncryption = isEncryption;
            _blockSize = algorithms[^1].BlockSize / 8;
        }

        public void Dispose()
        {
            if (_transforms != null)
            {
                foreach (var t in _transforms)
                    t?.Dispose();
                _transforms = null;
            }
        }

        private void EnsureTransforms()
        {
            if (_transforms != null) return;
            var n = _algs.Length;
            var arr = new ICryptoTransform[n];
            var offset = 0;
            for (int i = 0; i < n; i++)
            {
                var alg = _algs[i];
                var keySizeBytes = alg.KeySize / 8;
                var key = new byte[keySizeBytes];
                Buffer.BlockCopy(_key, offset, key, 0, keySizeBytes);
                offset += keySizeBytes;
                var iv = new byte[alg.BlockSize / 8];
                var t = _isEncryption ? alg.CreateEncryptor(key, iv) : alg.CreateDecryptor(key, iv);
                arr[i] = t;
            }

            _transforms = arr;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var output = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
            EnsureTransforms();
            if (_isEncryption)
                Encrypt(outputBuffer, outputOffset, inputCount);
            else
                Decrypt(outputBuffer, outputOffset, inputCount);
            return inputCount;
        }

        private void Encrypt(byte[] buffer, int offset, int count)
        {
            var iv = new byte[_iv.Length];
            Buffer.BlockCopy(_iv, 0, iv, 0, iv.Length);
            var pos = 0;
            foreach (var t in _transforms!)
            {
                var bs = t.InputBlockSize;
                var len = (count - pos) & ~(bs - 1);
                var end = pos + len;
                for (int j = pos; j < end; j += bs)
                {
                    var p = j + offset;
                    Xor(buffer, p, iv, 0, bs);
                    t.TransformBlock(buffer, p, bs, buffer, p);
                    Buffer.BlockCopy(buffer, p, iv, 0, bs);
                }
                pos = end;
                if (end == count) break;
            }
        }

        private void Decrypt(byte[] buffer, int offset, int count)
        {
            var iv = new byte[_iv.Length];
            Buffer.BlockCopy(_iv, 0, iv, 0, iv.Length);
            var tmp = new byte[iv.Length];
            var pos = 0;
            foreach (var t in _transforms!)
            {
                var bs = t.InputBlockSize;
                var len = (count - pos) & ~(bs - 1);
                var end = pos + len;
                for (int j = pos; j < end; j += bs)
                {
                    var p = j + offset;
                    Buffer.BlockCopy(buffer, p, tmp, 0, bs);
                    t.TransformBlock(buffer, p, bs, buffer, p);
                    Xor(buffer, p, iv, 0, bs);
                    Buffer.BlockCopy(tmp, 0, iv, 0, bs);
                }
                pos = end;
                if (end == count) break;
            }
        }

        private static void Xor(byte[] buffer, int offset, byte[] iv, int ivOffset, int count)
        {
            for (int i = 0; i < count; i++)
                buffer[offset + i] ^= iv[ivOffset + i];
        }
    }

    private static class SymmetricAlgorithmComparer
    {
        public static int CompareBlockSize(SymmetricAlgorithm a, SymmetricAlgorithm b) => b.BlockSize.CompareTo(a.BlockSize);
    }

    private readonly SymmetricAlgorithm[] _algs;
    private readonly int _ivSize;

    public override byte[] IV
    {
        get => base.IV;
        set => IVValue = (byte[])value.Clone();
    }

    public SymmetricAlgorithmChain(params SymmetricAlgorithm[] algorithms)
    {
        algorithms = (SymmetricAlgorithm[])algorithms.Clone();
        Array.Sort(algorithms, SymmetricAlgorithmComparer.CompareBlockSize);
        _algs = algorithms;
        var totalKeyBits = 0;
        foreach (var alg in algorithms)
        {
            totalKeyBits += alg.KeySize;
            alg.Mode = CipherMode.ECB;
            alg.Padding = PaddingMode.None;
        }

        BlockSizeValue = algorithms[^1].BlockSize;
        LegalBlockSizesValue = new[] { new KeySizes(BlockSizeValue, BlockSizeValue, 0) };
        KeySizeValue = totalKeyBits;
        LegalKeySizesValue = new[] { new KeySizes(totalKeyBits, totalKeyBits, 0) };
        _ivSize = algorithms[0].BlockSize;
        Mode = CipherMode.ECB;
        Padding = PaddingMode.None;
    }

    public int GetIVSize() => _ivSize;

    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) => CreateXorTransform(rgbKey, rgbIV, false);
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) => CreateXorTransform(rgbKey, rgbIV, true);

    private ICryptoTransform CreateXorTransform(byte[] rgbKey, byte[] rgbIv, bool isEncryption)
    {
        if (rgbKey.Length * 8 != KeySize)
            throw new ArgumentException("Invalid key size.");
        if (rgbIv.Length * 8 != GetIVSize())
            throw new ArgumentException("Invalid IV size.");
        return new XorTransform(_algs, rgbKey, rgbIv, isEncryption);
    }

    public override void GenerateIV() => throw new NotSupportedException();
    public override void GenerateKey() => throw new NotSupportedException();
}
