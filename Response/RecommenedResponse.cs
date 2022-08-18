using System.Xml.Serialization;

namespace ProxyAPI.Response
{
    [XmlRoot("Response")]
    [XmlType(AnonymousType = true)]
    public class RecommenedResponse : DefaultResponse
    {


        string _RecommendedMessage;
        public string RecommendedMessage { get { return _RecommendedMessage; } set { _RecommendedMessage = value; } }

        public RecommenedResponse()
        {
            _RecommendedMessage = "";
        }

        public RecommenedResponse(string messageCode, string message) : base(messageCode, message)
        {
            _RecommendedMessage = "";
        }
        public RecommenedResponse(string messageCode, string message, string recommendedMessage) : base(messageCode, message)
        {
            _RecommendedMessage = recommendedMessage;
        }

        public static new RecommenedResponse PathNotFound { get { return new RecommenedResponse("1001", "RequestPathNotFound", "Please check your url again."); } }
    }
}
