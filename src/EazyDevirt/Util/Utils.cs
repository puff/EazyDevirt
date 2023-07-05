using AsmResolver.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EazyDevirt.Util
{
    internal class Utils
    {
        public static int GetFieldCountFromRetType(TypeDefinition t, string typeName)
        {
            return t.Fields.Count(x => x.Signature?.FieldType.ToString() == typeName);
        }
    }
}
