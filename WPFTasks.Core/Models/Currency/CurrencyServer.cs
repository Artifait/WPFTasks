
using System.Net;
using TopNetwork.Core;

using MsgT = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder.Types;
using MsgH = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder.Headers;
using MsgBuilder = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyServer
    {
        private static readonly Func<MsgT, string> GetMsgTStr = MsgBuilder.GetMessageTypeStr; 
        public RequestResponseServer Server { get; set; }
        public CurrencyStatus Status
        {
            get => (CurrencyStatus)Server.Status;
        }
        public Logger Logger { get; private set; } = new();
        public UserManager UserManager { get; set; }
        public CurrencyConverter Converter { get; private set; }

        public CurrencyServer(IPAddress ip, int port)
        {
            UserManager = new UserManager("user_credentials.json") { Logger = Logger.Log };
            UserManager.LoadCredentials();
            Converter = new(Logger.Log);                    

            Server = new RequestResponseServer
            {
                Logger = Logger.Log,
                ShouldAcceptClient = async client =>
                {
                    Logger.Log("Checking if client should be accepted...");
                    return client != null && Status.ActiveConnections < Status.MaxActiveConnection;
                }
            };

            Server.ClientConnected += client => Status.ActiveConnections++;
            Server.ClientDisconnected += client => Status.ActiveConnections--;

            Server.Init(ip, port);
            Server.Status = new CurrencyStatus();   

            Server.ServerHandlers.AddHandlerForMessageType(
                GetMsgTStr(MsgT.AuthenticationRequest),
                UserManager.HandleAuthenticationRequest
            );

            Server.ServerHandlers.AddHandlerForMessageType(
                GetMsgTStr(MsgT.CloseSessionRequest),
                UserManager.HandleCloseSessionRequest
            );

            Server.ServerHandlers.AddHandlerForMessageType(
                GetMsgTStr(MsgT.CurrencyRateRequest),
                CurrencyConversionHandler
            );

            Server.ServerHandlers.SetDefaultHandler(async (client, message) =>
            {
                Logger.Log("What!");
                return MsgBuilder.CreateErroreMsg(payload: "Мы не смогли обработать ваш запрос...");
            });
        }

        public void Start()
        {
            Server.Start();
            Logger.Log("Server started!");
        }
        #region MainHandler
        private async Task<Message?> CurrencyConversionHandler(TopClient client, Message message)
        {
            if (!await UserManager.VerifyAuthenticatedConnection(client))
                return null;

            try
            {
                string fromCurrency = message.Headers[MsgBuilder.GetHeaderStr(MsgH.FromCurrency)];
                string toCurrency = message.Headers[MsgBuilder.GetHeaderStr(MsgH.ToCurrency)];
                double? exchangeRate = await Converter.GetExchangeRate(fromCurrency, toCurrency);

                return MsgBuilder.CreateCurrencyRateResult(fromCurrency, toCurrency, exchangeRate);
            }
            catch (Exception ex)
            {
                return MsgBuilder.CreateErroreMsg(payload: ex.Message);
            }
        }
        #endregion
    }
}
