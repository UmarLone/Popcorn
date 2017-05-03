using System.Globalization;
using CookComputing.XmlRpc;
using Popcorn.OSDB.Backend;

namespace Popcorn.OSDB
{
    public class Osdb
    {
        private IOsdb _proxyInstance;

        private IOsdb Proxy
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

        public IAnonymousClient Login(string userAgent)
        {
            var systemLanguage = GetSystemLanguage();
            return Login(systemLanguage, userAgent);
        }

        private IAnonymousClient Login(string language, string userAgent)
        {
            var client = new AnonymousClient(Proxy);
            client.Login(string.Empty, string.Empty, language, userAgent);
            return client;
        }

        private string GetSystemLanguage()
        {
            var currentCulture = CultureInfo.CurrentUICulture;
            return currentCulture.TwoLetterISOLanguageName.ToLower(CultureInfo.InvariantCulture);
        }
    }
}