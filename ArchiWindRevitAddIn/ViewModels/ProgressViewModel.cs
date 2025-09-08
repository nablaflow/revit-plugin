using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace ArchiWindRevitAddIn.ViewModels
{
    public sealed partial class ProgressViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private int progressValue = 0;

        [ObservableProperty]
        private int progressMaximum = 100;

        [ObservableProperty]
        private bool canCancel = true;

        [ObservableProperty]
        private bool isCompleted = false;

        public ObservableCollection<string> LogMessages { get; } = [];

        public RelayCommand CancelCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }

        private readonly CancellationTokenSource cancellationTokenSource = new();
        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public Dispatcher Dispatcher { get; private set; }

        public ProgressViewModel(string title)
        {
            Title = title;
            Dispatcher = Dispatcher.CurrentDispatcher;

            CancelCommand = new(Cancel, () => CanCancel && !IsCompleted);
            CloseCommand = new(Close, () => IsCompleted);
        }

        private void Cancel()
        {
            AddLogMessage("Cancelling...");

            cancellationTokenSource.Cancel();
            CanCancel = false;

            AddLogMessage("Process cancelled by the user.");
        }

        private void Close()
        {
        }

        public void UpdateProgress(int incr)
        {
            ProgressValue += incr;
        }

        public void AddLogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogMessages.Add($"[{timestamp}] {message}");
        }

        public void SetCompleted(string finalMessage)
        {
            IsCompleted = true;
            CanCancel = false;
            ProgressValue = ProgressMaximum;

            AddLogMessage(finalMessage);
        }
    }
}
