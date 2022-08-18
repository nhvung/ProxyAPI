namespace ProxyAPI
{
    public class MessageResponse
    {
        public const string _ACCOUNT_NOT_FOUND = "Account not found";
        public const string _ACCOUNT_INACTIVED = "Account inactived";
        public const string _REQUEST_PARAMS_INVALID = "Request params invalid";
        public const string _TOKEN_INVALID = "Token invalid";


        string _Message;
        public string Message { get { return _Message; } set { _Message = value; } }
        public MessageResponse()
        {

        }
        public MessageResponse(string message)
        {
            _Message = message;
        }
    }
}
