using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace SmoothScrollApp
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public class Program
    {
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string mutexName = "SmoothScrollApp_SingleInstance_Mutex";
            bool createdNew;

            mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Smooth Scroll is already running!\n\nCheck the system tray.", 
                    "Already Running", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SmoothScrollMainForm());

            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }
    }

    public class SmoothScrollMainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private SmoothScrollEngine scrollEngine;
        private SettingsForm settingsForm;

        public SmoothScrollMainForm()
        {
            scrollEngine = new SmoothScrollEngine();
            InitializeTrayIcon();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            scrollEngine.Start();
            scrollEngine.LoadSettings();
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Settings", null, OnSettings);
            trayMenu.Items.Add(new ToolStripSeparator());
            
            var enableItem = trayMenu.Items.Add("Enable Smooth Scroll", null, OnToggle);
            if (enableItem is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = true;
            }
            
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Smooth Scroll - Active";
            trayIcon.Icon = CreateTrayIcon();
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += OnSettings;
        }

        private Icon CreateTrayIcon()
        {
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.DodgerBlue, 2, 2, 12, 12);
                g.DrawEllipse(Pens.White, 2, 2, 12, 12);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void OnSettings(object sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm(scrollEngine);
            }
            
            if (settingsForm != null)
            {
                settingsForm.Show();
                settingsForm.BringToFront();
                settingsForm.WindowState = FormWindowState.Normal;
            }
        }

        private void OnToggle(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item)
            {
                if (item.Checked)
                {
                    scrollEngine.Stop();
                    item.Checked = false;
                    trayIcon.Text = "Smooth Scroll - Disabled";
                }
                else
                {
                    scrollEngine.Start();
                    item.Checked = true;
                    trayIcon.Text = "Smooth Scroll - Active";
                }
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            scrollEngine.Stop();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (trayIcon != null)
                {
                    trayIcon.Dispose();
                }
                if (scrollEngine != null)
                {
                    scrollEngine.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    public class SettingsForm : Form
    {
        private SmoothScrollEngine engine;
        private TrackBar smoothnessTrackBar = new TrackBar();
        private TrackBar multiplierTrackBar = new TrackBar();
        private TrackBar intervalTrackBar = new TrackBar();
        private Label smoothnessValueLabel = new Label();
        private Label multiplierValueLabel = new Label();
        private Label intervalValueLabel = new Label();
        private Button applyButton = new Button();
        private Button resetButton = new Button();
        private Label statusLabel = new Label();
        private Button themeToggleButton = new Button();
        private Panel supportPanel;
        private Label supportLabel;
        private LinkLabel donateLink;

        // Theme colors
        private Color lightBg = Color.White;
        private Color lightText = Color.FromArgb(50, 50, 50);
        private Color lightSecondary = Color.Gray;
        private Color lightPanel = Color.FromArgb(240, 240, 240);
        private Color lightBorder = Color.FromArgb(200, 200, 200);

        private Color darkBg = Color.FromArgb(30, 30, 30);
        private Color darkText = Color.FromArgb(220, 220, 220);
        private Color darkSecondary = Color.FromArgb(160, 160, 160);
        private Color darkPanel = Color.FromArgb(45, 45, 45);
        private Color darkBorder = Color.FromArgb(60, 60, 60);

        public SettingsForm(SmoothScrollEngine scrollEngine)
        {
            this.engine = scrollEngine;
            InitializeComponents();
            LoadCurrentSettings();
            ApplyTheme(engine.Theme);
        }

        private void InitializeComponents()
        {
            this.Text = "Smooth Scroll Settings";
            this.ClientSize = new Size(500, 530);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label titleLabel = new Label
            {
                Text = "Smooth Scroll Configuration",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(380, 30)
            };
            this.Controls.Add(titleLabel);

            // Theme toggle button
            themeToggleButton = new Button
            {
                Text = "🌙 Night",
                Location = new Point(410, 20),
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            themeToggleButton.FlatAppearance.BorderSize = 1;
            themeToggleButton.Click += ThemeToggleButton_Click;
            this.Controls.Add(themeToggleButton);

            int yPos = 70;

            CreateSettingControl("Smoothness", "Lower = smoother, slower", smoothnessTrackBar, smoothnessValueLabel, yPos, 10, 50, 25);
            yPos += 80;

            CreateSettingControl("Scroll Speed", "How much to scroll per wheel tick", multiplierTrackBar, multiplierValueLabel, yPos, 5, 30, 10);
            yPos += 80;

            CreateSettingControl("Update Rate", "Lower = smoother (more CPU)", intervalTrackBar, intervalValueLabel, yPos, 4, 16, 8);
            yPos += 90;

            statusLabel = new Label
            {
                Text = "",
                Location = new Point(20, yPos - 10),
                Size = new Size(450, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            this.Controls.Add(statusLabel);

            applyButton = new Button
            {
                Text = "Apply Settings",
                Location = new Point(180, yPos + 20),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += ApplyButton_Click;
            this.Controls.Add(applyButton);

            resetButton = new Button
            {
                Text = "Reset to Default",
                Location = new Point(310, yPos + 20),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            resetButton.FlatAppearance.BorderSize = 1;
            resetButton.Click += ResetButton_Click;
            this.Controls.Add(resetButton);

            yPos += 70;
            supportPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(460, 50),
                BorderStyle = BorderStyle.FixedSingle
            };

            supportLabel = new Label
            {
                Text = "💖 Support the project",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 5),
                Size = new Size(200, 20),
                BackColor = Color.Transparent
            };
            supportPanel.Controls.Add(supportLabel);

            donateLink = new LinkLabel
            {
                Text = "Buy me a coffee! ☕",
                Font = new Font("Segoe UI", 9),
                Location = new Point(10, 27),
                Size = new Size(200, 18),
                BackColor = Color.Transparent
            };
            donateLink.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://www.donationalerts.com/r/kilorrpy",
                        UseShellExecute = true
                    });
                }
                catch { }
            };
            supportPanel.Controls.Add(donateLink);

            this.Controls.Add(supportPanel);
        }

        private void CreateSettingControl(string title, string description, TrackBar trackBar, Label valueLabel, int yPos, int min, int max, int defaultValue)
        {
            Label titleLabel = new Label
            {
                Text = title,
                Location = new Point(20, yPos),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(titleLabel);

            Label descLabel = new Label
            {
                Text = description,
                Location = new Point(20, yPos + 22),
                Size = new Size(300, 15),
                Font = new Font("Segoe UI", 8)
            };
            this.Controls.Add(descLabel);

            trackBar.Location = new Point(20, yPos + 40);
            trackBar.Size = new Size(350, 45);
            trackBar.Minimum = min;
            trackBar.Maximum = max;
            trackBar.Value = defaultValue;
            trackBar.TickFrequency = (max - min) / 10;
            trackBar.ValueChanged += (s, e) => UpdateValueLabel(trackBar, valueLabel, title);
            this.Controls.Add(trackBar);

            valueLabel.Text = defaultValue.ToString();
            valueLabel.Location = new Point(380, yPos + 40);
            valueLabel.Size = new Size(80, 20);
            valueLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(valueLabel);
        }

        private void UpdateValueLabel(TrackBar trackBar, Label label, string settingName)
        {
            if (settingName == "Smoothness")
            {
                double value = trackBar.Value / 100.0;
                label.Text = value.ToString("F2");
            }
            else if (settingName == "Scroll Speed")
            {
                double value = trackBar.Value / 10.0;
                label.Text = value.ToString("F1") + "x";
            }
            else
            {
                label.Text = trackBar.Value.ToString() + "ms";
            }
        }

        private void LoadCurrentSettings()
        {
            smoothnessTrackBar.Value = (int)(engine.Smoothness * 100);
            multiplierTrackBar.Value = (int)(engine.ScrollMultiplier * 10);
            intervalTrackBar.Value = engine.TimerInterval;

            UpdateValueLabel(smoothnessTrackBar, smoothnessValueLabel, "Smoothness");
            UpdateValueLabel(multiplierTrackBar, multiplierValueLabel, "Scroll Speed");
            UpdateValueLabel(intervalTrackBar, intervalValueLabel, "Update Rate");
        }

        private void ThemeToggleButton_Click(object sender, EventArgs e)
        {
            AppTheme newTheme = engine.Theme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            engine.Theme = newTheme;
            ApplyTheme(newTheme);
            engine.SaveSettings();
        }

        private void ApplyTheme(AppTheme theme)
        {
            Color bg, text, secondary, panel, border;
            Color supportPanelBg, supportTextColor, supportLinkColor;

            if (theme == AppTheme.Dark)
            {
                bg = darkBg;
                text = darkText;
                secondary = darkSecondary;
                panel = darkPanel;
                border = darkBorder;
                themeToggleButton.Text = "☀️ Light";
                
                // Dark theme support panel colors
                supportPanelBg = Color.FromArgb(60, 30, 45);  // Dark pink-ish
                supportTextColor = Color.FromArgb(255, 150, 200);  // Light pink
                supportLinkColor = Color.FromArgb(255, 180, 220);  // Lighter pink
            }
            else
            {
                bg = lightBg;
                text = lightText;
                secondary = lightSecondary;
                panel = lightPanel;
                border = lightBorder;
                themeToggleButton.Text = "🌙 Night";
                
                // Light theme support panel colors
                supportPanelBg = Color.FromArgb(255, 240, 245);  // Light pink
                supportTextColor = Color.FromArgb(255, 105, 180);  // Hot pink
                supportLinkColor = Color.FromArgb(255, 105, 180);  // Hot pink
            }

            // Apply to form
            this.BackColor = bg;

            // Apply support panel theme
            supportPanel.BackColor = supportPanelBg;
            supportLabel.ForeColor = supportTextColor;
            donateLink.LinkColor = supportLinkColor;
            donateLink.ActiveLinkColor = supportLinkColor;
            donateLink.VisitedLinkColor = supportLinkColor;

            // Apply to all other controls
            foreach (Control control in this.Controls)
            {
                if (control != supportPanel)
                {
                    ApplyThemeToControl(control, bg, text, secondary, panel, border);
                }
            }

            // Special styling for buttons
            themeToggleButton.BackColor = panel;
            themeToggleButton.ForeColor = text;
            themeToggleButton.FlatAppearance.BorderColor = border;

            resetButton.BackColor = panel;
            resetButton.ForeColor = text;
            resetButton.FlatAppearance.BorderColor = border;

            // Apply button stays blue
            applyButton.BackColor = Color.FromArgb(0, 120, 215);
            applyButton.ForeColor = Color.White;

            // Update trackbar colors
            foreach (Control control in this.Controls)
            {
                if (control is TrackBar trackBar)
                {
                    trackBar.BackColor = bg;
                }
            }
        }

        private void ApplyThemeToControl(Control control, Color bg, Color text, Color secondary, Color panel, Color border)
        {
            if (control is Label label)
            {
                if (label.Font.Bold)
                {
                    label.ForeColor = text;
                }
                else if (label.Font.Size <= 8)
                {
                    label.ForeColor = secondary;
                }
                else
                {
                    label.ForeColor = text;
                }
                label.BackColor = Color.Transparent;
            }
            else if (control is TrackBar)
            {
                control.BackColor = bg;
            }
            else if (control is Panel controlPanel)
            {
                controlPanel.BackColor = panel;
                
                foreach (Control child in controlPanel.Controls)
                {
                    ApplyThemeToControl(child, bg, text, secondary, panel, border);
                }
            }
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            engine.Smoothness = smoothnessTrackBar.Value / 100.0;
            engine.ScrollMultiplier = multiplierTrackBar.Value / 10.0;
            engine.TimerInterval = intervalTrackBar.Value;

            engine.SaveSettings();

            statusLabel.Text = "✓ Settings applied successfully!";
            statusLabel.ForeColor = Color.Green;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += (s, ev) => 
            { 
                statusLabel.Text = ""; 
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            smoothnessTrackBar.Value = 25;
            multiplierTrackBar.Value = 10;
            intervalTrackBar.Value = 8;

            ApplyButton_Click(sender, e);
        }
    }

    public class SmoothScrollEngine : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_CONTROL = 0x11;
        private const int VK_SHIFT = 0x10;
        private const int VK_MENU = 0x12; // Alt key

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        public double Smoothness { get; set; } = 0.25;
        public double ScrollMultiplier { get; set; } = 1.0;
        public int TimerInterval { get; set; } = 8;
        public AppTheme Theme { get; set; } = AppTheme.Light;

        private IntPtr hookID = IntPtr.Zero;
        private LowLevelMouseProc proc;
        private System.Threading.Timer scrollTimer;
        private double targetScroll = 0;
        private double currentScroll = 0;
        private bool isScrolling = false;
        private readonly object scrollLock = new object();
        private bool isRunning = false;

        private string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmoothScroll", "settings.json"
        );

        public SmoothScrollEngine()
        {
            proc = HookCallback;
            scrollTimer = new System.Threading.Timer(ScrollTimerCallback, null, Timeout.Infinite, TimerInterval);
        }

        public void Start()
        {
            if (!isRunning)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hookID = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
                isRunning = true;
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                UnhookWindowsHookEx(hookID);
                hookID = IntPtr.Zero;
                isRunning = false;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                // Check if any modifier keys are pressed (Ctrl, Shift, Alt)
                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                bool altPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;

                // If any modifier key is pressed, don't apply smooth scrolling
                if (ctrlPressed || shiftPressed || altPressed)
                {
                    return CallNextHookEx(hookID, nCode, wParam, lParam);
                }

                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                short delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

                lock (scrollLock)
                {
                    targetScroll += delta * ScrollMultiplier;

                    if (!isScrolling)
                    {
                        isScrolling = true;
                        scrollTimer.Change(0, TimerInterval);
                    }
                }

                return (IntPtr)1;
            }

            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private void ScrollTimerCallback(object state)
        {
            lock (scrollLock)
            {
                double difference = targetScroll - currentScroll;

                if (Math.Abs(difference) < 1.0)
                {
                    currentScroll = targetScroll;
                    isScrolling = false;
                    scrollTimer.Change(Timeout.Infinite, TimerInterval);
                    return;
                }

                double step = difference * Smoothness;
                currentScroll += step;

                int scrollAmount = (int)Math.Round(step);
                if (scrollAmount != 0)
                {
                    POINT cursorPos;
                    if (GetCursorPos(out cursorPos))
                    {
                        IntPtr hWnd = WindowFromPoint(cursorPos);
                        if (hWnd != IntPtr.Zero)
                        {
                            IntPtr wParam = (IntPtr)((scrollAmount << 16) | 0);
                            IntPtr lParam = (IntPtr)((cursorPos.y << 16) | (cursorPos.x & 0xFFFF));
                            SendMessage(hWnd, WM_MOUSEWHEEL, wParam, lParam);
                        }
                    }
                }
            }
        }

        public void SaveSettings()
        {
            try
            {
                string directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var settings = new
                {
                    Smoothness = this.Smoothness,
                    ScrollMultiplier = this.ScrollMultiplier,
                    TimerInterval = this.TimerInterval,
                    Theme = this.Theme.ToString()
                };
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch { }
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    dynamic settings = JsonConvert.DeserializeObject(json);
                    if (settings != null)
                    {
                        if (settings.Smoothness != null)
                            this.Smoothness = (double)settings.Smoothness;
                        if (settings.ScrollMultiplier != null)
                            this.ScrollMultiplier = (double)settings.ScrollMultiplier;
                        if (settings.TimerInterval != null)
                            this.TimerInterval = (int)settings.TimerInterval;
                        if (settings.Theme != null)
                        {
                            string themeStr = (string)settings.Theme;
                            if (Enum.TryParse<AppTheme>(themeStr, out AppTheme theme))
                            {
                                this.Theme = theme;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
            if (scrollTimer != null)
            {
                scrollTimer.Dispose();
            }
        }
    }
}