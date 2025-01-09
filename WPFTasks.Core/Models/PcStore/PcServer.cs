
using System.Net;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Currency;
using WPFTasks.Core.Models.Currency.MessageBuilder;
using WPFTasks.Core.Models.PcStore.MessageBuilder;
using WPFTasks.Core.Models.PcStore.Services;

namespace WPFTasks.Core.Models.PcStore
{
    public class PcServer
    {
        // Регистрация всех фабрик для типов сообщений отправляемых сервером 
        private static readonly MessageBuilderService _msgService = new MessageBuilderService()
                    .Register(() => new AuthenticationResponseMessageBuilder())
                    .Register(() => new PcPartInfoResponseMessageBuilder())
                    .Register(() => new ErroreMessageBuilder())
                    .Register(() => new EndSessionNotificationMessageBuilder())
                    .Register(() => new ServerOverloadedNotificationMessageBuilder());

        private readonly AuthenticationService<PcUser> _authenticationService;
        private readonly Repository<PcUser> _userRepository;
        private readonly UserService<PcUser> _userService;
        private readonly TrackerUserActivityService _activityService;
        private readonly Repository<PcPart> _pcPartRepository;
        private readonly PcPartsService _pcPartsService;
        private readonly RrServerHandlerBase _handlers;
        private RrServer _server = new();

        public Logger Logger { get; private set; } = new();
        public EndPoint? EndPoint => _server.CurrentEndPoint;
        public bool IsRunning => _server.IsRunning;
        public int CountOpenSessions => _server.CountOpenSessions;

        public PcServer(string? userFilePath = null, string? pcPartsFilePath = null)
        {
            _server.Logger = Logger.LogString;

            _userRepository = new(userFilePath ?? "PcUsers.json");
            _userService = new(_userRepository, new PasswordService(), data => new(data.login, data.hashPassword));
            _authenticationService = new(_userService, _msgService) { Logger = Logger.LogString };
            _activityService = new(_msgService);
            _pcPartRepository = new Repository<PcPart>(pcPartsFilePath ?? "PcParts.json");
            _pcPartsService = new(_pcPartRepository);

            if(!_pcPartsService.GetAllParts().Any())
            {
                _pcPartsService
                    .RegisterPart("Intel Core i7-13700K Processor", 400)
                    .RegisterPart("AMD Ryzen 7 7800X3D Processor", 450)
                    .RegisterPart("NVIDIA GeForce RTX 4090 Graphics Card", 1600)
                    .RegisterPart("AMD Radeon RX 7900 XT Graphics Card", 900)
                    .RegisterPart("Corsair Vengeance RGB 32GB DDR5 RAM", 180)
                    .RegisterPart("Kingston Fury Beast 16GB DDR4 RAM", 75)
                    .RegisterPart("Samsung 980 Pro 1TB NVMe SSD", 120)
                    .RegisterPart("Western Digital Black 2TB HDD", 100)
                    .RegisterPart("MSI MPG B650 TOMAHAWK WiFi Motherboard", 200)
                    .RegisterPart("ASUS ROG STRIX Z790-E Gaming Motherboard", 450)
                    .RegisterPart("Corsair RM850x 850W Power Supply", 150)
                    .RegisterPart("Cooler Master MasterBox TD500 Mesh Case", 100)
                    .RegisterPart("Noctua NH-D15 CPU Cooler", 100)
                    .RegisterPart("Arctic MX-4 Thermal Paste", 10)
                    .RegisterPart("Logitech MX Master 3S Wireless Mouse", 100)
                    .RegisterPart("Razer Huntsman V2 Gaming Keyboard", 200)
                    .RegisterPart("Dell UltraSharp U2723QE 27-inch Monitor", 650)
                    .RegisterPart("LG UltraGear 27GP850-B Gaming Monitor", 450)
                    .RegisterPart("Elgato Wave:3 USB Microphone", 150)
                    .RegisterPart("HyperX Cloud II Gaming Headset", 100);
            }

            _server
                .RegisterService(_msgService)
                .RegisterService(_userRepository)
                .RegisterService(_userService)
                .RegisterService(_authenticationService)
                .RegisterService(_activityService);

            _handlers = new RrServerHandlerBase()
                .AddHandlerForMessageType(AuthenticationRequestData.MsgType, async (client, msg, context) =>
                {
                    try
                    {
                        var requestData = AuthenticationRequestMessageBuilder.Parse(msg);
                        return await _authenticationService.AuthenticateClient(client, requestData);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogString($"[Server]: Ошибка обработки {AuthenticationRequestData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                            .SetPayload($"Невозможно обработать {AuthenticationRequestData.MsgType}.\n{ex.Message}")
                        );
                    }
                })
                .AddHandlerForMessageType(CloseSessionRequestData.MsgType, async (client, msg, context) =>
                {
                    _authenticationService.CloseSession(client);
                    return _msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>();
                })
                .AddHandlerForMessageType(PcPartInfoRequestData.MsgType, async (client, msg, context) =>
                {
                    try
                    {
                        if (!_authenticationService.IsAuthClient(client))
                        {
                            return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                .SetPayload("Для использования данной функции нужно авторизироваться...")
                            );
                        }

                        var requestData = PcPartInfoRequestMessageBuilder.Parse(msg);
                        var pcPart = _pcPartsService.SearchPcPart(requestData.Title);
                        var response = _msgService.BuildMessage<PcPartInfoResponseMessageBuilder, PcPartInfoResponseData> (builder => builder
                            .SetTitle(pcPart?.Title ?? "Не найдено")
                            .SetPrice(pcPart?.Price ?? -1)
                        );

                        var user = _authenticationService.GetUserBy(client);
                        user.AddPcInfoRequest();
                        _userService.UpdateUser(user);

                        return response;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogString($"[Server]: Ошибка обработки {PcPartInfoRequestData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                            .SetPayload($"Невозможно обработать {PcPartInfoRequestData.MsgType}.\n{ex.Message}")
                        );
                    }
                });

