using System.Security.Cryptography;
using System;

namespace EazyDevirt.Core.Crypto;

internal sealed class PBKDF2 : DeriveBytes
{
    private static volatile bool HasError;
    private DeriveBytes? _derived;
    private readonly byte[] _password;
    private readonly byte[] _salt;
    private readonly int _iterations;

    public PBKDF2(byte[] password, byte[] salt, int iterations)
    {
        _password = (byte[])password.Clone();
        _salt = (byte[])salt.Clone();
        _iterations = iterations;
        if (!HasError)
        {
            try
            {
                // Match sample behavior: try platform PBKDF2 (HMAC-SHA1) first.
                _derived = new Rfc2898DeriveBytes(_password, _salt, _iterations);
            }
            catch
            {
                HasError = true;
            }
        }
        if (_derived == null)
        {
            _derived = new PBKDF2_MD5(_password, _salt, _iterations);
        }
    }

    public override byte[] GetBytes(int cb)
    {
        byte[]? result = null;
        if (!HasError)
        {
            try
            {
                result = _derived!.GetBytes(cb);
            }
            catch
            {
                HasError = true;
            }
        }
        if (result == null)
        {
            _derived = new PBKDF2_MD5(_password, _salt, _iterations);
            result = _derived.GetBytes(cb);
        }
        return result;
    }

    public override void Reset()
    {
        throw new NotSupportedException();
    }

    // Fallback PBKDF2 implementation using HMAC-MD5 as PRF (mirrors decompiled sample PBKDF2-MD5).
    private sealed class PBKDF2_MD5 : DeriveBytes
    {
        private readonly byte[] _password;
        private readonly byte[] _salt;
        private readonly int _iterations;

        public PBKDF2_MD5(byte[] password, byte[] salt, int iterations)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (salt == null) throw new ArgumentNullException(nameof(salt));
            if (iterations < 1) throw new ArgumentException("iterationCount");
            _password = (byte[])password.Clone();
            _salt = (byte[])salt.Clone();
            _iterations = iterations;
        }

        public override byte[] GetBytes(int cb)
        {
            if (cb < 0) throw new ArgumentOutOfRangeException(nameof(cb));
            const int dkLen = 16; // MD5 output size in bytes
            int blocks = (cb + dkLen - 1) / dkLen;
            byte[] output = new byte[blocks * dkLen];
            int offset = 0;

            for (int i = 1; i <= blocks; i++)
            {
                byte[] t = F(_password, _salt, _iterations, i);
                Buffer.BlockCopy(t, 0, output, offset, dkLen);
                offset += dkLen;
            }

            if (cb < output.Length)
            {
                byte[] truncated = new byte[cb];
                Buffer.BlockCopy(output, 0, truncated, 0, cb);
                return truncated;
            }
            return output;
        }

        private static byte[] F(byte[] P, byte[] S, int c, int blockIndex)
        {
            using var hmac = new HMACMD5(P);
            var saltBlock = new byte[S.Length + 4];
            Buffer.BlockCopy(S, 0, saltBlock, 0, S.Length);
            // PBKDF2 uses big-endian block index
            saltBlock[S.Length] = (byte)(blockIndex >> 24);
            saltBlock[S.Length + 1] = (byte)(blockIndex >> 16);
            saltBlock[S.Length + 2] = (byte)(blockIndex >> 8);
            saltBlock[S.Length + 3] = (byte)blockIndex;
            byte[] u = hmac.ComputeHash(saltBlock);
            byte[] t = (byte[])u.Clone();
            for (int j = 2; j <= c; j++)
            {
                u = hmac.ComputeHash(u);
                for (int k = 0; k < t.Length; k++)
                    t[k] ^= u[k];
            }
            return t;
        }

        public override void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
