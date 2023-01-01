using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types.Parsing;
using EazyDevirt.Architecture.InlineOperands;
using EazyDevirt.Devirtualization;

namespace EazyDevirt.Core.IO;

internal class Resolver
{
    private DevirtualizationContext Ctx { get; }
    
    private CryptoStreamV3 VMStream { get; }
    private VMBinaryReader VMStreamReader { get; }
    
    public Resolver(DevirtualizationContext ctx)
    {
        Ctx = ctx;
        VMStream = new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true);
        VMStreamReader = new VMBinaryReader(VMStream, true);
    }

    public ITypeDefOrRef ResolveType(int position)
    {
        VMStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return (ITypeDefOrRef)Ctx.Module.LookupMember(inlineOperand.Token);

        if (!inlineOperand.HasData || inlineOperand.Data is not VMTypeData data)
            throw new Exception("VM inline operand expected to have type data!");

        var typeDefOrRef = TypeNameParser.Parse(Ctx.Module, data.TypeName).ToTypeDefOrRef();

        if (!data.HasGenericTypes) return typeDefOrRef;
        
        // this looks like it's working, not 100% sure though.
        var generics = data.GenericTypes.Select(g => ResolveType(g.Position)).Select(gtype => gtype.ToTypeSignature()).ToArray();
        return typeDefOrRef.MakeGenericInstanceType(generics).ToTypeDefOrRef();
    }

    public FieldDefinition ResolveField(int position)
    {
        VMStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return (FieldDefinition)Ctx.Module.LookupMember(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMFieldData data)
            throw new Exception("VM inline operand expected to have field data!");

        var declaringType = ResolveType(data.FieldType.Position);
        if (declaringType == null)
            throw new Exception("Unable to resolve vm field type as TypeDef or TypeRef");

        var fieldDefinition = ((TypeDefinition)declaringType).Fields.First(f => f.Name == data.Name);

        return fieldDefinition;
    }
}