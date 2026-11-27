using Avalonia.Controls.UnitTests.Utils;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class AllowFocusWhenDisabledTests : ScopedTestBase
    {
        [Fact]
        public void Button_Keeps_Focus_While_Its_Command_Is_Busy()
        {
            using (UnitTestApplication.Start(TestServices.FocusableWindow))
            {
                var executed = 0;
                var busy = false;
                TestCommand? command = null;
                command = new TestCommand(_ => !busy, _ =>
                {
                    ++executed;
                    busy = true;
                    command!.RaiseCanExecuteChanged();
                });
                var target = new Button { Command = command };
                var window = new Window { AllowFocusWhenDisabled = true, Content = target };
                window.Show();

                target.Focus(NavigationMethod.Tab);
                SendKey(window, RawKeyEventType.KeyDown, Key.Space);
                SendKey(window, RawKeyEventType.KeyUp, Key.Space);

                Assert.Equal(1, executed);
                Assert.False(target.IsEffectivelyEnabled);
                Assert.True(target.IsFocused);
                Assert.Contains(":focus-visible", target.Classes);

                // Pressing again while busy neither executes nor presses the button.
                SendKey(window, RawKeyEventType.KeyDown, Key.Space);
                Assert.False(target.IsPressed);
                SendKey(window, RawKeyEventType.KeyUp, Key.Space);
                SendKey(window, RawKeyEventType.KeyDown, Key.Enter);

                Assert.Equal(1, executed);

                busy = false;
                command.RaiseCanExecuteChanged();

                Assert.True(target.IsEffectivelyEnabled);
                Assert.True(target.IsFocused);
            }
        }

        [Fact]
        public void Disabled_Focused_TextBox_Does_Not_Accept_Input()
        {
            using (UnitTestApplication.Start(TestServices.FocusableWindow))
            {
                var target = new TextBox { Text = "abc", CaretIndex = 3 };
                var window = new Window { AllowFocusWhenDisabled = true, Content = target };
                window.Show();

                target.Focus();
                target.IsEnabled = false;

                KeyboardDevice.Instance!.ProcessRawEvent(
                    new RawTextInputEventArgs(KeyboardDevice.Instance, 0, window.GetInputRoot()!, "d"));
                SendKey(window, RawKeyEventType.KeyDown, Key.Back);

                Assert.True(target.IsFocused);
                Assert.Equal("abc", target.Text);

                target.IsEnabled = true;
                KeyboardDevice.Instance.ProcessRawEvent(
                    new RawTextInputEventArgs(KeyboardDevice.Instance, 0, window.GetInputRoot()!, "d"));

                Assert.Equal("abcd", target.Text);
            }
        }

        private static void SendKey(Window window, RawKeyEventType type, Key key)
        {
            KeyboardDevice.Instance!.ProcessRawEvent(
                new RawKeyEventArgs(
                    KeyboardDevice.Instance,
                    0,
                    window.GetInputRoot()!,
                    type,
                    key,
                    RawInputModifiers.None,
                    PhysicalKey.None,
                    null));
        }
    }
}
