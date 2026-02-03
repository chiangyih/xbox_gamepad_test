using System.Numerics;

namespace xbox_gamepad
{
    /// <summary>
    /// Mock driver for testing purposes
    /// </summary>
    public sealed class MockControllerDriver : IControllerDriver
    {
        private bool _isConnected = true;
        private XboxControllerState _mockState = new();

        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected => _isConnected;

        public MockControllerDriver(bool isConnected = true)
        {
            _isConnected = isConnected;
            _mockState.IsConnected = isConnected;
        }

        public void SetConnected(bool connected)
        {
            if (_isConnected != connected)
            {
                _isConnected = connected;
                _mockState.IsConnected = connected;
                ConnectionChanged?.Invoke(this, connected);
            }
        }

        public void SetButtonState(string button, bool pressed)
        {
            switch (button.ToLower())
            {
                case "a": _mockState.A = pressed; break;
                case "b": _mockState.B = pressed; break;
                case "x": _mockState.X = pressed; break;
                case "y": _mockState.Y = pressed; break;
                case "view": _mockState.ViewButton = pressed; break;
                case "menu": _mockState.MenuButton = pressed; break;
                case "lb": _mockState.LB = pressed; break;
                case "rb": _mockState.RB = pressed; break;
            }
        }

        public void SetStickPosition(string stick, float x, float y)
        {
            switch (stick.ToLower())
            {
                case "left":
                    _mockState.LeftStick = new Vector2(x, y);
                    break;
                case "right":
                    _mockState.RightStick = new Vector2(x, y);
                    break;
            }
        }

        public void SetTrigger(string trigger, float value)
        {
            switch (trigger.ToLower())
            {
                case "lt":
                    _mockState.LT = Math.Clamp(value, 0.0f, 1.0f);
                    break;
                case "rt":
                    _mockState.RT = Math.Clamp(value, 0.0f, 1.0f);
                    break;
            }
        }

        public XboxControllerState ReadState()
        {
            return _mockState.Clone();
        }
    }
}
