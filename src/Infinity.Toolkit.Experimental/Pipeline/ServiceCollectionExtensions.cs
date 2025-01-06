using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Experimental.Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeline<TIn, TOut>(this IServiceCollection services, IPipeline<TIn, TOut> pipeline)
    {
        return services.AddTransient(p => pipeline);
    }

    public static IServiceCollection AddPipeline<TIn, TOut>(this IServiceCollection services, Func<IServiceProvider, IPipeline<TIn, TOut>> pipelineFactory)
    {
        return services.AddTransient(pipelineFactory);
    }
}
