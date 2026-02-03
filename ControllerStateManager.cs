using System.Numerics;

namespace xbox_gamepad
{
    public sealed class ControllerStateManager : IDisposable
    {
        private readonly IControllerDriver _driver;
        private readonly object _stateLock = new();
        private XboxControllerState _currentState = new();
        private System.Windows.Forms.Timer? _logicTimer;
        private bool _isRunning;

        // Configuration
        private const float StickDeadZone = 0.10f;
        private const float TriggerDeadZone = 0.02f;
        private const int LogicHz = 60;
        private const int LogicTickMs = 1000 / LogicHz;

        public XboxControllerState Current
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentState.Clone();
                }
            }
        }

        public ControllerStateManager(IControllerDriver driver)
        {
            _driver = driver;
            _isRunning = false;
            _driver.ConnectionChanged += OnDriverConnectionChanged;
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _logicTimer = new System.Windows.Forms.Timer();
            _logicTimer.Interval = LogicTickMs;
            _logicTimer.Tick += OnLogicTick;
            _logicTimer.Start();
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            if (_logicTimer != null)
            {
                _logicTimer.Stop();
                _logicTimer.Dispose();
                _logicTimer = null;
            }
        }

        private void OnLogicTick(object? sender, EventArgs e)
        {
            // Read raw state from driver
            var rawState = _driver.ReadState();

            // Normalize and update current state
            var normalizedState = NormalizeState(rawState);

            lock (_stateLock)
            {
                _currentState = normalizedState;
            }
        }

        private XboxControllerState NormalizeState(XboxControllerState rawState)
        {
            var normalized = rawState.Clone();

            // Apply radial dead zone + rescaling to sticks
            normalized.LeftStick = NormalizeStick(rawState.LeftStick, StickDeadZone);
            normalized.RightStick = NormalizeStick(rawState.RightStick, StickDeadZone);

            // Apply trigger anti-jitter dead zone
            normalized.LT = NormalizeTrigger(rawState.LT, TriggerDeadZone);
            normalized.RT = NormalizeTrigger(rawState.RT, TriggerDeadZone);

            return normalized;
        }

        private Vector2 NormalizeStick(Vector2 rawStick, float deadZone)
        {
            float magnitude = rawStick.Length();

            // Below dead zone
            if (magnitude < deadZone)
            {
                return Vector2.Zero;
            }

            // Rescaling to avoid jump
            float scaled = (magnitude - deadZone) / (1.0f - deadZone);
            Vector2 direction = Vector2.Normalize(rawStick);

            // Clamp to [0, 1] in case of floating point errors
            scaled = Math.Clamp(scaled, 0.0f, 1.0f);

            return direction * scaled;
        }

        private float NormalizeTrigger(float rawValue, float deadZone)
        {
            // Clamp to [0, 1]
            rawValue = Math.Clamp(rawValue, 0.0f, 1.0f);

            // Anti-jitter: values below dead zone become 0
            if (rawValue < deadZone)
            {
                return 0.0f;
            }

            // Optional: rescale to avoid jump (v1.2 feature)
            return (rawValue - deadZone) / (1.0f - deadZone);
        }

        private void OnDriverConnectionChanged(object? sender, bool isConnected)
        {
            lock (_stateLock)
            {
                if (!isConnected)
                {
                    _currentState = new XboxControllerState { IsConnected = false };
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _driver.ConnectionChanged -= OnDriverConnectionChanged;
        }
    }
}
