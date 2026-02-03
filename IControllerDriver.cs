namespace xbox_gamepad
{
    public interface IControllerDriver
    {
        bool IsConnected { get; }
        XboxControllerState ReadState();
        event EventHandler<bool>? ConnectionChanged;
    }
}
