using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Blob;
using Common;

namespace AzureRepositories
{
    public static class GeneralSettingsReader
    {
        public static T ReadGeneralSettings<T>(string connectionString)
        {
            var settingsStorage = new AzureBlobStorage(connectionString);
            var settingsData = settingsStorage.GetAsync("settings", "generalsettings.json").Result.ToBytes();
            var str = Encoding.UTF8.GetString(settingsData);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
        }
    }
}
