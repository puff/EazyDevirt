using System.Reflection;

namespace EazSample
{
    internal class Program
    {
        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static int virtualized_int;

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static string virtualized_string;
        
        private static uint not_virtualized_uint;
        
        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static void Main(string[] args)
        {
            Console.WriteLine("definitely not a virtualized entrypoint");

            virtualized_int = 1337;
            not_virtualized_uint = 0xc01db33f; // 3223171903
            virtualized_string = "100% not a virtualized string";
            var ret_code = clear_method(virtualized_string);
            Console.WriteLine("Return code: " + ret_code);
        }

        private static long clear_method(string arg)
        {
            Console.WriteLine(arg);
            var ret_code = virtualized_method(virtualized_int, not_virtualized_uint);
            Console.WriteLine(ret_code % 1337); // 490 0x1EA
            return ret_code;
        }

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private static long virtualized_method(int arg1, uint arg2)
        {
            return arg1 + arg2; // 3223173240 0xC01DB878
        }
    }
}