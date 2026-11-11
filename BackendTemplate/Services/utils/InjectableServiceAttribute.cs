namespace BackendTemplate.Services.utils;

public enum InjectType
{
    Singleton,
    Scoped,
    Transient,
}

[AttributeUsage(AttributeTargets.Class)]
public class InjectableServiceAttribute : Attribute
{
    public InjectType InjectType { get; }

    public InjectableServiceAttribute(InjectType injectType = InjectType.Scoped) {
        this.InjectType = injectType;
    }
}