            _server.SetSessionFactory(SessionFactory);
        }


        public void SetEndPoint(IPEndPoint endPoint)
            => _server.SetEndPoint(endPoint);

        public async Task StartServer(CancellationToken token = default)
            => await _server.StartAsync(token);

        public async Task StopServer()
            => await _server.StopAsync();

        private async Task<ClientSession?> SessionFactory(TopClient client, ServiceRegistry context, LogString? logger)
        {

            if (_server.CountOpenSessions >= MaxConnections)
            {
                try
                {
                    client.SendMessageAsync(_msgService.BuildMessage<ServerOverloadedNotificationMessageBuilder, ServerOverloadedNotificationData>(null)).Wait();
                    logger?.Invoke($"[SessionFactory]: Отвергнуто подключение с [{client.RemoteEndPoint}], из-за перегрузки сервера...");
                    return null;
                }
                catch (Exception ex)
                {
                    logger?.Invoke($"[SessionFactory]: {ex.Message}.");
                }
            }

            ClientSession session = new(client, _handlers, context)
            {
                logger = logger,
            };

            session.OnMessageProcessed += Session_OnMessageProcessed;
            session.OnMessageHandled += Session_OnMessageHandled;
            _activityService.UpdateLastActive(client);

            return session;
        }

        private void Session_OnMessageHandled(ClientSession arg1, Message arg2)
        {
            try { 
                _activityService.UpdateLastActive(arg1.Client);
                _server?.Logger?.Invoke($"[Server]: Обработано сообщение типа [{arg2.MessageType}] от [{arg1.RemoteEndPoint}] ");
            } 
            catch { }
        }

        private void Session_OnMessageProcessed(ClientSession arg1, Message arg2)
        {
            if (arg2.MessageType == PcPartInfoResponseData.MsgType)
            {
                if (!_authenticationService.GetUserBy(arg1.Client).IsUserLoginPossibleAsync().Result)
                {
                    arg1.SendMessage(_msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>(builder => builder
                        .SetPayload($"Вы сделали максимальное количество запросов...\nЧерез {CurrencyUser.Cooldown.TotalMinutes} минут вы снова сможете отправлять запросы."))).Wait();
                    _authenticationService.CloseSession(arg1.Client);
                    arg1.CloseSession();
                }
            }
        }

        public void RegisterUser(string login, string password)
            => _userService.RegisterUser(login, password);

        // Свойства Задаваемые юзером
        public string UserFilePath => _userRepository.FilePath;
        public string PcPartFilePath => _pcPartRepository.FilePath;

        public TimeSpan MaxAuthSessionDuration => _authenticationService.MaxSessionDuration;
        public async Task UpdateAuthSessionDuration(TimeSpan newDuration)
            => await _authenticationService.UpdateSessionDuration(newDuration);

        public TimeSpan MaxDurationInactive => _activityService.MaxDurationInactive;
        public async Task UpdateMaxDurationInactive(TimeSpan newDuration)
            => await _activityService.UpdateMaxDurationInactive(newDuration);

        public int MaxConnections { get; set; } = 3;
        public bool SetIndividMaxRequests(string login, int newCount)
        {
            var user = _userRepository.Get(user => user.Login == login);
            if (user != null)
            {
                user.MaxRequests = newCount;
                _userService.UpdateUser(user);
            }

            return user != null;
        }

        public bool SetIndividTimeWindow(string login, TimeSpan newTimeWindow)
        {
            var user = _userRepository.Get(user => user.Login == login);
            if (user != null)
            {
                user.TimeWindow = newTimeWindow;
                _userService.UpdateUser(user);
            }

            return user != null;
        }
    }
}
