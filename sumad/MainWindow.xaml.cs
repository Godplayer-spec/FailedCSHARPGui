using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace sumad
{ 


    

    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                values[0] is double progress &&
                values[1] is double totalWidth)
            {
                return (progress / 100) * totalWidth;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class MainWindow : Window



    {

        //dll practice: 
        // Import the function from the C++ DLL
        // Define the delegate that matches the callback function signature
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ProgressCallback(int progress, [MarshalAs(UnmanagedType.LPStr)] string currentPath);
        //test ig
        [DllImport("FSKCoredll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HelloFromCpp();

        //for cleaning resgirty path cant spell ok!
        [DllImport("FSKCoredll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void CleanRegistry(ProgressCallback callback);

        [DllImport("FSKCoredll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TestCallback();
        // run dll after ui is rdy setty goey
        [DllImport("FSKCoredll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void InitializeDLL();




        // Define the delegate for the callback method
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ConsoleCallback(string text);

        // Import the C++ DLL function
        [DllImport("FSKCoredll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetConsoleCallback(ConsoleCallback callback);

        [DllImport("FSKCoredll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AttachMessage();


        private DispatcherTimer _timer;
        private int _currentIndex = 0;
        private string[] _notifications = new string[]
        {
            "🆕 New Update Available!",
            "✅ You're currently using the latest version.",
            "⚠️ FraudSKlent is not compatible with the latest update :("
        };
        private Random random = new Random(); // Random object for random snowflake behavior

        // Timer to create snowflakes at intervals
        private DispatcherTimer snowTimer;

        // To limit the number of snowflakes on screen at once
        private static int MaxSnowflakes = 60;
        private static int MAX_SNOWFLAKES = MaxSnowflakes;
        private int currentSnowflakes = 0;

        // below is console related
        private ConsoleCallback _consoleCallback;
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private Dictionary<string, Action<string[]>> _commands;

        //above strictly console related

        public MainWindow()
        {
            InitializeComponent();

            // Set up the UI components first
            StartSnowfall();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1.5);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            LoadConfig();
            PowerSavingToggle.IsChecked = _powerSavingEnabled;
            StartCleaningButton.IsEnabled = true;

            // Initialize DLL communication after UI is ready
            Task.Delay(100).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Setup callback
                        _consoleCallback = new ConsoleCallback(AddToConsole);
                        SetConsoleCallback(_consoleCallback);

                        // Initialize DLL
                        InitializeDLL();

                        
                    }

                    catch (DllNotFoundException ex)
                    {
                        //using it to initialize the console too cuz why not lol
                        InitializeConsoleCommands();
                        string dllName = ExtractDllName(ex.Message);
                        MessageBox.Show($"I was unable to find the  required DLL: {dllName}. Please add it manually.",
                                        "Missing DLL", MessageBoxButton.OK, MessageBoxImage.Error);
                        AddToConsoleWithLevel($"Couldn't Find {dllName} To Load.", LogLevel.Error);
                        AddToConsoleWithLevel($"Couldn't Find {dllName} To Load.", LogLevel.Info);
                    }

                });
            });
        }

        // Helper method to extract the DLL name from the exception message
        private string ExtractDllName(string message)
        {
            int startIndex = message.IndexOf("'") + 1;
            int endIndex = message.IndexOf("'", startIndex);
            return startIndex > 0 && endIndex > startIndex
                ? message.Substring(startIndex, endIndex - startIndex)
                : "Unknown DLL";
        }

        // to ensure that this guy doesn't crash my laptop cuz im using it to test 
        private DispatcherTimer _resourceMonitorTimer;
        private Process currentProcess;
        private const float CPU_THRESHOLD = 3.5f;
        private const float MEMORY_THRESHOLD = 50f;
        private const int MAX_THROTTLE = 64;
        private const int THROTTLE_INCREMENT = 8;
        private PerformanceCounter _cpuCounter;

        private void InitializeResourceMonitoring()
        {
            try
            {
                
                currentProcess = Process.GetCurrentProcess();
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", currentProcess.ProcessName);

                _resourceMonitorTimer = new DispatcherTimer();
                _resourceMonitorTimer.Interval = TimeSpan.FromSeconds(7);
                _resourceMonitorTimer.Tick += MonitorResources;
                _resourceMonitorTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize resource monitoring: {ex.Message}");
            }
        }


        private void MonitorResources(object sender, EventArgs e)
        {
            try
            {
                currentProcess.Refresh();
                float cpuUsage = GetCurrentCpuUsage();
                float memoryUsageMB = currentProcess.WorkingSet64 / 1024f / 1024f;

                if (cpuUsage > CPU_THRESHOLD || memoryUsageMB > MEMORY_THRESHOLD)
                {
                    ApplyResourceMitigations();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in resource monitoring: {ex.Message}");
            }
        }

        private void ApplyResourceMitigations()
        {
            try
            {
                // Use your existing MaxSnowflakes variable
                MaxSnowflakes = Math.Max(50, MaxSnowflakes - 50);
                DRAG_THROTTLE_MS = Math.Min(MAX_THROTTLE, DRAG_THROTTLE_MS + THROTTLE_INCREMENT);

                if (currentSnowflakes > MaxSnowflakes / 2)
                {
                    RemoveExcessSnowflakes();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying resource mitigations: {ex.Message}");
            }
        }

        // Reuse existing PerformanceCounter
        private float GetCurrentCpuUsage()
        {
            try
            {
                if (_cpuCounter == null) return 0;
                return _cpuCounter.NextValue() / Environment.ProcessorCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU usage: {ex.Message}");
                return 0;
            }
        }


        // Add cleanup in Window_Closed event handler
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                _resourceMonitorTimer?.Stop();
                _cpuCounter?.Dispose();
                currentProcess?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
        // no hard feelings but if you have to run this cuz it's making ur pc slow then broski 
        // Add these properties to your MainWindow class



        private void PowerSavingToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Update the PowerSavingEnabled property
            PowerSavingEnabled = true;
            PowerSavingStatus.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void PowerSavingToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Update the PowerSavingEnabled property
            PowerSavingEnabled = false;
            PowerSavingStatus.Foreground = new SolidColorBrush(Color.FromRgb(47, 123, 166));
        }
        private const string CONFIG_PATH = "config.json";
        private bool _powerSavingEnabled = false;

        public bool PowerSavingEnabled
        {
            get => _powerSavingEnabled;
            set
            {
                _powerSavingEnabled = value;
                if (value)
                {
                    InitializeResourceMonitoring();
                    AddToConsoleWithLevel("Confirmed, power saving is enabled. Hopefully this helps you.", LogLevel.Info); // Log when power saving is enabled
                }
                else
                {
                    DisableResourceMonitoring();
                    AddToConsoleWithLevel("Power saving has been disabled. I see you decided to role with the big boys😎", LogLevel.Info); // Log when power saving is disabled
                }
                SaveConfig();
            }
        }

        // Configuration class
        private class AppConfig
        {
            public bool PowerSavingEnabled { get; set; }
        }

        // Add these methods to manage configuration
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(CONFIG_PATH))
                {
                    var json = File.ReadAllText(CONFIG_PATH);
                    var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
                    _powerSavingEnabled = config.PowerSavingEnabled;

                    if (_powerSavingEnabled)
                    {
                        InitializeResourceMonitoring();
                    }
                }
            }
            catch (Exception ex)
            {
                AddToConsoleWithLevel($"Hm coudln't load the json file :/ ");
                Debug.WriteLine($"Error loading config: {ex.Message}"); 
                
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new AppConfig
                {
                    PowerSavingEnabled = _powerSavingEnabled
                };
                var json = System.Text.Json.JsonSerializer.Serialize(config);
                File.WriteAllText(CONFIG_PATH, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        private void DisableResourceMonitoring()
        {
            _resourceMonitorTimer?.Stop();
            _cpuCounter?.Dispose();
            currentProcess?.Dispose();

            // Reset values
            MaxSnowflakes = 60; // Your original value
            DRAG_THROTTLE_MS = 16; // Reset to original value
        }



        private void RemoveExcessSnowflakes()
        {
            // Remove some snowflakes if we have too many
            int snowflakesToRemove = (int)(currentSnowflakes * 0.5); // Remove 20%
            for (int i = MainCanvas.Children.Count - 1; i >= 0 && snowflakesToRemove > 0; i--)
            {
                if (MainCanvas.Children[i] is Ellipse)
                {
                    MainCanvas.Children.RemoveAt(i);
                    currentSnowflakes--;
                    snowflakesToRemove--;
                }
            }
        }



        // Timer_Tick event handler for updating notifications
        private void Timer_Tick(object sender, EventArgs e)
        {
            NotificationTextBlock.Text = _notifications[_currentIndex];

            // Update the index for the next notification
            _currentIndex++;
            if (_currentIndex >= _notifications.Length)
            {
                _currentIndex = 0;  // Loop back to the first notification
            }
        }

        private void StartSnowfall()
        {
            // Initialize and start a timer to drop snowflakes
            snowTimer = new DispatcherTimer();
            snowTimer.Interval = TimeSpan.FromMilliseconds(150); // Adjust interval for performance
            snowTimer.Tick += SnowTimer_Tick;
            snowTimer.Start();
        }

        private void SnowTimer_Tick(object sender, EventArgs e)

        {
            if (isSnowfallPaused)
                return;

            // Only create new snowflakes every 32ms
            if ((DateTime.Now - lastSnowUpdate).TotalMilliseconds < 32)
                return;

            // Limit snowflakes on screen to MaxSnowflakes
            if (currentSnowflakes >= MaxSnowflakes)
                return;

            // Create a new snowflake
            Ellipse snowflake = new Ellipse
            {
                Width = random.Next(5, 15), // Random size for snowflakes
                Height = random.Next(5, 15),
                Fill = new SolidColorBrush(Colors.White),
                Opacity = random.NextDouble() * 0.5 + 0.5  // Random opacity for a more natural look
            };

            // Position the snowflake at a random X position
            double xPosition = random.Next(0, (int)this.ActualWidth);
            Canvas.SetLeft(snowflake, xPosition);
            Canvas.SetTop(snowflake, -snowflake.Height); // Start off-screen

            // Add the snowflake to the canvas
            MainCanvas.Children.Add(snowflake);
            currentSnowflakes++;

            // Add random rotation for each snowflake to simulate drifting
            double rotationAngle = random.Next(0, 360);
            snowflake.RenderTransform = new RotateTransform(rotationAngle);

            // Animate the snowflake falling down the screen
            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = -snowflake.Height, // Start from the top
                To = this.ActualHeight,    // End at the bottom
                Duration = new Duration(TimeSpan.FromSeconds(random.Next(3, 5))), // Shorter fall duration for smooth animation
                AutoReverse = false
            };

            fallAnimation.Completed += (s, args) =>
            {
                // Remove the snowflake once it reaches the bottom
                MainCanvas.Children.Remove(snowflake);
                currentSnowflakes--; // Decrement the snowflake count
            };

            snowflake.BeginAnimation(Canvas.TopProperty, fallAnimation);
            lastSnowUpdate = DateTime.Now;

        }
        private void MinimizeWindow(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseWindow(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void CleanerTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide all tabs
            CleanerTabContent.Visibility = Visibility.Visible;
            AnnouncementTabContent.Visibility = Visibility.Collapsed;
            SerialCheckerTabContent.Visibility = Visibility.Collapsed;
            ConsoleLogTabContent.Visibility = Visibility.Collapsed;
        }

        private void AnnouncementTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide all tabs
            CleanerTabContent.Visibility = Visibility.Collapsed;
            AnnouncementTabContent.Visibility = Visibility.Visible;
            SerialCheckerTabContent.Visibility = Visibility.Collapsed;
            ConsoleLogTabContent.Visibility = Visibility.Collapsed;
        }

        private void SerialCheckerTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide all tabs
            CleanerTabContent.Visibility = Visibility.Collapsed;
            AnnouncementTabContent.Visibility = Visibility.Collapsed;
            SerialCheckerTabContent.Visibility = Visibility.Visible;
            ConsoleLogTabContent.Visibility = Visibility.Collapsed;
        }

        // Event handler for StartCleaningButton click
        private void StartCleaningButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the button while cleaning
            StartCleaningButton.IsEnabled = false;
            CleaningProgressBar.Value = 0;
            CleaningStatusText.Text = "Cleaning in progress...";

            // Create the callback delegate
            ProgressCallback progressCallback = (progress, currentPath) =>
            {
                // Since we're being called from an unmanaged thread, we need to use Dispatcher
                Dispatcher.Invoke(() =>
                {
                    CleaningProgressBar.Value = progress;
                    CleaningStatusText.Text = $"Cleaning: {currentPath}";

                    if (progress >= 100)
                    {
                        CleaningStatusText.Text = "Cleaning completed!";
                        StartCleaningButton.IsEnabled = true;
                        HelloFromCpp(); // Show completion message
                        AddToConsole("Console cleared");
                    }
                });
            };

            // Start the cleaning process in a separate thread to keep UI responsive
            Task.Run(() =>
            {
                try
                {
                    // Call the CleanRegistry function in the background
                    CleanRegistry(progressCallback);
                }
                catch (Exception ex)
                {
                    // Ensure UI updates are done on the UI thread
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Error during cleaning: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StartCleaningButton.IsEnabled = true;  // Re-enable the button if an error occurred
                    });
                }
            });

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void RefreshSerial_Click(object sender, RoutedEventArgs e)
        {
            // Start loading, change text, or update image as needed
            SerialImage.Visibility = Visibility.Visible; // If hidden by default
            SerialNumberLabel.Text = "Fetching Serial...";

            // Simulate fetching serial (replace with actual logic)
            Task.Delay(2000).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Once "fetching" is done, update the UI
                    SerialNumberLabel.Text = "Serial: ABC123456789";
                    SerialImage.Visibility = Visibility.Hidden; // Hide the spinner
                });
            });
        }

        // Add these fields to your MainWindow class
        private bool isDragging = false;
        private Point dragStart;
        private static int DRAG_THROTTLE_MS = 16; // Start with 60fps
        private DateTime lastSnowUpdate = DateTime.MinValue;
        private DateTime lastBackgroundUpdate = DateTime.MinValue;
        private bool isSnowfallPaused = false;

        // Modify your existing Window_MouseLeftButtonDown
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                isDragging = true;
                dragStart = e.GetPosition(this);
                this.CaptureMouse();

                // Completely pause snowfall during drag
                isSnowfallPaused = true;

            }
        }

        // Add Window_MouseLeftButtonUp handler
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                this.ReleaseMouseCapture();

                // Resume snowfall
                isSnowfallPaused = false;
            }
        }

        // Modify your existing Window_MouseMove
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var currentPos = e.GetPosition(this);
                var offset = currentPos - dragStart;

                // Update window position using transform for better performance
                this.Left += offset.X;
                this.Top += offset.Y;
                // Only update background every 32ms (30fps) during drag
                if ((DateTime.Now - lastBackgroundUpdate).TotalMilliseconds >= DRAG_THROTTLE_MS)
                {
                    UpdateDynamicBackground(e);
                    lastBackgroundUpdate = DateTime.Now;
                }
                return;
            }
            // Normal mouse move handling when not dragging
            // Only update every 16ms (60fps) for smoother performance
            if ((DateTime.Now - lastBackgroundUpdate).TotalMilliseconds >= 16)
            {
                UpdateDynamicBackground(e);
                lastBackgroundUpdate = DateTime.Now;
            }
        }
        // Separate method for background updates
        private void UpdateDynamicBackground(MouseEventArgs e)
        {
            var mousePosition = e.GetPosition(this);
            double xPercentage = mousePosition.X / this.ActualWidth;
            double yPercentage = mousePosition.Y / this.ActualHeight;

            DynamicBackground.StartPoint = new Point(xPercentage, yPercentage);
            DynamicBackground.EndPoint = new Point(1 - xPercentage, 1 - yPercentage);
        }

        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void SaveLogs_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"ConsoleLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ConsoleTextBox.Text);
                    MessageBox.Show("Logs saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all logs?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ConsoleTextBox.Clear();
                
            }
        }
        // Method to add text to console
        // Enhanced AddToConsole with log levels
        private void AddToConsole(string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AddToConsole(text));
                return;
            }

            ConsoleTextBox?.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm}] {text}{Environment.NewLine}");
            ConsoleTextBox?.ScrollToEnd();
        }


        public static class LogSigns
        {
            public static readonly Dictionary<string, string> LevelSigns = new Dictionary<string, string>
        {
            { "Error", "❌ " },
            { "Warning", "⚠️ " },
            { "Info", "ℹ️ " },
            { "Success", "✅ " },
            { "Debug", "🔧 " }
        };
        }

        private void AddToConsoleWithLevel(string text, LogLevel level = LogLevel.Info)
        {
            string prefix = level switch
            {
                LogLevel.Error => "❌ ",    // Error sign
                LogLevel.Warning => "⚠️ ",  // Warning sign
                LogLevel.Info => "ℹ️ ",     // Info sign
                LogLevel.Success => "✅ ",   // Success sign
                LogLevel.Debug => "🔧 ",    // Debug sign
                _ => ""
            };

            AddToConsole($"{prefix}{text}");
        }
        public enum LogLevel
        {
            Error,   // ❌
            Warning, // ⚠️
            Info,    // ℹ️
            Success, // ✅
            Debug    // 🔧
        }

        private void ConsoleLogTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide all tabs
            CleanerTabContent.Visibility = Visibility.Collapsed;
            AnnouncementTabContent.Visibility = Visibility.Collapsed;
            SerialCheckerTabContent.Visibility = Visibility.Collapsed;
            ConsoleLogTabContent.Visibility = Visibility.Visible;
        }
        // console cmds
        private void InitializeConsoleCommands()
        {
            _commands = new Dictionary<string, Action<string[]>>
        {
            // Clear console command
            {"clear", _ => {
                ConsoleTextBox.Clear();
                AddToConsole("Console cleared");
            }},
            
            // Help command
            {"help", _ => {
                AddToConsole("Available commands:");
                AddToConsole("  clear - Clears the console");
                AddToConsole("  help - Shows this help message");
                AddToConsole("  time - Shows current time");
                AddToConsole("  filter <text> - Shows only lines containing the specified text");
                AddToConsole("  export <filename> - Exports console content to a file");
                AddToConsole("  status - Shows current system status");
            }},
            
            // Time command
            {"time", _ => {
                AddToConsole($"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }},
            
            // Filter command
            // Inside the InitializeConsoleCommands method, replace the filter command with:
            {"filter", args => {
                if (args.Length < 1) {
                    AddToConsole("Usage: filter <text>");
                    return;
                }

                string filterText = args[0].ToLower(); // Make filter case-insensitive
    
                // Create a dictionary to map between text and emojis both ways
                var symbolMap = new Dictionary<string, string>
                {
                    {"error", "❌"},
                    {"warning", "⚠️"},
                    {"info", "ℹ️"},
                    {"success", "✅"},
                    {"debug", "🔧"},
                    // Reverse mappings
                    {"❌", "error"},
                    {"⚠️", "warning"},
                    {"ℹ️", "info"},
                    {"✅", "success"},
                    {"🔧", "debug"}
                };

                string[] lines = ConsoleTextBox.Text.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries
                );

                var filteredLines = lines.Where(line => {
                    if (line.Contains("> filter")) return false;

                    string lowerLine = line.ToLower();
        
                    // Check if the filter text is in our symbol map
                    if (symbolMap.ContainsKey(filterText))
                    {
                        // Check for both the text version and emoji version
                        return lowerLine.Contains(filterText) ||
                               lowerLine.Contains(symbolMap[filterText]);
                    }
        
                    // If not in the map, just do a normal contains check
                    return lowerLine.Contains(filterText);
                });

                ConsoleTextBox.Text = string.Join(Environment.NewLine, filteredLines);
            }},


            
            // Export command
            {"export", args => {
                if (args.Length < 1) {
                    AddToConsole("Usage: export <filename>");
                    return;
                }
                try {
                    File.WriteAllText(args[0], ConsoleTextBox.Text);
                    AddToConsole($"Console content exported to {args[0]}");
                }
                catch (Exception ex) {
                    AddToConsole($"Error exporting: {ex.Message}");
                }
            }},
            
            // Status command
            {"status", _ => {
                var process = Process.GetCurrentProcess();
                AddToConsole($"Memory usage: {process.WorkingSet64 / 1024 / 1024} MB");
                AddToConsole($"Process uptime: {DateTime.Now - process.StartTime}");
                AddToConsole($"Thread count: {process.Threads.Count}");
            }}
        };
        }
        // Add a command input TextBox in your XAML named CommandInput
        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string command = CommandInput.Text.Trim();
                if (string.IsNullOrEmpty(command)) return;

                // Add to history
                _commandHistory.Add(command);
                _historyIndex = _commandHistory.Count;

                // Process command
                string[] parts = command.Split(' ');
                string cmd = parts[0].ToLower();
                string[] args = parts.Skip(1).ToArray();

                if (_commands.ContainsKey(cmd))
                {
                    AddToConsole($"> {command}");
                    _commands[cmd](args);
                }
                else
                {
                    AddToConsole($"Unknown command: {cmd}. Type 'help' for available commands.");
                }

                CommandInput.Clear();
            }
            else if (e.Key == Key.Up)
            {
                if (_historyIndex > 0)
                {
                    _historyIndex--;
                    CommandInput.Text = _commandHistory[_historyIndex];
                    CommandInput.CaretIndex = CommandInput.Text.Length;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (_historyIndex < _commandHistory.Count - 1)
                {
                    _historyIndex++;
                    CommandInput.Text = _commandHistory[_historyIndex];
                    CommandInput.CaretIndex = CommandInput.Text.Length;
                }
                else
                {
                    _historyIndex = _commandHistory.Count;
                    CommandInput.Clear();
                }
            }
        }
        private void ConsoleTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            //idk what to put
        }

    }
}
