using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;
using EazyDevirt.PatternMatching.Patterns;
#pragma warning disable CS8618

namespace EazyDevirt.Devirtualization.Pipeline;

public class ResourceParser : Stage
{
    // Rider won't shut up unless I make them nullable.
    private MethodDefinition? _resourceGetterMethod;
    private MethodDefinition? _resourceInitializationMethod;
    private MethodDefinition? _resourceModulusStringMethod;
    private ManifestResource? _resource;

    private string _resourceString;
    private byte[] _keyBytes;
    private string _modulusString;
    
    private protected override bool Init()
    {
        var found = FindVMStreamMethods();
        if (_resourceGetterMethod == null)
            Ctx.Console.Error("Failed to find vm resource stream getter method.");

        if (_resourceInitializationMethod == null)
            Ctx.Console.Error("Failed to find vm resource stream initialization method.");

        if (_resourceModulusStringMethod == null || _resourceModulusStringMethod.CilMethodBody!.Instructions.All
                (i => i.OpCode != CilOpCodes.Ldstr))
            Ctx.Console.Error("Failed to find vm resource modulus string method. Have strings been decrypted?");

        if (found && Ctx.Options.Verbose)
        {
            Ctx.Console.Success("Found vm resource stream getter, initializer, and modulus string methods!");

            if (Ctx.Options.VeryVerbose)
            {
                Ctx.Console.InfoStr("VM Resource Stream Getter", _resourceGetterMethod!.MetadataToken);
                Ctx.Console.InfoStr("VM Resource Stream Initializer", _resourceInitializationMethod!.MetadataToken);
                Ctx.Console.InfoStr("VM Resource Modulus String Method", _resourceModulusStringMethod!.MetadataToken);
            }
        }

        _resourceString = _resourceGetterMethod!.CilMethodBody!.Instructions[5].Operand?.ToString()!;
        _resource = Ctx.Module.Resources.FirstOrDefault(r => r.Name == _resourceString);
        if (_resource == null)
        {
            Ctx.Console.Error("Failed to get resource");
            found = false;
        }
        else if (Ctx.Options.Verbose)
        {
            Ctx.Console.Success("Found vm resource!");
            if (Ctx.Options.VeryVerbose)
                Ctx.Console.InfoStr("VM Resource", _resourceString);
        }

        var a1 = (SerializedFieldDefinition)_resourceGetterMethod!.CilMethodBody!.Instructions[10].Operand!;
        if (!a1.HasFieldRva || a1.FieldRva!.GetType() != typeof(DataSegment))
        {
            Ctx.Console.Error("Failed to get vm resource stream key byte array.");
            found = false;
        }

        _keyBytes = ((DataSegment)a1.FieldRva!).Data;
        if (Ctx.Options.Verbose)
        {
            Ctx.Console.Success("Found vm resource stream key bytes!");
            if (Ctx.Options.VeryVerbose)
                Ctx.Console.InfoStr("VM Resource Stream Key Bytes", BitConverter.ToString(_keyBytes));
        }

        _modulusString = _resourceModulusStringMethod!.CilMethodBody!.Instructions.FirstOrDefault
            (i => i.OpCode == CilOpCodes.Ldstr)!.Operand?.ToString()!;
        if (string.IsNullOrWhiteSpace(_modulusString))
        {
            Ctx.Console.Error("VM Resource Modulus String is null.");
            found = false;
        } 
        else if (Ctx.Options.Verbose)
        {
            Ctx.Console.Success("Found vm resource modulus string!");
            if (Ctx.Options.VeryVerbose)
                Ctx.Console.InfoStr("VM Resource Modulus String", _modulusString);
        }

        return found;
    }

    public override bool Run()
    {
        // the fun begins...
        if (!Init()) return false;

        
        
        return true;
    }

    private bool FindVMStreamMethods()
    {
        foreach (var type in Ctx.Module.GetAllTypes())
        {
            if (_resourceGetterMethod != null && _resourceInitializationMethod != null) return true;
            foreach (var method in type.Methods.Where(m =>
                         m.Managed && m.IsPublic && m.IsStatic &&
                         m.Signature?.ReturnType.FullName == typeof(Stream).FullName))
            {
                if (_resourceGetterMethod != null && _resourceInitializationMethod != null) return true;
                // TODO: make a better way of using non-vm patterns
                if (_resourceGetterMethod != null ||
                    !PatternMatching.PatternMatcher.MatchesPattern(new GetVMStreamPattern(), method)) continue;
                
                _resourceGetterMethod = method;
                _resourceInitializationMethod =
                    (SerializedMethodDefinition)method.CilMethodBody!.Instructions[13].Operand!;
                _resourceModulusStringMethod =
                    (SerializedMethodDefinition)method.CilMethodBody!.Instructions[12].Operand!;
            }

        }

        return false;
    }

    public ResourceParser(DevirtualizationContext ctx) : base(ctx)
    {
    }
}