using AsmResolver.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EazyDevirt.Util
{
    internal static class TypeDefinitionExtensions
    {
        public static List<TypeDefinition> GetAllInheriting(this TypeDefinition typeDefinition, TypeDefinition typeToFind)
        {
            List<TypeDefinition> inheritedTypes = new();
            var module = typeDefinition.Module;

            if(module == null)
                return inheritedTypes;

            foreach (var t in module.GetAllTypes())
                if (t.BaseType != null && t.BaseType.MetadataToken == typeToFind.MetadataToken)
                    inheritedTypes.Add(t);

            return inheritedTypes;
        }
    }
}
