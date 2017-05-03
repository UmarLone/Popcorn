using CookComputing.XmlRpc;
using Popcorn.OSDB.Backend;

namespace Popcorn.OSDB
{
    public static class Osdb
    {
        private static IOsdb _proxyInstance;

        private static IOsdb Proxy
        {
            get
            {
                if (_proxyInstance == null)
                {
                    _proxyInstance = XmlRpcProxyGen.Create<IOsdb>();
                }
                return _proxyInstance;
            }
        }

        public static IAnonymousClient Login(string userAgent)
        {
            var systemLanguage = GetSystemLanguage();
            return Login(systemLanguage, userAgent);
        }

        private static IAnonymousClient Login(string language, string userAgent)
        {
            var client = new AnonymousClient(Proxy);
            client.Login(string.Empty, string.Empty, language, userAgent);
            return client;
        }

        private static string GetSystemLanguage()
        {
            var currentCulture = System.Globalization.CultureInfo.CurrentUICulture;
            return currentCulture.TwoLetterISOLanguageName.ToLower();
        }
    }
}