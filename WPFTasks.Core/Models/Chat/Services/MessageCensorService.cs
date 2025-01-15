using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Chat.Services
{
    public class MessageCensorService
    {
        private Repository<string> _badWords;

        public MessageCensorService(string? filePath = null) 
        {
            _badWords = new(filePath ?? "MessageCensorService.json");
        }

        public void AddWord(string word)
        {
            _badWords.Add(word); 
        }

        public void RemoveWord(string word)
        {
            _badWords.Remove(word => word.Equals(word, StringComparison.CurrentCultureIgnoreCase));
        }

        public List<string> GetAllBadWords()
            => _badWords.GetAll();

        public string HandleMessage(string message, out bool containsBadWord)
        {
            containsBadWord = false;

            if (string.IsNullOrEmpty(message))
                return message;

            var badWords = _badWords.GetAll();
            var censoredMessage = message;

            foreach (var badWord in badWords)
            {
                var wordPattern = $@"\b{Regex.Escape(badWord)}\b"; // Используем регулярное выражение для точного совпадения слов
                if (Regex.IsMatch(censoredMessage, wordPattern, RegexOptions.IgnoreCase))
                {
                    containsBadWord = true;
                    censoredMessage = Regex.Replace(censoredMessage, wordPattern, "###", RegexOptions.IgnoreCase);
                }
            }

            return censoredMessage;
        }
    }
}
