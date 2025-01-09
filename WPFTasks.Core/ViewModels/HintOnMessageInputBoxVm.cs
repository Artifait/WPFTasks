
using System.Collections.ObjectModel;
using WPFTasks.Core.Models;

namespace WPFTasks.Core.ViewModels
{
    public class HintOnMessageInputBoxBaseVm : BaseViewModel
    {
        private string _newMessage;
        private bool _areHintsVisible;
        private ObservableCollection<string> _hints = [];
        protected readonly ChatCommandProcessor _commandProcessor;

        public HintOnMessageInputBoxBaseVm()
        {
            _commandProcessor = new();
        }

        public string NewMessage
        {
            get => _newMessage;
            set
            {
                _newMessage = value;
                UpdateHints();
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        public bool AreHintsVisible
        {
            get => _areHintsVisible;
            set => SetProperty(ref _areHintsVisible, value);
        }

        public ObservableCollection<string> Hints
        {
            get => _hints;
            set => SetProperty(ref _hints, value);
        }

        public void SelectHint(string hint)
        {
            if (!string.IsNullOrWhiteSpace(hint))
            {
                NewMessage = hint;
            }
        }

        public string TryCompleteCommand(string text)
        {
            if (_commandProcessor.TryCompleteCommand(text, out var completedCommand))
                return completedCommand;

            return text;
        }

        private void UpdateHints()
        {
            int index = NewMessage.LastIndexOf('/');
            if (index == -1) return;
            string text = NewMessage[index..];
            _commandProcessor.GetHints(text, Hints);
            AreHintsVisible = Hints.Any();
        }
    }
}
