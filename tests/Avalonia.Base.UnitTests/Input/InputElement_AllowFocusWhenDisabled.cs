using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class InputElement_AllowFocusWhenDisabled : ScopedTestBase
    {
        [Fact]
        public void Focus_Is_Kept_When_Control_Is_Disabled()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };

                target.Focus();
                target.IsEnabled = false;

                Assert.True(target.IsFocused);
                Assert.Same(target, root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Is_Kept_When_Control_Is_Effectively_Disabled()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var container = new Border { Child = target };
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = container };

                target.Focus();
                container.IsEnabled = false;

                Assert.True(target.IsFocused);
                Assert.True(container.IsKeyboardFocusWithin);
                Assert.Same(target, root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Disabled_Control_Can_Receive_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button { IsEnabled = false };
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };

                Assert.True(target.Focus());
                Assert.Same(target, root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Disabled_Control_Cannot_Receive_Focus_When_Not_Allowed_Locally()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button { IsEnabled = false, AllowFocusWhenDisabled = false };
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };

                Assert.False(target.Focus());
                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Is_Cleared_When_AllowFocusWhenDisabled_Is_Turned_Off_While_Disabled()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };

                target.Focus();
                target.IsEnabled = false;
                root.AllowFocusWhenDisabled = false;

                Assert.False(target.IsFocused);
                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Focus_Is_Still_Cleared_When_Control_Is_Hidden()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };

                target.Focus();
                target.IsEnabled = false;
                target.IsVisible = false;

                Assert.Null(root.FocusManager.GetFocusedElement());
            }
        }

        [Fact]
        public void Key_Input_Goes_To_First_Enabled_Ancestor_Of_Disabled_Focused_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var inner = new Border { Child = target };
                var outer = new Border { Child = inner };
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = outer };
                var raised = new List<(string Handler, object? Source)>();

                target.KeyDown += (_, e) => raised.Add(("target", e.Source));
                target.TextInput += (_, e) => raised.Add(("target", e.Source));
                inner.KeyDown += (_, e) => raised.Add(("inner", e.Source));
                outer.KeyDown += (_, e) => raised.Add(("outer", e.Source));
                outer.TextInput += (_, e) => raised.Add(("outer", e.Source));

                target.Focus();
                inner.IsEnabled = false;

                KeyDown(root, Key.A);
                TextInput(root, "a");

                Assert.Same(target, root.FocusManager.GetFocusedElement());
                Assert.Equal(new[] { ("outer", (object?)outer), ("outer", outer) }, raised);
            }
        }

        [Fact]
        public void Key_Input_Goes_To_Focused_Control_When_It_Is_Enabled_Again()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };
                var raised = 0;

                target.KeyDown += (_, _) => ++raised;
                target.Focus();
                target.IsEnabled = false;
                KeyDown(root, Key.A);
                target.IsEnabled = true;
                KeyDown(root, Key.A);

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void KeyBindings_Of_Disabled_Focused_Control_Are_Not_Invoked()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var container = new Border { Child = target };
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = container };
                var targetRaised = 0;
                var containerRaised = 0;

                target.KeyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture(Key.O, KeyModifiers.Control),
                    Command = new Utilities.DelegateCommand(() => ++targetRaised),
                });
                container.KeyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture(Key.O, KeyModifiers.Control),
                    Command = new Utilities.DelegateCommand(() => ++containerRaised),
                });

                target.Focus();
                target.IsEnabled = false;
                KeyDown(root, Key.O, RawInputModifiers.Control);

                Assert.Equal(0, targetRaised);
                Assert.Equal(1, containerRaised);
            }
        }

        [Fact]
        public void Disabling_Focused_Control_Requeries_Text_Input_Client()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new Button();
                var root = new TestRoot { AllowFocusWhenDisabled = true, Child = target };
                var requeried = 0;

                InputMethod.AddTextInputMethodClientRequeryRequestedHandler(root, (_, _) => ++requeried);
                target.Focus();
                target.IsEnabled = false;
                target.IsEnabled = true;

                Assert.Equal(2, requeried);
            }
        }

        [Fact]
        public void Tab_Skips_Disabled_Controls_By_Default()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var first = new Button();
                var disabled = new Button { IsEnabled = false };
                var last = new Button();
                var root = new TestRoot
                {
                    AllowFocusWhenDisabled = true,
                    Child = new StackPanel { Children = { first, disabled, last } }
                };

                Assert.Same(last, KeyboardNavigationHandler.GetNext(first, NavigationDirection.Next));
                Assert.Same(first, KeyboardNavigationHandler.GetNext(last, NavigationDirection.Previous));
            }
        }

        [Fact]
        public void Tab_Stops_At_Disabled_Controls_When_IsTabStopWhenDisabled()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var first = new Button();
                var disabled = new Button { IsEnabled = false };
                var last = new Button();
                var root = new TestRoot
                {
                    AllowFocusWhenDisabled = true,
                    IsTabStopWhenDisabled = true,
                    Child = new StackPanel { Children = { first, disabled, last } }
                };

                Assert.Same(disabled, KeyboardNavigationHandler.GetNext(first, NavigationDirection.Next));
                Assert.Same(disabled, KeyboardNavigationHandler.GetNext(last, NavigationDirection.Previous));
            }
        }

        [Fact]
        public void Tab_Stops_At_Children_Of_Disabled_Container_When_IsTabStopWhenDisabled()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var first = new Button();
                var child = new Button();
                var last = new Button();
                var root = new TestRoot
                {
                    AllowFocusWhenDisabled = true,
                    IsTabStopWhenDisabled = true,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            first,
                            new StackPanel { IsEnabled = false, Children = { child } },
                            last
                        }
                    }
                };

                Assert.Same(child, KeyboardNavigationHandler.GetNext(first, NavigationDirection.Next));
            }
        }

        [Fact]
        public void IsTabStopWhenDisabled_Has_No_Effect_Without_AllowFocusWhenDisabled()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var first = new Button();
                var disabled = new Button { IsEnabled = false };
                var last = new Button();
                var root = new TestRoot
                {
                    IsTabStopWhenDisabled = true,
                    Child = new StackPanel { Children = { first, disabled, last } }
                };

                Assert.Same(last, KeyboardNavigationHandler.GetNext(first, NavigationDirection.Next));
            }
        }

        [Fact]
        public void Tab_Continues_From_Disabled_Focused_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var first = new Button();
                var target = new Button();
                var last = new Button();
                var root = new TestRoot
                {
                    AllowFocusWhenDisabled = true,
                    Child = new StackPanel { Children = { first, target, last } }
                };

                target.Focus();
                target.IsEnabled = false;

                Assert.Same(last, KeyboardNavigationHandler.GetNext(target, NavigationDirection.Next));
                Assert.Same(first, KeyboardNavigationHandler.GetNext(target, NavigationDirection.Previous));
            }
        }

        [Fact]
        public void Tab_Key_Moves_Focus_Away_From_Disabled_Focused_Control()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var first = new Button();
                var target = new Button();
                var last = new Button();
                var root = new TestRoot
                {
                    AllowFocusWhenDisabled = true,
                    Child = new StackPanel { Children = { first, target, last } }
                };
                var handler = new KeyboardNavigationHandler();
                handler.SetOwner(root);

                target.Focus();
                target.IsEnabled = false;
                KeyDown(root, Key.Tab);

                Assert.Same(last, root.FocusManager.GetFocusedElement());
            }
        }

        private static void KeyDown(TestRoot root, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            KeyboardDevice.Instance!.ProcessRawEvent(
                new RawKeyEventArgs(
                    KeyboardDevice.Instance,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    key,
                    modifiers,
                    PhysicalKey.None,
                    null));
        }

        private static void TextInput(TestRoot root, string text)
        {
            KeyboardDevice.Instance!.ProcessRawEvent(
                new RawTextInputEventArgs(KeyboardDevice.Instance, 0, root, text));
        }
    }
}
