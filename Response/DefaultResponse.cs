using System.Net;
using System.Xml.Serialization;

namespace ProxyAPI.Response
{
    [XmlRoot("Response")]
    [XmlType(AnonymousType = true)]
    public class DefaultResponse
    {

        string _MessageCode;
        public string MessageCode { get { return _MessageCode; } set { _MessageCode = value; } }

        string _Message;
        public string Message { get { return _Message; } set { _Message = value; } }
        public DefaultResponse()
        {
            _MessageCode = "0";
            _Message = "OK";
        }
        public DefaultResponse(string messageCode, string message)
        {
            _MessageCode = messageCode;
            _Message = message;
        }
        public DefaultResponse(int messageCode, string message)
        {
            _MessageCode = messageCode.ToString();
            _Message = message;
        }
        public DefaultResponse(HttpStatusCode messageCode, string message)
        {
            _MessageCode = ((int)messageCode).ToString();
            _Message = message;
        }
        public static DefaultResponse Hello { get { return new DefaultResponse("0001", "Hi, how are you today?"); } }
        public static DefaultResponse Success { get { return new DefaultResponse("0002", "Success"); } }
        public static DefaultResponse PathNotFound { get { return new DefaultResponse("1001", "Request Path Not Found"); } }
        public static DefaultResponse NoRecordFound { get { return new DefaultResponse("1002", "No Record Found"); } }
        public static DefaultResponse MethodNotAllowed { get { return new DefaultResponse("1003", "Method Not Allowed"); } }
        public static DefaultResponse InvalidParameters { get { return new DefaultResponse("1004", "Invalid Parameters"); } }
        public static DefaultResponse ServerError { get { return new DefaultResponse("1008", "Server Error"); } }
    }

    public class DefaultResponse<TResult> : DefaultResponse
    {

        int _TotalRecords;
        public int TotalRecords { get { return _TotalRecords; } set { _TotalRecords = value; } }

        double _ProcessTime;
        public double ProcessTime { get { return _ProcessTime; } set { _ProcessTime = value; } }

        TResult _Result;
        public TResult Result { get { return _Result; } set { _Result = value; } }
        public DefaultResponse() : base()
        {
            _TotalRecords = 0;
            _ProcessTime = 0;
            _Result = default;
        }
        public DefaultResponse(string messageCode, string message) : base(messageCode, message)
        {
            _TotalRecords = 0;
            _ProcessTime = 0;
            _Result = default;
        }
    }
}
