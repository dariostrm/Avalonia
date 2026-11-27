using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class InputElement_FocusVisible
    {
        [Fact]
        public void Unspecified_Focus_Is_Not_Visible_Without_Input()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (_, target, _) = CreateTarget();

                target.Focus();

                Assert.True(target.IsFocused);
                Assert.False(IsFocusVisible(target));
            }
        }

        [Fact]
        public void Unspecified_Focus_Is_Visible_After_Key_Input()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessKey(root, Key.A);
                target.Focus();

                Assert.True(IsFocusVisible(target));
            }
        }

        [Theory]
        [InlineData(RawPointerEventType.LeftButtonDown)]
        [InlineData(RawPointerEventType.RightButtonUp)]
        [InlineData(RawPointerEventType.NonClientLeftButtonDown)]
        [InlineData(RawPointerEventType.Wheel)]
        [InlineData(RawPointerEventType.TouchBegin)]
        [InlineData(RawPointerEventType.Magnify)]
        [InlineData(RawPointerEventType.Move)]
        public void Unspecified_Focus_Is_Not_Visible_After_Pointer_Input(RawPointerEventType type)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessPointer(root, RawPointerEventType.Move, new Point(1, 1));
                ProcessKey(root, Key.A);
                ProcessPointer(root, type, new Point(2, 2));
                target.Focus();

                Assert.False(IsFocusVisible(target));
            }
        }

        [Fact]
        public void Pointer_Move_Without_Position_Change_Is_Ignored()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessPointer(root, RawPointerEventType.Move, new Point(1, 1));
                ProcessKey(root, Key.A);
                ProcessPointer(root, RawPointerEventType.Move, new Point(1, 1));
                target.Focus();

                Assert.True(IsFocusVisible(target));
            }
        }

        [Theory]
        [InlineData(RawPointerEventType.LeaveWindow)]
        [InlineData(RawPointerEventType.CancelCapture)]
        public void Pointer_Events_Without_User_Input_Are_Ignored(RawPointerEventType type)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessKey(root, Key.A);
                ProcessPointer(root, type, new Point(5, 5));
                target.Focus();

                Assert.True(IsFocusVisible(target));
            }
        }

        [Fact]
        public void Text_Input_Is_Ignored()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessPointer(root, RawPointerEventType.LeftButtonDown, new Point(1, 1));
                GetInputManager().ProcessInput(new RawTextInputEventArgs(new KeyboardDevice(), 0, root, "a"));
                target.Focus();

                Assert.False(IsFocusVisible(target));
            }
        }

        [Theory]
        [InlineData(Key.Tab, true)]
        [InlineData(Key.Down, true)]
        [InlineData(Key.Back, false)]
        [InlineData(Key.Enter, false)]
        [InlineData(Key.A, false)]
        public void Only_Focus_Navigation_Keys_Count_While_Input_Pane_Is_Open(Key key, bool expected)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessPointer(root, RawPointerEventType.TouchBegin, new Point(1, 1));
                ProcessKey(root, key, isInputPaneOpen: true);
                target.Focus();

                Assert.Equal(expected, IsFocusVisible(target));
            }
        }

        [Fact]
        public void Explicit_Pointer_Focus_Is_Not_Visible_After_Key_Input()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessKey(root, Key.A);
                target.Focus(NavigationMethod.Pointer);

                Assert.False(IsFocusVisible(target));
            }
        }

        [Theory]
        [InlineData(NavigationMethod.Tab)]
        [InlineData(NavigationMethod.Directional)]
        public void Explicit_Keyboard_Focus_Is_Visible_After_Pointer_Input(NavigationMethod method)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, _) = CreateTarget();

                ProcessPointer(root, RawPointerEventType.LeftButtonDown, new Point(1, 1));
                target.Focus(method);

                Assert.True(IsFocusVisible(target));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Restoring_Focus_Scope_Keeps_Focus_Visible_State(bool visible)
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var (root, target, other) = CreateTarget();
                var focusManager = (FocusManager)root.FocusManager;

                ProcessKey(root, Key.A);
                target.Focus(visible ? NavigationMethod.Tab : NavigationMethod.Pointer);
                Assert.Equal(visible, IsFocusVisible(target));

                // Window deactivated, then input from the other device. The stored state must win.
                KeyboardDevice.Instance!.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, false);
                Assert.False(target.IsFocused);
                if (visible)
                    ProcessPointer(root, RawPointerEventType.LeftButtonDown, new Point(3, 3));
                else
                    ProcessKey(root, Key.A);

                // Window activated again.
                focusManager.SetFocusScope(root);

                Assert.True(target.IsFocused);
                Assert.False(other.IsFocused);
                Assert.Equal(visible, IsFocusVisible(target));
            }
        }

        private static (TestRoot root, Button target, Button other) CreateTarget()
        {
            var target = new Button();
            var other = new Button();
            var root = new TestRoot
            {
                Child = new StackPanel { Children = { target, other } }
            };
            return (root, target, other);
        }

        private static bool IsFocusVisible(Control control) => control.Classes.Contains(":focus-visible");

        private static InputManager GetInputManager() =>
            (InputManager)AvaloniaLocator.Current.GetRequiredService<IInputManager>();

        private static void ProcessKey(IInputRoot root, Key key, bool isInputPaneOpen = false)
        {
            // A separate device, so the key is not routed to the focused element.
            GetInputManager().ProcessInput(new RawKeyEventArgs(
                new KeyboardDevice(), 0, root, RawKeyEventType.KeyDown, key,
                RawInputModifiers.None, PhysicalKey.None, null)
            {
                IsInputPaneOpen = isInputPaneOpen
            });
        }

        private static void ProcessPointer(IInputRoot root, RawPointerEventType type, Point position)
        {
            GetInputManager().ProcessInput(new RawPointerEventArgs(
                new NullInputDevice(), 0, root, type, position, RawInputModifiers.None));
        }

        private class NullInputDevice : IInputDevice
        {
            public void ProcessRawEvent(RawInputEventArgs ev)
            {
            }
        }
    }
}
