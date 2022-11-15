using System.Text;

namespace EazyDevirt.Architecture;

internal class VMBinaryReader : BinaryReader
{
    
    
    public VMBinaryReader(Stream input) : base(input, new UTF8Encoding())
    {
        
    }
    
}