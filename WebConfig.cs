using VSSystem.Logger;

namespace ProxyAPI
{
    public class WebConfig
    {

        static string _ApiUrl;
        public static string ApiUrl { get { return _ApiUrl; } set { _ApiUrl = value; } }

        static int _DefaultTimeout = 100;
        public static int DefaultTimeout { get { return _DefaultTimeout; } set { _DefaultTimeout = value; } }
        static ALogger _Logger;
        public static ALogger Logger { get { return _Logger; } set { _Logger = value; } }

        static string[] _TimeHeaders;
        public static string[] TimeHeaders { get { return _TimeHeaders; } set { _TimeHeaders = value; } }

        #region web

        static int _web_max_concurrent_connections = 100;
        public static int web_max_concurrent_connections { get { return _web_max_concurrent_connections; } set { _web_max_concurrent_connections = value; } }

        static string _web_component_name = "ProxyAPI";
        public static string web_component_name { get { return _web_component_name; } set { _web_component_name = value; } }

        #endregion
    }
}
