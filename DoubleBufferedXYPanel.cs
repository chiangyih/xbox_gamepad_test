using System.Numerics;

namespace xbox_gamepad
{
    public sealed class DoubleBufferedXYPanel : Panel
    {
        private Vector2 _currentPosition = Vector2.Zero;
        private int CenterX => Width / 2;
        private int CenterY => Height / 2;
        private int StickRadius => (int)(Math.Min(Width, Height) * 0.4f);
        private int DotRadius => (int)(Math.Min(Width, Height) * 0.04f);

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Vector2 CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                Invalidate();
            }
        }

        public DoubleBufferedXYPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Size = new Size(200, 200);
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw background
            e.Graphics.Clear(BackColor);

            // Draw dead zone circle (light gray)
            DrawDeadZoneCircle(e.Graphics);

            // Draw max range circle
            DrawMaxRangeCircle(e.Graphics);

            // Draw crosshairs
            DrawCrosshairs(e.Graphics);

            // Draw current position dot
            DrawPositionDot(e.Graphics);
        }

        private void DrawDeadZoneCircle(Graphics g)
        {
            const float deadZone = 0.10f;
            int radius = (int)(StickRadius * deadZone);

            using (var pen = new Pen(Color.LightGray, 1))
            {
                g.DrawEllipse(pen, CenterX - radius, CenterY - radius, radius * 2, radius * 2);
            }
        }

        private void DrawMaxRangeCircle(Graphics g)
        {
            using (var pen = new Pen(Color.LightBlue, 1))
            {
                g.DrawEllipse(pen, CenterX - StickRadius, CenterY - StickRadius, StickRadius * 2, StickRadius * 2);
            }
        }

        private void DrawCrosshairs(Graphics g)
        {
            using (var pen = new Pen(Color.LightGray, 1))
            {
                // Horizontal
                g.DrawLine(pen, CenterX - StickRadius, CenterY, CenterX + StickRadius, CenterY);
                // Vertical
                g.DrawLine(pen, CenterX, CenterY - StickRadius, CenterX, CenterY + StickRadius);
            }
        }

        private void DrawPositionDot(Graphics g)
        {
            int x = CenterX + (int)(_currentPosition.X * StickRadius);
            int y = CenterY - (int)(_currentPosition.Y * StickRadius); // Invert Y for screen coordinates

            using (var brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush, x - DotRadius, y - DotRadius, DotRadius * 2, DotRadius * 2);
            }

            using (var pen = new Pen(Color.DarkRed, 2))
            {
                g.DrawEllipse(pen, x - DotRadius, y - DotRadius, DotRadius * 2, DotRadius * 2);
            }
        }
    }
}
