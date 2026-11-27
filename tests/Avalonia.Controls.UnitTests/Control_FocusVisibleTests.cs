using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class Control_FocusVisibleTests : ScopedTestBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Unspecified_Focus_Adds_Focus_Adorner_After_Key_Input_Only(bool keyboard)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var root = new TestRoot { Child = new VisualLayerManager { Child = target } };
                var adornerLayer = AdornerLayer.GetAdornerLayer(target)!;
                adornerLayer.DefaultFocusAdorner = new FuncTemplate<Control>(() => new Border());

                SendInput(root, keyboard);
                target.Focus();

                Assert.True(target.IsFocused);
                Assert.Equal(keyboard ? 1 : 0, adornerLayer.Children.Count);
            }
        }

        [Fact]
        public void TextBox_Unspecified_Focus_After_Key_Input_Does_Not_Select_All()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new TextBox { Text = "abc" };
                var root = new TestRoot { Child = target };

                SendInput(root, keyboard: true);
                target.Focus();

                Assert.True(target.Classes.Contains(":focus-visible"));
                Assert.Equal(target.SelectionStart, target.SelectionEnd);
            }
        }

        internal static void SendInput(IInputRoot root, bool keyboard)
        {
            var inputManager = (InputManager)AvaloniaLocator.Current.GetRequiredService<IInputManager>();

            // Separate devices, so the input is not routed to the focused element.
            RawInputEventArgs e = keyboard
                ? new RawKeyEventArgs(new KeyboardDevice(), 0, root, RawKeyEventType.KeyDown, Key.A,
                    RawInputModifiers.None, PhysicalKey.None, null)
                : new RawPointerEventArgs(new NullInputDevice(), 0, root, RawPointerEventType.LeftButtonDown,
                    new Point(1, 1), RawInputModifiers.None);

            inputManager.ProcessInput(e);
        }

        private class NullInputDevice : IInputDevice
        {
            public void ProcessRawEvent(RawInputEventArgs ev)
            {
            }
        }
    }
}
