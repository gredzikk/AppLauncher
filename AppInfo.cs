using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace app_launcher
{
    public class AppInfo : INotifyPropertyChanged
    {
        private string _name = "N/A";
        private string? _path = "N/A"; 
        private string? _version = "N/A";
        private string? _execPath; 

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string? Path
        {
            get => _path;
            set => SetField(ref _path, value);
        }

        public string? Version
        {
            get => _version;
            set => SetField(ref _version, value);
        }

        public string? ExecPath
        {
            get => _execPath;
            set
            {
                if (SetField(ref _execPath, value))
                {
                    OnPropertyChanged(nameof(IsConfigured)); 
                }
            }
        }

        public bool IsConfigured => !string.IsNullOrEmpty(ExecPath);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}