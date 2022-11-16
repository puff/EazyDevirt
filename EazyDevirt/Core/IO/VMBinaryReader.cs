using EazyDevirt.Architecture;

namespace EazyDevirt.Core.IO;

internal class VMBinaryReader : BinaryReader
{
    
    
    public VMBinaryReader(Stream input) : base(input, new VMEncoding())
    {
        
    }
    
}