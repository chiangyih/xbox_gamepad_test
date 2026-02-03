using System.Numerics;

namespace xbox_gamepad
{
    public enum BatteryStatus
    {
        Unknown = 0,
        Charging,
        Discharging,
        Idle,
        NotPresent
    }

    public sealed class XboxControllerState
    {
        public bool IsConnected { get; set; }

        // Buttons
        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }

        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }

        public bool LB { get; set; }
        public bool RB { get; set; }

        public bool ViewButton { get; set; }
        public bool MenuButton { get; set; }
        public bool XboxButton { get; set; }
        public bool ShareButton { get; set; }

        public bool LeftStickButton { get; set; }
        public bool RightStickButton { get; set; }

        // Axes
        public Vector2 LeftStick { get; set; }
        public Vector2 RightStick { get; set; }
        public float LT { get; set; }
        public float RT { get; set; }

        // Battery
        public BatteryStatus BatteryStatus { get; set; }
        public double RemainingLevel { get; set; }

        public XboxControllerState()
        {
            IsConnected = false;
            BatteryStatus = BatteryStatus.Unknown;
            RemainingLevel = -1;
        }

        public XboxControllerState Clone()
        {
            return new XboxControllerState
            {
                IsConnected = this.IsConnected,
                A = this.A,
                B = this.B,
                X = this.X,
                Y = this.Y,
                DPadUp = this.DPadUp,
                DPadDown = this.DPadDown,
                DPadLeft = this.DPadLeft,
                DPadRight = this.DPadRight,
                LB = this.LB,
                RB = this.RB,
                ViewButton = this.ViewButton,
                MenuButton = this.MenuButton,
                XboxButton = this.XboxButton,
                ShareButton = this.ShareButton,
                LeftStickButton = this.LeftStickButton,
                RightStickButton = this.RightStickButton,
                LeftStick = this.LeftStick,
                RightStick = this.RightStick,
                LT = this.LT,
                RT = this.RT,
                BatteryStatus = this.BatteryStatus,
                RemainingLevel = this.RemainingLevel
            };
        }
    }
}
