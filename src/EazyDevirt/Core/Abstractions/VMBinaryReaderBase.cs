using System.Text;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.Core.Abstractions;

// TODO: The endianness is scrambled across samples. See issue #4
internal abstract class VMBinaryReaderBase : BinaryReader, IVMBinaryReader
{
    protected VMBinaryReaderBase(Stream input, Encoding encoding, bool leaveOpen = false)
        : base(input, encoding, leaveOpen)
    {
    }

    /// <summary>
    ///     Used in reading code instructions.
    /// </summary>
    /// <remarks>
    ///     This is from a separate stream, called VMMemoryStream in the sample.
    ///     The endianness of this is also scrambled across samples, see #4.
    /// </remarks>
    public abstract int ReadInt32Special();
}