using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace PongBackend;

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
    }
}
