using System.Linq;

namespace Common.Platforms
{
    public static class DetectOpera
    {
        //"Opera/9.80 (Windows NT 5.1) Presto/2.12.388 Version/12.14";
        //Opera/9.80 (Series 60; Opera Mini/7.1.32444/36.2639; U; en) Presto/2.12.423 Version/12.16


        //User-Agent: Opera/9.80 ($PLATFORM_NAME$; $PRODUCT_NAME$/$CLIENT_VERSION$/ $SERVER_VERSION$;U; $LOCALE$) $PRESTO_VERSION$ $EQUIV_DESKTOP_VERSION$


         public static DeviceInfo ParseOpera(this string userAgent)
         {

             FormFactor formFactor;
             PlarformType platformType;
             string osVersion;
             userAgent.DetectPlatfrom(out formFactor, out platformType, out osVersion);

            var result = new DeviceInfo
            {
                Architecture = ArchitectureType.Unknown,
                AppHost = ApplicationHost.Opera,
                OsVersion = osVersion,
                AppVersion = userAgent.SubstringFromString("Version/"),
                FormFactor = formFactor,
                PlarformType = platformType
            };

            return result;
        }

         private static string DetectOperaOsVersion(this string userAgent)
         {

             var theInfo = userAgent.SubstringBetween('(', ')').Split(';');

             return (from s in theInfo where s.StartsWith("Windows NT") select s.SubstringFromString("Windows NT")).FirstOrDefault();
         }

         private static void DetectPlatfrom(this string userAgent, out FormFactor formFactor, out PlarformType plarformType, out string osVersion)
         {
             var theData = userAgent.ToLower().SubstringBetween('(', ')').Split(';');


             foreach (var s in theData)
             {
                 if (s.StartsWith("series 60"))
                 {
                     plarformType = PlarformType.Symbian;
                     formFactor = FormFactor.Mobile;
                     osVersion = null;
                     return;
                 }


                 if (s.StartsWith("android"))
                 {
                     plarformType = PlarformType.Android;
                     formFactor = FormFactor.Mobile;
                     osVersion = null;
                     return;
                 }

                 if (s.StartsWith("blackberry"))
                 {
                     plarformType = PlarformType.BlackBerry;
                     formFactor = FormFactor.Mobile;
                     osVersion = null;
                     return;
                 }

                 if (s.StartsWith("iphone"))
                 {
                     plarformType = PlarformType.Apple;
                     formFactor = FormFactor.Mobile;
                     osVersion = null;
                     return;
                 }

                 if (s.StartsWith("ipad"))
                 {
                     plarformType = PlarformType.Apple;
                     formFactor = FormFactor.Tablet;
                     osVersion = null;
                     return;
                 }

                 if (s.StartsWith("windows nt"))
                 {
                     plarformType = PlarformType.Windows;
                     formFactor = FormFactor.Desktop;
                     osVersion = WindowsVersions.GetExtendedInfo(s.SubstringFromString("windows nt").Trim());
                     return;
                 }


                 if (s.StartsWith("windows mobile"))
                 {
                     plarformType = PlarformType.Windows;
                     formFactor = FormFactor.Mobile;
                     osVersion = null;
                     return;
                 }
             }

             plarformType = PlarformType.Unknown;
             formFactor = FormFactor.Unknown;
             osVersion = null;
         }




    }
}
