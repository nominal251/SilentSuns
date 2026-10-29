using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private static Component LiveInput() => (Component)Object.FindFirstObjectByType(GameplayType("InputHandler"));
        private static int BindingIndex(string action, string path) => TerminalBindings.Controls.FindAction(action).bindings.ToList().FindIndex(b => b.path == path);
        private static void SetTestBinding(string action, string path, string replacement)
        {
            TerminalBindings.Controls.FindAction(action).ApplyBindingOverride(BindingIndex(action, path), replacement);
        }
        private IEnumerator LoadInputScene()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            yield return null;
            var player = SurfacePlayer();
            HoldPlayer(player);
            var body = player.GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            player.transform.position = body.position = new Vector3(0, 200, 0);
            player.transform.rotation = body.rotation = Quaternion.identity;
            body.detectCollisions = false;
            player.transform.Find("Player Camera").localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            // Settle the test teleport before placing a raycast target in front of the camera.
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(TerminalPause.BlocksGameplayInput, Is.False);
        }
        private static GameObject PickupTarget(Component player)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Input test pickup";
            var view = player.transform.Find("Player Camera");
            target.transform.position = view.position + view.forward * 1.2f;
            target.transform.localScale = Vector3.one * .3f;
            target.AddComponent<Rigidbody>().useGravity = false;
            target.AddComponent(GameplayType("PhysicsInteractable"));
            Physics.SyncTransforms();
            return target;
        }

        [UnityTest, Category("Menu"), Category("InputIntegration")]
        public IEnumerator PhysicsBindingsUseSettingsApplyDiscardAndDeviceReset()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            MainSettings.ShowTab("KEYBINDS");
            yield return null;
            Assert.That(TerminalBindings.ConflictWarning(), Is.Empty, "Defaults must not conflict with existing controls.");
            foreach (bool pad in new[] { false, true })
            {
                MainSettings.ShowBindingDevice(pad);
                var labels = MainSettings.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => t.text).ToArray();
                foreach (string name in new[] { "PICK UP / DROP", "ROTATE HELD OBJECT", "MOVE OBJECT CLOSER", "MOVE OBJECT FARTHER" })
                    Assert.That(labels.Any(t => t.Contains(name)), Is.True, name);
            }
            MainSettings.ShowBindingDevice(false);
            var pickup = TerminalBindings.Controls.FindAction("Physics Interact");
            int index = BindingIndex("Physics Interact", "<Mouse>/rightButton");
            TerminalBindings.Rebind(pickup, index, _ => { });
            yield return new WaitForSecondsRealtime(.15f);
            yield return Press(Key.W);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(MainSettings.ApplyChanges(), Is.False);
            Assert.That(TerminalBindings.ConflictWarning(), Does.Contain("PICK UP / DROP").And.Contain("MOVE FORWARD"));
            TerminalBindings.Rebind(pickup, index, _ => { });
            yield return new WaitForSecondsRealtime(.15f);
            yield return Press(Key.F);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(MainSettings.ApplyChanges(), Is.True);
            var reach = TerminalBindings.Controls.FindAction("Physics Extend");
            int farther = BindingIndex("Physics Extend", "<Mouse>/scroll/up");
            TerminalBindings.Rebind(reach, farther, _ => { });
            yield return new WaitForSecondsRealtime(.15f);
            yield return Press(Key.V);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(reach.bindings[farther].effectivePath, Is.EqualTo("<Keyboard>/v"));
            Assert.That(MainSettings.ApplyChanges(), Is.True);
            var saved = Object.Instantiate(TerminalBindings.Controls);
            try
            {
                saved.RemoveAllBindingOverrides();
                saved.LoadBindingOverridesFromJson(PlayerPrefs.GetString("SilentSuns.Input.Bindings"));
                Assert.That(saved.FindAction("Physics Interact").bindings[index].effectivePath, Is.EqualTo("<Keyboard>/f"));
            }
            finally { Object.Destroy(saved); }
            pickup.ApplyBindingOverride(index, "<Keyboard>/g");
            MainSettings.DiscardChanges();
            Assert.That(pickup.bindings[index].effectivePath, Is.EqualTo("<Keyboard>/f"));
            SetTestBinding("Physics Interact", "<Gamepad>/buttonWest", "<Gamepad>/dpad/left");
            TerminalBindings.ResetDevice(false);
            Assert.That(pickup.bindings[index].effectivePath, Is.EqualTo("<Mouse>/rightButton"));
            Assert.That(pickup.bindings[BindingIndex("Physics Interact", "<Gamepad>/buttonWest")].effectivePath, Is.EqualTo("<Gamepad>/dpad/left"));
            Assert.That(MainSettings.ApplyChanges(), Is.True);
        }

        [UnityTest, Category("Menu"), Category("InputIntegration")]
        public IEnumerator RemappedMovementAndInstrumentInteractionReachGameplay()
        {
            yield return LoadInputScene();
            TerminalBindings.BeginEdit();
            SetTestBinding("Move", "<Keyboard>/w", "<Keyboard>/i");
            SetTestBinding("Interact", "<Mouse>/leftButton", "<Keyboard>/f");
            Assert.That(TerminalBindings.ApplyEdits(), Is.True);
            var player = SurfacePlayer();
            var body = player.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(.1f);
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(.01f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I));
            yield return new WaitForSeconds(.15f);
            Assert.That(body.linearVelocity.z, Is.GreaterThan(.1f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            HoldPlayer(player);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                target.tag = "Interactable";
                target.transform.position = player.transform.position + player.transform.forward * 1.2f;
                target.transform.localScale = Vector3.one * .3f;
                var dial = target.AddComponent(GameplayType("RotarySwitch"));
                Physics.SyncTransforms();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                yield return null; yield return null;
                Assert.That(ReadMember(dial, "currentState"), Is.EqualTo(0));
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return Press(Key.F);
                Assert.That(ReadMember(dial, "currentState"), Is.EqualTo(1));
                var pause = Object.FindFirstObjectByType<TerminalPause>();
                pause.Pause();
                yield return Press(Key.F);
                Assert.That(ReadMember(dial, "currentState"), Is.EqualTo(1), "Menu input must not turn the instrument.");
            }
            finally { Object.Destroy(target); }
        }

        [UnityTest, Category("Menu"), Category("InputIntegration")]
        public IEnumerator RemappedPickupRotationAndReachStopDuringPause()
        {
            yield return LoadInputScene();
            SetTestBinding("Physics Interact", "<Mouse>/rightButton", "<Keyboard>/f");
            SetTestBinding("Physics Rotate", "<Mouse>/middleButton", "<Keyboard>/g");
            SetTestBinding("Physics Extend", "<Mouse>/scroll/up", "<Keyboard>/v");
            SetTestBinding("Physics Extend", "<Mouse>/scroll/down", "<Keyboard>/c");
            var player = SurfacePlayer();
            var target = PickupTarget(player);
            var hand = player.transform.Find("Hand");
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
                yield return null; yield return null;
                Assert.That(target.transform.parent, Is.Null);
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return Press(Key.F);
                Assert.That(target.transform.parent, Is.EqualTo(hand));
                Assert.That(target.GetComponent<Rigidbody>().isKinematic, Is.True);
                float start = hand.localPosition.z;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.V));
                yield return new WaitForSeconds(.15f);
                Assert.That(hand.localPosition.z, Is.GreaterThan(start));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
                yield return new WaitForSeconds(.2f);
                Assert.That(hand.localPosition.z, Is.EqualTo(start).Within(.01f));
                var playerRotation = player.transform.rotation;
                var handRotation = hand.rotation;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(40, 0) });
                yield return null; yield return null;
                Assert.That(Quaternion.Angle(handRotation, hand.rotation), Is.GreaterThan(.5f));
                Assert.That(Quaternion.Angle(playerRotation, player.transform.rotation), Is.LessThan(.01f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                var pause = Object.FindFirstObjectByType<TerminalPause>();
                pause.Pause();
                start = hand.localPosition.z;
                handRotation = hand.rotation;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F, Key.G, Key.V));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(40, 0) });
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(target.transform.parent, Is.EqualTo(hand));
                Assert.That(hand.localPosition.z, Is.EqualTo(start));
                Assert.That(Quaternion.Angle(handRotation, hand.rotation), Is.LessThan(.01f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return null;
                pause.Resume();
                float deadline = Time.realtimeSinceStartup + 2;
                while (TerminalPause.BlocksGameplayInput && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(TerminalPause.BlocksGameplayInput, Is.False);
                yield return Press(Key.F);
                Assert.That(target.transform.parent, Is.Null);
                Assert.That(target.GetComponent<Rigidbody>().isKinematic, Is.False);
                Assert.That(target.GetComponent<Rigidbody>().detectCollisions, Is.True);
            }
            finally { Object.Destroy(target); }
        }

        [UnityTest, Category("Menu"), Category("InputIntegration")]
        public IEnumerator ControllerPickupReachAndRotationUseTheSharedActions()
        {
            yield return LoadInputScene();
            var player = SurfacePlayer();
            var hand = player.transform.Find("Hand");
            var target = PickupTarget(player);
            try
            {
                SetTestBinding("Physics Interact", "<Gamepad>/buttonWest", "<Gamepad>/dpad/left");
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null; yield return null;
                Assert.That(target.transform.parent, Is.Null);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadLeft));
                yield return null; yield return null;
                Assert.That(target.transform.parent, Is.EqualTo(hand));
                float before = hand.localPosition.z;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadUp));
                yield return new WaitForSeconds(.15f);
                Assert.That(hand.localPosition.z, Is.GreaterThan(before));
                var rotation = hand.rotation;
                var view = player.transform.rotation;
                GamePreferences.SetOption("ControllerSensitivity", 2);
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(.7f, 0) }.WithButton(GamepadButton.RightStick));
                yield return new WaitForSeconds(.15f);
                Assert.That(Quaternion.Angle(rotation, hand.rotation), Is.GreaterThan(1));
                Assert.That(Quaternion.Angle(view, player.transform.rotation), Is.LessThan(.01f));
                Assert.That(ReadMember(LiveInput(), "lookSensitivity"), Is.EqualTo(2f));
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
            }
            finally { Object.Destroy(target); }
        }

        [UnityTest, Category("Menu"), Category("InputIntegration")]
        public IEnumerator MouseSensitivityAndWheelReachAffectThePlayer()
        {
            yield return LoadInputScene();
            var player = SurfacePlayer();
            var initial = player.transform.rotation;
            GamePreferences.SetMouseSensitivity(1);
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(40, 0) });
            yield return null; yield return null;
            float first = Quaternion.Angle(initial, player.transform.rotation);
            initial = player.transform.rotation;
            GamePreferences.SetMouseSensitivity(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(40, 0) });
            yield return null; yield return null;
            Assert.That(first, Is.GreaterThan(.1f));
            Assert.That(Quaternion.Angle(initial, player.transform.rotation), Is.EqualTo(first * 2).Within(.1f));
            var target = PickupTarget(player);
            var hand = player.transform.Find("Hand");
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
                yield return null; yield return null;
                Assert.That(target.transform.parent, Is.EqualTo(hand));
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return null;
                float before = hand.localPosition.z;
                InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) });
                yield return null; yield return null;
                Assert.That(hand.localPosition.z, Is.GreaterThan(before));
                InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, -120) });
                yield return null; yield return null;
                Assert.That(hand.localPosition.z, Is.EqualTo(before).Within(.01f));
            }
            finally { Object.Destroy(target); }
        }
    }
}
