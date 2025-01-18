# Как оно работает

## Client
Ему доступно несколько команд которые в комбинации дают всё что ему нужно.

#### Команды
* /Clear -> очистить чат
* /Send <ChatId: str> <Message: str> -> отправка сообщения в заданый чат
* /GotoChat <ChatId: str> -> Очистить чат, запрос на получение истории чата, если нету прав ErroreMessage
* /AddUser <ChatId: str> <Login: str> -> Добавить пользователя в заданый чат, если нету прав ErroreMessage
* /CreateChat <ChatId: str> -> Создать чат где вы админ
* /BanUser <ChatId: str> <Login: str> -> Забанить пользователя в заданом чате, если нету прав ErroreMessage
* /BanUser <ChatId: str> <Login: str> <Period: timeSpan> -> Забанить на определённое время, если нету прав ErroreMessage

## Server
Команды для администрирования сервером

#### Команды
* /AddBadWord <Word: str> -> добавить слово, которое будет цензуриться
* /GetUserInfo <Login: str> -> получить всю хранимую информацию про пользователя
* /SetMaxDurationInactive <Period: timeSpan> -> установить максимальное время бездействия, после которого отключаем пользователя
* /SetAuthSessionDuration <Period: timeSpan> -> установить максимальное время длительности сессии

