
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Input;
using Timer = System.Timers.Timer;

namespace WPFTasks.ViewModels
{
    public class NokiaKeyboardViewModel : INotifyPropertyChanged
    {
        private string _outputText = "";
        private int _cursorPosition = 0;
        private string _currentCharacterGroup = "";
        private int _currentCharacterIndex = 0;
        private Timer _multiPressTimer;
        private InputMode _inputMode = InputMode.Text;

        public event PropertyChangedEventHandler PropertyChanged;

        public NokiaKeyboardViewModel()
        {
            _multiPressTimer = new Timer(1000); 
            _multiPressTimer.Elapsed += OnMultiPressTimerElapsed;
        }
        public string OutputText
        {
            get => _outputText;
            set
            {
                if (_outputText != value)
                {
                    _outputText = value;
                    OnPropertyChanged(nameof(OutputText));
                }
            }
        }

        public int CursorPosition
        {
            get => _cursorPosition;
            private set
            {
                _cursorPosition = value;
                OnPropertyChanged();
            }
        }

        public InputMode CurrentInputMode
        {
            get => _inputMode;
            set
            {
                _inputMode = value;
                OnPropertyChanged();
            }
        }

        public ICommand KeyPressCommand => new RelayCommand<string>(OnKeyPress);
        public ICommand MoveCursorLeftCommand => new RelayCommand(MoveCursorLeft, CanMoveCursorLeft);
        public ICommand MoveCursorRightCommand => new RelayCommand(MoveCursorRight, CanMoveCursorRight);

        private void OnKeyPress(string key)
        {
            switch (key)
            {
                case "2": _currentCharacterGroup = "ABC"; break;
                case "3": _currentCharacterGroup = "DEF"; break;
                case "4": _currentCharacterGroup = "GHI"; break;
            }

            _currentCharacterIndex = (_currentCharacterIndex + 1) % _currentCharacterGroup.Length;
            var selectedChar = _currentCharacterGroup[_currentCharacterIndex];

            InsertCharacter(selectedChar.ToString());

            _multiPressTimer.Stop();
            _multiPressTimer.Start();
        }

        private void OnMultiPressTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _multiPressTimer.Stop();
            _currentCharacterIndex = 0;
        }

        private void InsertCharacter(string character)
        {
            OutputText = OutputText.Insert(CursorPosition, character);
            CursorPosition++;
        }

        private void MoveCursorLeft()
        {
            CursorPosition--;
        }

        private void MoveCursorRight()
        {
            CursorPosition++;
        }

        private bool CanMoveCursorLeft() => CursorPosition > 0;
        private bool CanMoveCursorRight() => CursorPosition < OutputText.Length;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum InputMode
    {
        Text,
        Number,
        Symbol
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        private readonly Action<object> _executeWithParam;
        private readonly Func<object, bool> _canExecuteWithParam;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _executeWithParam = execute;
            _canExecuteWithParam = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (_canExecuteWithParam != null) return _canExecuteWithParam(parameter);
            if (_canExecute != null) return _canExecute();
            return true;
        }

        public void Execute(object parameter)
        {
            if (_executeWithParam != null) _executeWithParam(parameter);
            else _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

}
