using System.Reflection;

namespace EazSample
{
    internal class Program
    {
        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static int _virtualizedInt;

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static string? _virtualizedString;
        
        private static uint _notVirtualizedUint;
        
        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static void Main(string[] args)
        {
            Console.WriteLine("definitely not a virtualized entrypoint");

            _virtualizedInt = 1337;
            _notVirtualizedUint = 0xc01db33f; // 3223171903
            _virtualizedString = "100% not a virtualized string";
            var retCode = ClearMethod(_virtualizedString);
            Console.WriteLine("Return code: " + retCode);
        }

        private static long ClearMethod(string? arg)
        {
            Console.WriteLine(arg);
            var retCode = VirtualizedMethod(_virtualizedInt, _notVirtualizedUint);
            Console.WriteLine(retCode % 1337); // 490 0x1EA
            return retCode;
        }

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static long VirtualizedMethod(int arg1, uint arg2)
        {
            return arg1 + arg2; // 3223173240 0xC01DB878
        }
    }
}