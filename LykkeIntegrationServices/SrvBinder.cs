using Common.IocContainer;
using Core.LykkeIntegration.Services;

namespace LykkeIntegrationServices
{
    public static class SrvBinder
    {
        public static void BindLykkeServices(this IoC ioc)
        {
            ioc.RegisterSingleTone<IPreBroadcastHandler, PreBroadcastHandler>();
        }
    }
}
