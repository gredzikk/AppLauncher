using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace app_launcher {
    /// <summary>
    /// ViewModel for the main application window, handling application data and user interactions.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged {
        private static readonly string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string AppFolder = Path.Combine(AppDataFolder, "AppLauncher");
        private static readonly string SaveFilePath = Path.Combine(AppFolder, "applications.json");

        /// <summary>
        /// Gets or sets the collection of applications displayed in the UI.
        /// </summary>
        public ObservableCollection<AppInfo> Applications { get; set; }

        /// <summary>
        /// Command to select an executable file for an application.
        /// </summary>
        public ICommand SelectExecCommand { get; }
        /// <summary>
        /// Command to launch an application.
        /// </summary>
        public ICommand LaunchExecCommand { get; }
        /// <summary>
        /// Command to open the folder containing an application's executable.
        /// </summary>
        public ICommand OpenFolderCommand { get; }
        /// <summary>
        /// Command to add a new application entry.
        /// </summary>
        public ICommand AddAppCommand { get; }
        /// <summary>
        /// Command to remove an existing application entry.
        /// </summary>
        public ICommand RemoveAppCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// Loads existing applications and initializes commands.
        /// </summary>
        public MainViewModel() {
            Applications = LoadApplications();

            SelectExecCommand = new RelayCommand<AppInfo>(SelectExecutable);
            LaunchExecCommand = new RelayCommand<AppInfo>(LaunchExecutable, CanLaunchExecutable);
            OpenFolderCommand = new RelayCommand<AppInfo>(OpenExecutableFolder, CanOpenExecutableFolder);
            AddAppCommand = new RelayCommand(AddApplication);
            RemoveAppCommand = new RelayCommand<AppInfo>(RemoveApplication);
        }

        /// <summary>
        /// Loads the list of applications from a JSON file.
        /// If the file does not exist or an error occurs, an empty list is returned.
        /// </summary>
        /// <returns>An <see cref="ObservableCollection{AppInfo}"/> containing the loaded applications.</returns>
        private ObservableCollection<AppInfo> LoadApplications() {
            FileLogger.LogInfo("Attempting to load applications.");
            try {
                if (File.Exists(SaveFilePath)) {
                    string json = File.ReadAllText(SaveFilePath);
                    var loadedApps = JsonSerializer.Deserialize<ObservableCollection<AppInfo>>(json);
                    if (loadedApps != null) {
                        FileLogger.LogInfo($"Successfully loaded {loadedApps.Count} applications from {SaveFilePath}.");
                        return loadedApps;
                    }
                    FileLogger.LogWarning($"No applications found or deserialization failed from {SaveFilePath}.");
                }
                else {
                    FileLogger.LogInfo($"Application save file not found at {SaveFilePath}. Starting with an empty list.");
                }
            }
            catch (Exception ex) {
                FileLogger.LogError($"Error loading application list from {SaveFilePath}.", ex);
                MessageBox.Show($"Błąd wczytywania listy aplikacji:\n{ex.Message}", "Błąd wczytywania", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return new ObservableCollection<AppInfo>();
        }

        /// <summary>
        /// Saves the current list of applications to a JSON file.
        /// Creates the application data folder if it doesn't exist.
        /// </summary>
        public void SaveApplications() {
            FileLogger.LogInfo("Attempting to save applications.");
            try {
                Directory.CreateDirectory(AppFolder);

                string json = JsonSerializer.Serialize(Applications, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
                FileLogger.LogInfo($"Successfully saved {Applications.Count} applications to {SaveFilePath}.");
            }
            catch (Exception ex) {
                FileLogger.LogError($"Error saving application list to {SaveFilePath}.", ex);
            }
        }

        /// <summary>
        /// Adds a new, unconfigured application entry to the list and saves the list.
        /// </summary>
        private void AddApplication() {
            var newApp = new AppInfo { Name = "Nowy program" };
            Applications.Add(newApp);
            FileLogger.LogInfo($"Added new application: '{newApp.Name}'.");
            SaveApplications();
        }

        /// <summary>
        /// Removes the specified application from the list after user confirmation and saves the list.
        /// </summary>
        /// <param name="app">The application to remove. If null, the method returns without action.</param>
        private void RemoveApplication(AppInfo? app) {
            if (app == null) return;

            var result = MessageBox.Show($"Na pewno chcesz usunąć '{app.Name}'?",
                                         "Potwierdź usunięcie",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes) {
                string appName = app.Name;
                Applications.Remove(app);
                FileLogger.LogInfo($"Removed application: '{appName}'.");
                SaveApplications();
            }
            else {
                FileLogger.LogInfo($"Removal cancelled for application: '{app.Name}'.");
            }
        }

        /// <summary>
        /// Opens a file dialog for the user to select an executable file for the specified application.
        /// Updates the application's properties (ExecPath, Version, Path, Name) and saves the list.
        /// </summary>
        /// <param name="app">The application for which to select an executable. If null, the method returns without action.</param>
        private void SelectExecutable(AppInfo? app) {
            if (app == null) return;
            string oldAppName = app.Name;

            var openFileDialog = new OpenFileDialog {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = $"Wybierz plik wykonywalny {app.Name}"
            };

            if (openFileDialog.ShowDialog() == true) {
                app.ExecPath = openFileDialog.FileName;
                app.Version = GetFileVersion(app.ExecPath);
                app.Path = System.IO.Path.GetDirectoryName(app.ExecPath);

                if (app.Name == "Nowy program" || string.IsNullOrEmpty(app.Name)) {
                    app.Name = System.IO.Path.GetFileNameWithoutExtension(app.ExecPath);
                }
                FileLogger.LogInfo($"Selected executable for '{oldAppName}': Path='{app.ExecPath}', Version='{app.Version}', Name set to '{app.Name}'.");
                SaveApplications();

                (LaunchExecCommand as RelayCommand<AppInfo>)?.RaiseCanExecuteChanged();
                (OpenFolderCommand as RelayCommand<AppInfo>)?.RaiseCanExecuteChanged();
            }
            else {
                FileLogger.LogInfo($"Executable selection cancelled for '{app.Name}'.");
            }
        }

        /// <summary>
        /// Determines whether the specified application can be launched.
        /// </summary>
        /// <param name="app">The application to check.</param>
        /// <returns>True if the application is not null, has a valid ExecPath, and the file exists; otherwise, false.</returns>
        private bool CanLaunchExecutable(AppInfo? app) {
            return app != null && !string.IsNullOrEmpty(app.ExecPath) && File.Exists(app.ExecPath);
        }

        /// <summary>
        /// Launches the specified application.
        /// Logs the attempt and any errors that occur.
        /// </summary>
        /// <param name="app">The application to launch. If null or not launchable, the method returns without action.</param>
        private void LaunchExecutable(AppInfo? app) {
            if (!CanLaunchExecutable(app) || app?.ExecPath == null) {
                FileLogger.LogWarning($"Launch attempt failed for '{app?.Name}': Cannot launch (App or ExecPath is null, or file does not exist). ExecPath: '{app?.ExecPath}'");
                return;
            }

            FileLogger.LogInfo($"Attempting to launch application: '{app.Name}' from '{app.ExecPath}'.");
            try {
                string? directory = System.IO.Path.GetDirectoryName(app.ExecPath);
                Process.Start(new ProcessStartInfo(app.ExecPath) {
                    WorkingDirectory = directory,
                    UseShellExecute = true
                });
                FileLogger.LogInfo($"Successfully launched '{app.Name}'.");
            }
            catch (Exception ex) {
                FileLogger.LogError($"Error launching application '{app.Name}' from '{app.ExecPath}'.", ex);
                System.Windows.MessageBox.Show($"Błąd uruchamiania {app.Name}:\n{ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Determines whether the folder containing the specified application's executable can be opened.
        /// </summary>
        /// <param name="app">The application to check.</param>
        /// <returns>True if the application is not null, has a valid ExecPath, and its directory path is not empty; otherwise, false.</returns>
        private bool CanOpenExecutableFolder(AppInfo? app) {
            return app != null && !string.IsNullOrEmpty(app.ExecPath) && !string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(app.ExecPath));
        }

        /// <summary>
        /// Opens the folder containing the specified application's executable in File Explorer and selects the file.
        /// Logs the attempt and any errors that occur.
        /// </summary>
        /// <param name="app">The application whose folder is to be opened. If null or the folder cannot be opened, the method returns without action.</param>
        private void OpenExecutableFolder(AppInfo? app) {
            if (!CanOpenExecutableFolder(app) || app?.ExecPath == null) {
                FileLogger.LogWarning($"Open folder attempt failed for '{app?.Name}': Cannot open folder (App or ExecPath is null, or directory does not exist). ExecPath: '{app?.ExecPath}'");
                return;
            }

            string? directory = System.IO.Path.GetDirectoryName(app.ExecPath);
            FileLogger.LogInfo($"Attempting to open folder for application: '{app.Name}' at '{directory}'.");
            if (directory != null && Directory.Exists(directory)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{app.ExecPath}\"");
                    FileLogger.LogInfo($"Successfully opened folder for '{app.Name}'.");
                }
                catch (Exception ex) {
                    FileLogger.LogError($"Error opening folder for application '{app.Name}' at '{directory}'.", ex);
                    System.Windows.MessageBox.Show($"Błąd otwierania folderu dla {app.Name}:\n{ex.Message}", "Błąd folderu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else {
                FileLogger.LogWarning($"Directory not found for '{app.Name}': '{directory}'.");
                System.Windows.MessageBox.Show($"Nie znaleziono ścieżki {directory}\n dla {app.Name}", "Błąd ścieżki", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Retrieves the file version of the specified executable file.
        /// </summary>
        /// <param name="filePath">The path to the executable file.</param>
        /// <returns>The file version as a string, or "N/A" if the version cannot be retrieved or an error occurs.</returns>
        private string GetFileVersion(string? filePath) {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                FileLogger.LogWarning($"Cannot get file version. FilePath is null, empty, or does not exist: '{filePath}'.");
                return "N/A";
            }
            try {
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                var version = versionInfo.FileVersion ?? "N/A";
                FileLogger.LogInfo($"Retrieved file version for '{filePath}': '{version}'.");
                return version;
            }
            catch (Exception ex) {
                FileLogger.LogError($"Error getting file version for '{filePath}'.", ex);
                return "N/A";
            }
        }

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed. Automatically determined by the caller.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}