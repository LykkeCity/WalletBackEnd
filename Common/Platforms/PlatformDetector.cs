using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Platforms

{

    public enum FormFactor
    {
        Unknown, Desktop, Mobile, Tablet
    }

    public enum PlarformType
    {
        Unknown, Windows,
        Android,
        Apple,
        Linux,
        Symbian,
        BlackBerry,


    }

    public enum ApplicationHost
    {
        Unknown, Native, Opera, Chrome, Ie, Firefox, Safari, UcBrowser, Android, WeChat
    }

    public enum ArchitectureType
    {
        Unknown, X86, X64
    }


    public class DeviceInfo
    {
        public FormFactor FormFactor { get; set; }
        public PlarformType PlarformType { get; set; }
        public ApplicationHost AppHost { get; set; }
        public string OsVersion { get; set; }
        public string AppVersion { get; set; }
        public ArchitectureType Architecture { get; set; }

    }

    public static class PlatformDetector
    {

        public static string GetAndoidVersionName(this string version)
        {
            if (version.StartsWith("1.5"))
                return "Cupcake";

            if (version.StartsWith("1.6"))
                return "Donut";

            if (version.StartsWith("2.0") || version.StartsWith("2.1"))
                return "Eclair";

            if (version.StartsWith("2.2"))
                return "Froyo";


            if (version.StartsWith("2.3"))
                return "Gingerbread";

            if (version.StartsWith("2.3"))
                return "Gingerbread";


            if (version.StartsWith("3"))
                return "Honeycomb";


            if (version.StartsWith("4.0"))
                return "IceCreamSandwich";

            if (version.StartsWith("4.1") || version.StartsWith("4.2") || version.StartsWith("4.3"))
                return "JellyBean";

            if (version.StartsWith("4.4"))
                return "KitKat";


            if (version.StartsWith("5"))
                return "Lollipop";

            return null;
        }


        private static readonly DeviceInfo UnknownPlatform = new DeviceInfo
        {
            FormFactor = FormFactor.Unknown,
            PlarformType = PlarformType.Unknown,
            AppHost = ApplicationHost.Unknown
        };

        private static string GetProductLine(string userAgent)
        {
            var index = userAgent.IndexOf('/');

            if (index < 0)
                return null;

            return userAgent.Substring(0, index);
        }




        private static string WindowsOsNumberToName(string version)
        {
            return WindowsVersions.GetExtendedInfo(version);
        }


        private static DeviceInfo ParseMozillaWindows(string userAgent, string[] platformInfo)
        {

            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Windows,
                Architecture = platformInfo.Any(itm => itm == "WOW64") ? ArchitectureType.X64 : ArchitectureType.X86,
                FormFactor = FormFactor.Desktop,
                OsVersion = WindowsOsNumberToName(platformInfo.First(itm => itm.StartsWith("Windows NT")).SubstringFromString("Windows NT "))
            };


            if (platformInfo.Any(itm => itm.StartsWith("Trident")) || platformInfo.Any(itm => itm.StartsWith(".NET")) || platformInfo.Any(itm => itm.StartsWith("MSIE")))
            {
                var ieData = platformInfo.FirstOrDefault(itm => itm.StartsWith("MSIE"));
                if (ieData != null)
                {
                    result.AppHost = ApplicationHost.Ie;
                    result.AppVersion = ieData.SubstringFromString("MSIE ");
                    return result;
                }

                ieData = platformInfo.FirstOrDefault(itm => itm.StartsWith("rv:"));

                if (ieData != null)
                {
                    result.AppHost = ApplicationHost.Ie;
                    result.AppVersion = ieData.SubstringFromString("rv:");
                    return result;
                }

            }




            var browserDataString = userAgent.SubstringFromChar(')', 1);

            if (browserDataString == null)
            {
                browserDataString = userAgent.SubstringFromChar(')');

                var bd = browserDataString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var bi = bd.FirstOrDefault(itm => itm.StartsWith("Firefox"));
                if (bi != null)
                {
                    result.AppHost = ApplicationHost.Firefox;
                    result.AppVersion = bi.SubstringFromChar('/');
                    return result;
                }

                return UnknownPlatform;
            }


            var browserData = browserDataString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("OPR"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Opera;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Chrome"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Chrome;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }



            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Version"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Safari;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }
            

            return result;
        }


        private static DeviceInfo ParseIphoneIPad(string userAgent, string[] platformInfo, FormFactor formFactor)
        {
            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Apple,
                FormFactor = formFactor,
                OsVersion = null
            };

            var browserData = userAgent.SubstringFromChar(')', 1)
   .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("CriOS"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Chrome;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("OPiOS"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Opera;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }



            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("UCBrowser"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.UcBrowser;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("MicroMessenger"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.WeChat;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }
            
            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Version"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Safari;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }


            return result;

        }

        private static DeviceInfo ParseAndroid(string userAgent, string[] platformInfo)
        {
            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Android,
                FormFactor = FormFactor.Mobile,
                OsVersion = WindowsOsNumberToName(platformInfo.First(itm => itm.StartsWith("Android")).SubstringFromString("Android "))
            };

            var browserDataString = userAgent.SubstringFromChar(')', 1);


            if (browserDataString == null)
            {
                browserDataString = userAgent.SubstringFromChar(')');

                var bd = browserDataString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var bi = bd.FirstOrDefault(itm => itm.StartsWith("Firefox"));
                if (bi != null)
                {
                    result.AppHost = ApplicationHost.Firefox;
                    result.AppVersion = bi.SubstringFromChar('/');
                    return result;
                }

                return UnknownPlatform;
            }



            var browserData = browserDataString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Chrome"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Chrome;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Version"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Android;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            return result;

        }

        private static DeviceInfo ParseWindowsPhone(string userAgent, string[] platformInfo)
        {
            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Windows,
                FormFactor = FormFactor.Mobile,
                OsVersion = WindowsOsNumberToName(platformInfo.First(itm => itm.StartsWith("Windows Phone")).SubstringFromString("Windows Phone "))
            };

            var ieData = platformInfo.FirstOrDefault(itm => itm.StartsWith("IEMobile"));
            if (ieData != null)
            {
                result.AppHost = ApplicationHost.Ie;
                result.AppVersion = ieData.SubstringFromString("IEMobile/");
                return result;
            }


            return result;

        }

        private static DeviceInfo ParseMacintosh(string userAgent, string[] platformInfo)
        {
            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Apple,
                FormFactor = FormFactor.Desktop,
                OsVersion = WindowsOsNumberToName(platformInfo.FirstOrDefault(itm => itm.Contains("Mac OS X")))
            };



            var browserData = userAgent.SubstringFromChar(')', 1)
               .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Chrome"));

            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Chrome;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Version"));

            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Safari;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            return result;

        }

        private static DeviceInfo ParseNative(string userAgent)
        {
            var platformInfo = userAgent.SubstringBetween('(', ')').Split(';').Select(itm => itm.Trim()).ToArray();

            var result = new DeviceInfo
            {
                FormFactor = FormFactor.Mobile,
                AppHost = ApplicationHost.Native
            };


            if (platformInfo.Any(itm => itm.StartsWith("WinPhone")))
            {  
               result.PlarformType = PlarformType.Windows; 
            }
            else if (platformInfo.Any(itm => itm.StartsWith("Android")))
            {
                result.PlarformType = PlarformType.Android;
            }

            return result;

        }

        private static DeviceInfo ParseAndroidTablet(string userAgent, string[] platformInfo)
        {
            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Android,
                FormFactor = FormFactor.Tablet,
                OsVersion = null
            };

            var browserDataString = userAgent.SubstringFromChar(')', 1);


            if (browserDataString == null)
            {
                browserDataString = userAgent.SubstringFromChar(')');

                var bd = browserDataString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var bi = bd.FirstOrDefault(itm => itm.StartsWith("Firefox"));
                if (bi != null)
                {
                    result.AppHost = ApplicationHost.Firefox;
                    result.AppVersion = bi.SubstringFromChar('/');
                    return result;
                }

                return UnknownPlatform;
            }



            var browserData = browserDataString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Chrome"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Chrome;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            browserInfo = browserData.FirstOrDefault(itm => itm.StartsWith("Version"));
            if (browserInfo != null)
            {
                result.AppHost = ApplicationHost.Android;
                result.AppVersion = browserInfo.SubstringFromChar('/');
                return result;
            }

            return result;
        }


        private static DeviceInfo ParseMozilla(string userAgent)
        {
            try
            {
                var platformInfo = userAgent.SubstringBetween('(', ')').Split(';').Select(itm => itm.Trim()).ToArray();

                if (platformInfo.Any(itm => itm.StartsWith("Windows NT")))
                    return ParseMozillaWindows(userAgent, platformInfo);

                if (platformInfo.Any(itm => itm.StartsWith("iPhone")))
                    return ParseIphoneIPad(userAgent, platformInfo, FormFactor.Mobile);

                if (platformInfo.Any(itm => itm.StartsWith("iPad")))
                    return ParseIphoneIPad(userAgent, platformInfo, FormFactor.Tablet);

                if (platformInfo.Any(itm => itm.StartsWith("Windows Phone")))
                    return ParseWindowsPhone(userAgent, platformInfo);

                if (platformInfo.Any(itm => itm.StartsWith("Android")) && platformInfo.Any(itm => itm.StartsWith("Linux")))
                    return ParseAndroid(userAgent, platformInfo);

                if (platformInfo.Any(itm => itm.StartsWith("Android")) && platformInfo.Any(itm => itm.StartsWith("Tablet")))
                    return ParseAndroidTablet(userAgent, platformInfo);

                if (platformInfo.Any(itm => itm.StartsWith("Macintosh")))
                    return ParseMacintosh(userAgent, platformInfo);

            }
            catch (Exception)
            {
                return UnknownPlatform;
            }

            return UnknownPlatform;
        }








        private static DeviceInfo ParseLG_P503(string userAgent)
        {

            var data = userAgent.Split(' ');

            var result = new DeviceInfo
            {
                PlarformType = PlarformType.Android,
                FormFactor = FormFactor.Mobile,
                AppHost = ApplicationHost.Android,
            };


            var androidData = data.FirstOrDefault(itm => itm.StartsWith("Android"));
            if (androidData != null)
            {
                result.OsVersion = androidData.SubstringFromString("Android/");
            }

            return result;

        }

        public static DeviceInfo DetectPlatformByUserAgent(this string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return UnknownPlatform;

            var productLine = GetProductLine(userAgent);

            if (productLine == null)
                return UnknownPlatform;


            if (productLine == "Mozilla")
                return ParseMozilla(userAgent);

            if (productLine == "Opera")
                return userAgent.ParseOpera();

            if (productLine == "ZebraFx.Mobile")
                return ParseNative(userAgent);

            if (productLine == "LG_P503")
                return ParseLG_P503(userAgent);

            return UnknownPlatform;

        }

        public static bool IsIphone(this DeviceInfo src)
        {
            return src.PlarformType == PlarformType.Apple && src.FormFactor == FormFactor.Mobile;
        }

    }

}
