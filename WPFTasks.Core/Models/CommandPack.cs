
namespace WPFTasks.Core.Models
{
    public class CommandPack
    {
        public Dictionary<string, (string Description, Func<string, Task> Handler)> Commands { get; set; } = [];
        public CommandPack AddCommand(string command, string description, Func<string, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(handler);

            if (string.IsNullOrWhiteSpace(command) || !command.StartsWith('/'))
                throw new ArgumentException("Команда должна начинаться с '/' и не быть пустой.", nameof(command));

            if (!Commands.TryAdd(command, (description, handler)))
                throw new ArgumentException($"Команда '{command}' уже существует.");

            return this;
        }
    }
}
