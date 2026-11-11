using System.Reflection;

namespace BackendTemplate.Services.utils;

public static class InjectHelper
{
    public static void AutoInjectCustomServices(this IServiceCollection Services) {
        // find all classes that has a InjectableServiceAttribute
        var serviceClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<InjectableServiceAttribute>() != null);
        foreach(var serviceClass in serviceClasses) {
            InjectServiceClass(Services, serviceClass);
        }
    }

    private static void InjectServiceClass(IServiceCollection Services, Type serviceClass) {
        var injectType = serviceClass.GetCustomAttribute<InjectableServiceAttribute>()!.InjectType;
        switch(injectType) {
            case InjectType.Singleton:
                Services.AddSingleton(serviceClass);
                break;
            case InjectType.Scoped:
                Services.AddScoped(serviceClass);
                break;
            case InjectType.Transient:
                Services.AddTransient(serviceClass);
                break;
        }
    }
}