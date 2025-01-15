
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace WPFTasks.Core.Models.Chat.Services
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Sender { get; set; } // Отправитель сообщения
        public string ChatName { get; set; } // Название чата
        public string Content { get; set; } // Содержимое сообщения
    }

    public class ChatDbContext : DbContext
    {
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatMessage>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Sender)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.ChatName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Content)
                .IsRequired();
        }

        public async Task<List<ChatMessage>> GetMessagesByChatAsync(string chatName)
        {
            return await ChatMessages
                .Where(m => m.ChatName == chatName)
                .ToListAsync();
        }
    }

    public class DbService
    {
        private static readonly DbContextOptions<ChatDbContext> _options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseSqlServer("Server=localhost;Database=ChatAppDb;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

        private static SemaphoreSlim _semaphore = new(1, 1);

        public async Task<List<ChatMessage>> GetMessagesByChatAsync(string chatName)
        {
            if (string.IsNullOrWhiteSpace(chatName))
            {
                throw new ArgumentException("Chat name cannot be null or empty", nameof(chatName));
            }

            await _semaphore.WaitAsync();
            try
            {
                using var context = new ChatDbContext(_options);
                return await context.GetMessagesByChatAsync(chatName);
            }
            finally { _semaphore.Release(); }
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _semaphore.WaitAsync();
            try
            {
                using var context = new ChatDbContext(_options);
                await context.ChatMessages.AddAsync(message);
                await context.SaveChangesAsync();
            }
            finally { _semaphore.Release(); }
        }

        public async Task AddMessagesAsync(ChatMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                throw new ArgumentException("Messages cannot be null or empty", nameof(messages));
            }

            await _semaphore.WaitAsync();
            try
            {
                using var context = new ChatDbContext(_options);
                await context.ChatMessages.AddRangeAsync(messages);
                await context.SaveChangesAsync();
            }
            finally { _semaphore.Release(); }
        }
    }
}
