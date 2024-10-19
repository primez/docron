using System.Text.Json;
using Quartz.Simpl;

namespace Docron;

public sealed class QuartzJsonSerializer : SystemTextJsonObjectSerializer
{
    protected override JsonSerializerOptions CreateSerializerOptions()
    {
        var baseOptions = base.CreateSerializerOptions();
        
        baseOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);

        return baseOptions;
    }
}