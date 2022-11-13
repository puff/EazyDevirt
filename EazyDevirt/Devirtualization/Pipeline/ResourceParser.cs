using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using EazyDevirt.Abstractions;
using EazyDevirt.PatternMatching.Patterns;

namespace EazyDevirt.Devirtualization.Pipeline;

public class ResourceParser : Stage
{
    // Rider won't shut up unless I make them nullable.
    private MethodDefinition? _resourceGetterMethod;
    private MethodDefinition? _resourceInitializationMethod;

    private protected override bool Init()
    {
        var found = FindVMStreamMethods();
        if (_resourceGetterMethod == null)
            Ctx.Console.Error("Failed to find vm resource getter / initializer method!");

        if (_resourceInitializationMethod == null)
            Ctx.Console.Error("Failed to find resource initialization method!");

        if (found && Ctx.Options.Verbose)
        {
            Ctx.Console.InfoStr("VM Resource Getter", _resourceGetterMethod!.MetadataToken.ToString());
            Ctx.Console.InfoStr("VM Resource Initializer", _resourceInitializationMethod!.MetadataToken.ToString());
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
            }

        }

        return false;
    }

    public ResourceParser(DevirtualizationContext ctx) : base(ctx)
    {
    }
}