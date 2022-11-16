using System.Text;

namespace EazyDevirt.Architecture;

public class VMEncoding : UTF8Encoding
{
    
    public override int GetMaxByteCount(int charCount)
    {
        var max = base.GetMaxByteCount(charCount);
        return max < 16 ? 16 : base.GetMaxByteCount(charCount);
    }
}