namespace EazyDevirt.Util;

public static class Ascii85
{
    /// <summary>
    ///     Magic numbers.
    /// </summary>
    /// <remarks>
    ///     Powers of 85: 4, 3, 2, 1, 0 respectively.
    /// </remarks>
    private static readonly uint[] Powers = { 52200625U, 614125U, 7225U, 85U, 1U };

    /// <summary>
    ///     Decodes an ascii85 string.
    /// </summary>
    /// <param name="str">Ascii85 Encoded string.</param>
    /// <remarks>Most of this is copied from decompilation.</remarks>
    /// <returns>Decoded byte array.</returns>
    public static byte[] FromAscii85String(string str)
    {
        var memoryStream = new MemoryStream(str.Length * 4 / 5);
        byte[] result;

        try
        {
            var num = 0;
            var num2 = 0u;

            foreach (var c in str)
                if (c == 'z' && num == 0)
                {
                    WriteValue(memoryStream, num2, 0);
                }
                else
                {
                    if (c is < '!' or > 'u')
                        throw new FormatException("Illegal character");
                    checked
                    {
                        num2 += (uint)(Powers[num] * (ulong)checked(c - '!'));
                    }

                    num++;
                    if (num != 5) continue;
                    WriteValue(memoryStream, num2, 0);
                    num = 0;
                    num2 = 0u;
                }

            switch (num)
            {
                case 1:
                    throw new Exception();
                case > 1:
                {
                    for (var j = num; j < 5; j++)
                        checked
                        {
                            num2 += 84u * Powers[j];
                        }

                    WriteValue(memoryStream, num2, 5 - num);
                    break;
                }
            }

            result = memoryStream.ToArray();
        }
        finally
        {
            ((IDisposable)memoryStream).Dispose();
        }

        return result;
    }

    private static void WriteValue(Stream stream, uint val, int int_0)
    {
        stream.WriteByte((byte)(val >> 24));
        if (int_0 == 3) return;
        stream.WriteByte((byte)(val >> 16 /*& 255u*/));
        if (int_0 == 2) return;
        stream.WriteByte((byte)(val >> 8 /*& 255u*/));
        if (int_0 == 1) return;
        stream.WriteByte((byte)val /*& 255u*/);
    }
}