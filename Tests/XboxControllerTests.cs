using System.Numerics;

namespace xbox_gamepad.Tests
{
    /// <summary>
    /// Unit tests for Xbox Controller functionality
    /// Covers TC-01, TC-04, TC-06 from spec.md
    /// </summary>
    public class XboxControllerTests
    {
        [TestCase]
        public void TC01_ConnectionEventTriggersDisconnect()
        {
            // Test FR-01: Event-driven connection management
            var driver = new MockControllerDriver(true);
            var manager = new ControllerStateManager(driver);
            manager.Start();

            // Initially connected
            System.Threading.Thread.Sleep(100);
            var state1 = manager.Current;
            Assert.True(state1.IsConnected, "Should be initially connected");

            // Disconnect
            driver.SetConnected(false);
            System.Threading.Thread.Sleep(100);
            var state2 = manager.Current;
            Assert.False(state2.IsConnected, "Should be disconnected after event");

            manager.Stop();
            manager.Dispose();
        }

        [TestCase]
        public void TC04_StickRadialDeadZoneAndRescaling()
        {
            // Test FR-04: Radial dead zone + rescaling
            var driver = new MockControllerDriver(true);
            var manager = new ControllerStateManager(driver);
            manager.Start();

            // Test 1: Below dead zone (0.10)
            driver.SetStickPosition("left", 0.05f, 0.05f); // magnitude ~= 0.0707
            System.Threading.Thread.Sleep(50);
            var state1 = manager.Current;
            Assert.AreEqual(0.0f, state1.LeftStick.X, 0.01f, "Below dead zone should be 0");
            Assert.AreEqual(0.0f, state1.LeftStick.Y, 0.01f, "Below dead zone should be 0");

            // Test 2: Just above dead zone - should rescale smoothly
            driver.SetStickPosition("left", 0.07f, 0.07f); // magnitude ~= 0.099
            System.Threading.Thread.Sleep(50);
            var state2 = manager.Current;
            float mag2 = state2.LeftStick.Length();
            Assert.True(mag2 > 0.01f && mag2 < 0.15f, $"Just above dead zone should rescale smoothly, got {mag2}");

            // Test 3: Full deflection
            driver.SetStickPosition("left", 1.0f, 0.0f);
            System.Threading.Thread.Sleep(50);
            var state3 = manager.Current;
            Assert.AreEqual(1.0f, state3.LeftStick.X, 0.02f, "Full deflection X should be ~1.0");
            Assert.AreEqual(0.0f, state3.LeftStick.Y, 0.02f, "Full deflection Y should be ~0.0");

            // Test 4: Circular motion test - draw a circle and verify no axis discontinuity
            bool hasAxisDiscontinuity = false;
            float prevDistance = 0;
            for (int angle = 0; angle < 360; angle += 10)
            {
                float rad = angle * (float)Math.PI / 180;
                float x = (float)Math.Cos(rad) * 0.6f; // ~0.6 magnitude
                float y = (float)Math.Sin(rad) * 0.6f;
                driver.SetStickPosition("left", x, y);
                System.Threading.Thread.Sleep(10);

                var state = manager.Current;
                float distance = state.LeftStick.Length();

                // Check for discontinuity (large jump)
                if (prevDistance > 0 && Math.Abs(distance - prevDistance) > 0.25f)
                {
                    hasAxisDiscontinuity = true;
                    break;
                }
                prevDistance = distance;
            }
            Assert.False(hasAxisDiscontinuity, "Circular motion should be smooth without axis discontinuity");

            manager.Stop();
            manager.Dispose();
        }

        [TestCase]
        public void TC06_TriggerAntiJitter()
        {
            // Test FR-06: Trigger anti-jitter dead zone
            var driver = new MockControllerDriver(true);
            var manager = new ControllerStateManager(driver);
            manager.Start();

            // Test 1: Below trigger dead zone (0.02)
            driver.SetTrigger("lt", 0.01f);
            System.Threading.Thread.Sleep(50);
            var state1 = manager.Current;
            Assert.AreEqual(0.0f, state1.LT, 0.001f, "Below trigger dead zone should be 0");

            // Test 2: Just above trigger dead zone - should rescale
            driver.SetTrigger("lt", 0.05f); // Should rescale to ~(0.05-0.02)/(1-0.02) = 0.0306
            System.Threading.Thread.Sleep(50);
            var state2 = manager.Current;
            Assert.True(state2.LT > 0.01f && state2.LT < 0.1f, $"Just above dead zone should rescale, got {state2.LT}");

            // Test 3: Full trigger pull
            driver.SetTrigger("rt", 1.0f);
            System.Threading.Thread.Sleep(50);
            var state3 = manager.Current;
            Assert.AreEqual(1.0f, state3.RT, 0.02f, "Full trigger pull should be ~1.0");

            manager.Stop();
            manager.Dispose();
        }

