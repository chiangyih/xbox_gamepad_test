using System.Numerics;
using SharpDX.XInput;

namespace xbox_gamepad
{
    /// <summary>
    /// XInput Driver for Xbox Controllers
    /// Uses SharpDX.XInput to detect and read Xbox controller input
    /// </summary>
    public sealed class WgiDriver : IControllerDriver
    {
        private bool _isConnected;
        private Controller? _controller;
        private UserIndex _currentUserIndex;
        private const int MaxFailureCount = 3;
        private int _consecutiveFailures;
        private System.Windows.Forms.Timer? _discoveryTimer;

        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected => _isConnected;

        public WgiDriver()
        {
            _isConnected = false;
            _consecutiveFailures = 0;
            _currentUserIndex = UserIndex.One;
            
            // Try to find an already connected gamepad
            RefreshController();
            
            // Start discovery timer to periodically check for controllers
            _discoveryTimer = new System.Windows.Forms.Timer();
            _discoveryTimer.Interval = 500; // Check every 500ms
            _discoveryTimer.Tick += OnDiscoveryTick;
            _discoveryTimer.Start();
        }

        private void OnDiscoveryTick(object? sender, EventArgs e)
        {
            RefreshController();
        }

        private void RefreshController()
        {
            // Try all user indices to find a connected controller
            for (int i = 0; i < 4; i++)
            {
                var userIndex = (UserIndex)i;
                var controller = new Controller(userIndex);
                
                if (controller.IsConnected)
                {
                    // Found a connected controller
                    if (_currentUserIndex != userIndex || _controller == null)
                    {
                        _currentUserIndex = userIndex;
                        _controller = controller;
                        SetConnectionState(true);
                        System.Diagnostics.Debug.WriteLine($"Controller connected at UserIndex {userIndex}");
                    }
                    return;
                }
            }
            
            // No controller found
            if (_controller != null && _controller.IsConnected == false)
            {
                _controller = null;
                SetConnectionState(false);
                System.Diagnostics.Debug.WriteLine("No controller found");
            }
        }

        private void SetConnectionState(bool connected)
        {
            if (_isConnected != connected)
            {
                _isConnected = connected;
                ConnectionChanged?.Invoke(this, connected);
            }
        }

        public XboxControllerState ReadState()
        {
            var state = new XboxControllerState();
            
            if (_controller == null || !_controller.IsConnected)
            {
                state.IsConnected = false;
                return state;
            }

            try
            {
                State reading;
                if (!_controller.GetState(out reading))
                {
                    state.IsConnected = false;
                    _consecutiveFailures++;
                    
                    if (_consecutiveFailures >= MaxFailureCount)
                    {
                        _controller = null;
                        SetConnectionState(false);
                    }
                    
                    return state;
                }
                
                state.IsConnected = true;
                
                // Buttons
                var buttons = reading.Gamepad.Buttons;
                state.A = (buttons & GamepadButtonFlags.A) != 0;
                state.B = (buttons & GamepadButtonFlags.B) != 0;
                state.X = (buttons & GamepadButtonFlags.X) != 0;
                state.Y = (buttons & GamepadButtonFlags.Y) != 0;
                
                // D-Pad
                state.DPadUp = (buttons & GamepadButtonFlags.DPadUp) != 0;
                state.DPadDown = (buttons & GamepadButtonFlags.DPadDown) != 0;
                state.DPadLeft = (buttons & GamepadButtonFlags.DPadLeft) != 0;
                state.DPadRight = (buttons & GamepadButtonFlags.DPadRight) != 0;
                
                // Shoulder buttons
                state.LB = (buttons & GamepadButtonFlags.LeftShoulder) != 0;
                state.RB = (buttons & GamepadButtonFlags.RightShoulder) != 0;
                
                // System buttons
                state.ViewButton = (buttons & GamepadButtonFlags.Back) != 0;
                state.MenuButton = (buttons & GamepadButtonFlags.Start) != 0;
                
                // Stick buttons
                state.LeftStickButton = (buttons & GamepadButtonFlags.LeftThumb) != 0;
                state.RightStickButton = (buttons & GamepadButtonFlags.RightThumb) != 0;
                
                // Analog sticks (convert from range [-32768, 32767] to [-1, 1])
                float leftX = NormalizeStickAxis(reading.Gamepad.LeftThumbX);
                float leftY = NormalizeStickAxis(reading.Gamepad.LeftThumbY);
                float rightX = NormalizeStickAxis(reading.Gamepad.RightThumbX);
                float rightY = NormalizeStickAxis(reading.Gamepad.RightThumbY);
                
                state.LeftStick = new Vector2(leftX, leftY);
                state.RightStick = new Vector2(rightX, rightY);
                
                // Triggers (convert from range [0, 255] to [0, 1])
                state.LT = reading.Gamepad.LeftTrigger / 255f;
                state.RT = reading.Gamepad.RightTrigger / 255f;
                
                // Battery information
                try
                {
                    BatteryInformation batteryInfo = default;
                    if (_controller.GetBatteryInformation(BatteryDeviceType.Gamepad) is BatteryInformation info)
                    {
                        batteryInfo = info;
                    }
                    
                    state.BatteryStatus = batteryInfo.BatteryLevel switch
                    {
                        BatteryLevel.Full => xbox_gamepad.BatteryStatus.Idle,
                        BatteryLevel.Medium => xbox_gamepad.BatteryStatus.Discharging,
                        BatteryLevel.Low => xbox_gamepad.BatteryStatus.Discharging,
                        _ => xbox_gamepad.BatteryStatus.Unknown
                    };

                    // Estimate remaining level based on BatteryLevel
                    state.RemainingLevel = batteryInfo.BatteryLevel switch
                    {
                        BatteryLevel.Full => 1.0,
                        BatteryLevel.Medium => 0.5,
                        BatteryLevel.Low => 0.25,
                        _ => -1.0
                    };
                }
                catch
                {
                    // Battery info might not be available on all systems
                }
                
                _consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                System.Diagnostics.Debug.WriteLine($"Error reading gamepad state: {ex.Message}");
                
                if (_consecutiveFailures >= MaxFailureCount)
                {
                    _controller = null;
                    SetConnectionState(false);
                }
                
                state.IsConnected = false;
            }

            return state;
        }

        private float NormalizeStickAxis(short rawValue)
        {
            // XInput uses range [-32768, 32767]
            if (rawValue > 0)
            {
                return (float)rawValue / 32767f;
            }
            else
            {
                return (float)rawValue / 32768f;
            }
        }

        public void Dispose()
        {
            if (_discoveryTimer != null)
            {
                _discoveryTimer.Stop();
                _discoveryTimer.Dispose();
                _discoveryTimer = null;
            }
            _controller = null;
        }
    }
}
