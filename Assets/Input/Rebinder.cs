using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    public class Rebinder : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        [SerializeField] Button fwd;
        [SerializeField] Button left;
        [SerializeField] Button back;
        [SerializeField] Button right;
        [SerializeField] Button jump;
        [SerializeField] Button hCrouch;
        [SerializeField] Button tCrouch;
        [SerializeField] Button fire;
        [SerializeField] Button a1;
        [SerializeField] Button a2;
        [SerializeField] Button a3;
        [SerializeField] Button a4;
        [SerializeField] bool isKbm;

        ControlsWrapper controls;

        string group => isKbm ? "Keyboard" : "Gamepad";

        private void Awake()
        {
            controls = ControlsWrapper.Singleton;
            if (isKbm)
            {
                SetBtn(fwd, controls.controls.Player.Move, "up");
                SetBtn(left, controls.controls.Player.Move, "left");
                SetBtn(back, controls.controls.Player.Move, "down");
                SetBtn(right, controls.controls.Player.Move, "right");
            }

            SetBtn(jump, controls.controls.Player.Jump);
            SetBtn(hCrouch, controls.controls.Player.Crouch);
            SetBtn(tCrouch, controls.controls.Player.ToggleCrouch);

            SetBtn(fire, controls.controls.Player.Fire);
            SetBtn(a1, controls.controls.Player.Ability1);
            SetBtn(a2, controls.controls.Player.Ability2);
            SetBtn(a3, controls.controls.Player.Ability3);
            SetBtn(a4, controls.controls.Player.Ability4);
        }

        private void SetBtn(Button btn, InputAction action)
        {
            btn.GetComponentInChildren<TextMeshProUGUI>().text = action.GetBindingDisplayString(group: group);
            btn.onClick.AddListener(() => Rebind(btn, action));
        }

        private void SetBtn(Button btn, InputAction action, string target)
        {
            btn.GetComponentInChildren<TextMeshProUGUI>().text = action.bindings.First((x) => x.name == target && x.groups.Contains(group)).ToDisplayString();
            btn.onClick.AddListener(() => Rebind(btn, action, target));
        }

        private void Rebind(Button btn, InputAction actionToRebind)
        {
            void OnComplete(InputActionRebindingExtensions.RebindingOperation op)
            {
                btn.GetComponentInChildren<TextMeshProUGUI>().text = actionToRebind.GetBindingDisplayString(group: group);
                op.Dispose();
            }

            btn.GetComponentInChildren<TextMeshProUGUI>().text = "<ESC>";
            var rebindOp = actionToRebind.PerformInteractiveRebinding()
                                         .WithBindingGroup(group)
                                         .WithCancelingThrough("<Keyboard>/escape")
                                         .OnComplete(OnComplete)
                                         .OnCancel(OnComplete)
                                         .Start();
        }

        private void Rebind(Button btn, InputAction actionToRebind, string target)
        {
            void OnComplete(InputActionRebindingExtensions.RebindingOperation op)
            {
                btn.GetComponentInChildren<TextMeshProUGUI>().text = actionToRebind.bindings.First((x) => x.name == target && x.groups.Contains(group)).ToDisplayString();
                op.Dispose();
            }

            btn.GetComponentInChildren<TextMeshProUGUI>().text = "<ESC>";
            var rebindOp = actionToRebind.PerformInteractiveRebinding()
                             .WithBindingMask(new() { name = target })
                             .WithBindingGroup(group)
                             .WithCancelingThrough("<Keyboard>/escape")
                             .OnComplete(OnComplete)
                             .OnCancel(OnComplete)
                             .Start();
        }
    }
}