        [TestCase]
        public void TC02_ABXYButtons()
        {
            // Test FR-02: ABXY button states
            var driver = new MockControllerDriver(true);
            var manager = new ControllerStateManager(driver);
            manager.Start();

            driver.SetButtonState("a", true);
            System.Threading.Thread.Sleep(50);
            var state = manager.Current;
            Assert.True(state.A, "A button should be pressed");

            driver.SetButtonState("b", true);
            driver.SetButtonState("x", true);
            driver.SetButtonState("y", true);
            System.Threading.Thread.Sleep(50);
            var state2 = manager.Current;
            Assert.True(state2.B && state2.X && state2.Y, "B, X, Y buttons should be pressed");

            manager.Stop();
            manager.Dispose();
        }

        [TestCase]
        public void TC07_LogicFrequency60Hz()
        {
            // Test FR-07: Logic loop at 60Hz
            var driver = new MockControllerDriver(true);
            var manager = new ControllerStateManager(driver);
            manager.Start();

            int updateCount = 0;
            Vector2 lastStick = Vector2.Zero;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            driver.SetStickPosition("left", 0.5f, 0.5f);

            // Collect samples over 1 second
            while (stopwatch.ElapsedMilliseconds < 1000)
            {
                var state = manager.Current;
                if (state.LeftStick != lastStick)
                {
                    updateCount++;
                    lastStick = state.LeftStick;
                }
                System.Threading.Thread.Sleep(5);
            }

            stopwatch.Stop();
            // At 60Hz, we should get approximately 60 updates per second
            // Allow some tolerance (50-70 updates)
            Assert.True(updateCount >= 50 && updateCount <= 70, 
                $"Logic should update ~60 times per second, got {updateCount} updates");

            manager.Stop();
            manager.Dispose();
        }

        [TestCase]
        public void TestStateCloning()
        {
            // Verify XboxControllerState cloning works correctly
            var original = new XboxControllerState
            {
                IsConnected = true,
                A = true,
                LeftStick = new Vector2(0.5f, 0.5f),
                LT = 0.75f,
                BatteryStatus = BatteryStatus.Discharging,
                RemainingLevel = 0.8
            };

            var cloned = original.Clone();

            Assert.AreEqual(original.IsConnected, cloned.IsConnected);
            Assert.AreEqual(original.A, cloned.A);
            Assert.AreEqual(original.LeftStick, cloned.LeftStick);
            Assert.AreEqual(original.LT, cloned.LT);
            Assert.AreEqual(original.BatteryStatus, cloned.BatteryStatus, "BatteryStatus should be cloned");
            Assert.AreEqual(original.RemainingLevel, cloned.RemainingLevel, "RemainingLevel should be cloned");

            // Verify it's a true copy (different instance)
            cloned.A = false;
            Assert.True(original.A, "Clone should not affect original");
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestCaseAttribute : Attribute { }

    public static class Assert
    {
        public static void True(bool condition, string message = "")
        {
            if (!condition) throw new AssertionException($"Expected true, got false. {message}");
        }

        public static void False(bool condition, string message = "")
        {
            if (condition) throw new AssertionException($"Expected false, got true. {message}");
        }

        public static void AreEqual(bool expected, bool actual, string message = "")
        {
            if (expected != actual)
                throw new AssertionException($"Expected {expected}, got {actual}. {message}");
        }

        public static void AreEqual(float expected, float actual, float tolerance, string message = "")
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new AssertionException($"Expected {expected} กำ{tolerance}, got {actual}. {message}");
        }

        public static void AreEqual(Vector2 expected, Vector2 actual, string message = "")
        {
            if (expected != actual)
                throw new AssertionException($"Expected {expected}, got {actual}. {message}");
        }

        public static void AreEqual(BatteryStatus expected, BatteryStatus actual, string message = "")
        {
            if (expected != actual)
                throw new AssertionException($"Expected {expected}, got {actual}. {message}");
        }

        public static void AreEqual(double expected, double actual, string message = "")
        {
            if (!expected.Equals(actual))
                throw new AssertionException($"Expected {expected}, got {actual}. {message}");
        }
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
