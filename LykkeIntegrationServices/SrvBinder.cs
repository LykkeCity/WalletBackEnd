using Common.IocContainer;
using Core.LykkeIntegration.Services;

namespace LykkeIntegrationServices
{
    public static class SrvBinder
    {
        public static void BindLykkeServices(this IoC ioc, bool useMockAsLykkeNotification)
        {
            if (useMockAsLykkeNotification)
            {
                ioc.RegisterSingleTone<IPreBroadcastHandler, MockBroadcastHandler>();
            }
            else
            {
                ioc.RegisterSingleTone<IPreBroadcastHandler, PreBroadcastHandler>();
            }
        }
    }
}
