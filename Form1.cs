using System.Numerics;

namespace xbox_gamepad
{
    public partial class Form1 : Form
    {
        private ControllerStateManager? _stateManager;
        private System.Windows.Forms.Timer? _renderTimer;
        private const int RenderIntervalMs = 16; // ~60Hz
        
        // Store original styles to restore them when button is released
        private readonly Dictionary<Button, (Color Back, Color Fore)> _buttonOriginalStyles = new();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // Capture original button styles from Designer
            CaptureButtonStyles();

            // Initialize driver and manager
            IControllerDriver driver = new WgiDriver();
            _stateManager = new ControllerStateManager(driver);
            _stateManager.Start();

            // Setup render timer (UI loop)
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = RenderIntervalMs;
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            // Attach Exit button event handler
            _exitButton.Click += ExitButton_Click;
        }

        private void CaptureButtonStyles()
        {
            var buttons = new Button[]
            {
                _aButton, _bButton, _xButton, _yButton,
                _dpadUpButton, _dpadDownButton, _dpadLeftButton, _dpadRightButton,
                _lbButton, _rbButton,
                _viewButton, _menuButton,
                _leftStickButton, _rightStickButton
            };

            foreach (var btn in buttons)
            {
                _buttonOriginalStyles[btn] = (btn.BackColor, btn.ForeColor);
            }
        }

        private void OnRenderTick(object? sender, EventArgs e)
        {
            if (_stateManager == null) return;

            // Pull current state from manager (thread-safe)
            var state = _stateManager.Current;

            // Update UI
            UpdateButtonStates(state);
            UpdateAxisStates(state);
            UpdateStatusAndBattery(state);
        }

        private void UpdateButtonStates(XboxControllerState state)
        {
            SetButtonState(_aButton, state.A);
            SetButtonState(_bButton, state.B);
            SetButtonState(_xButton, state.X);
            SetButtonState(_yButton, state.Y);

            SetButtonState(_dpadUpButton, state.DPadUp);
            SetButtonState(_dpadDownButton, state.DPadDown);
            SetButtonState(_dpadLeftButton, state.DPadLeft);
            SetButtonState(_dpadRightButton, state.DPadRight);

            SetButtonState(_lbButton, state.LB);
            SetButtonState(_rbButton, state.RB);

            SetButtonState(_viewButton, state.ViewButton);
            SetButtonState(_menuButton, state.MenuButton);

            SetButtonState(_leftStickButton, state.LeftStickButton);
            SetButtonState(_rightStickButton, state.RightStickButton);
        }

        private void SetButtonState(Button btn, bool pressed)
        {
            if (!_buttonOriginalStyles.ContainsKey(btn)) return;

            var style = _buttonOriginalStyles[btn];

            if (pressed)
            {
                // Active/Pressed State: High contrast (White background, Black text)
                btn.BackColor = Color.White;
                btn.ForeColor = Color.Black;
                // Optional: Add border or other visual cue
            }
            else
            {
                // Idle State: Restore original branding colors
                btn.BackColor = style.Back;
                btn.ForeColor = style.Fore;
            }
        }

        private void UpdateAxisStates(XboxControllerState state)
        {
            // Update stick positions
            _leftStickPanel.CurrentPosition = state.LeftStick;
            _rightStickPanel.CurrentPosition = state.RightStick;

            // Update stick coordinate values
            _leftStickCoordLabel.Text = $"X: {state.LeftStick.X:F2}  Y: {state.LeftStick.Y:F2}";
            _rightStickCoordLabel.Text = $"X: {state.RightStick.X:F2}  Y: {state.RightStick.Y:F2}";

            // Update trigger values
            _ltLabel.Text = $"LT: {state.LT:F2}";
            _rtLabel.Text = $"RT: {state.RT:F2}";

            _ltProgress.Value = (int)(state.LT * 100);
            _rtProgress.Value = (int)(state.RT * 100);
        }

        private void UpdateStatusAndBattery(XboxControllerState state)
        {
            if (state.IsConnected)
            {
                _statusLabel.Text = "Status: Connected";
                _statusLabel.ForeColor = Color.Green;
            }
            else
            {
                _statusLabel.Text = "Status: Disconnected";
                _statusLabel.ForeColor = Color.Red;
            }

            // Battery status
            string batteryText = state.BatteryStatus switch
            {
                BatteryStatus.Charging => "Charging",
                BatteryStatus.Discharging => "Discharging",
                BatteryStatus.Idle => "Battery OK",
                BatteryStatus.Unknown => "Battery Unknown",
                _ => "Unknown"
            };

            if (state.RemainingLevel >= 0)
            {
                batteryText += $" ({state.RemainingLevel * 100:F0}%)";
            }

            _batteryLabel.Text = batteryText;
            
            // Adjust battery label color based on level
            if (state.BatteryStatus == BatteryStatus.Charging)
            {
                _batteryLabel.ForeColor = Color.Green;
            }
            else if (state.RemainingLevel >= 0 && state.RemainingLevel < 0.2)
            {
                _batteryLabel.ForeColor = Color.Red;
            }
            else
            {
                _batteryLabel.ForeColor = Color.Black;
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _renderTimer?.Stop();
            _renderTimer?.Dispose();
            _stateManager?.Stop();
            _stateManager?.Dispose();
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
