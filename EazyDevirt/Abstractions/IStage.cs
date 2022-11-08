using EazyDevirt.Devirtualization;

namespace EazyDevirt.Abstractions;

public interface IStage
{
    string Name { get; }
    void Run(DevirtualizationContext ctx);
}