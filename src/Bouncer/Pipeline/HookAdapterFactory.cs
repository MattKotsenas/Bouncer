using System.Text.Json;

namespace Bouncer.Pipeline;

public interface IHookAdapterFactory
{
    IHookAdapter Create(JsonElement root);

    IHookAdapter Default { get; }
}

public sealed class HookAdapterFactory : IHookAdapterFactory
{
    private readonly IReadOnlyList<IHookAdapter> _adapters;

    public HookAdapterFactory(IEnumerable<IHookAdapter> adapters)
    {
        _adapters = adapters.ToList();
    }

    public IHookAdapter Default => _adapters[0];

    public IHookAdapter Create(JsonElement root)
    {
        foreach (var adapter in _adapters)
        {
            if (adapter.CanHandle(root))
                return adapter;
        }

        return Default;
    }
}
