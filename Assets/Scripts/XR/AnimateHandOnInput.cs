using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    private static readonly int TriggerHash = Animator.StringToHash("Trigger");
    private static readonly int GripHash = Animator.StringToHash("Grip");

    public InputActionProperty triggerValue;
    public InputActionProperty gripValue;

    public Animator handAnimator;

    void Update()
    {
        float trigger = triggerValue.action.ReadValue<float>();
        float grip = gripValue.action.ReadValue<float>();

        handAnimator.SetFloat(TriggerHash, trigger);
        handAnimator.SetFloat(GripHash, grip);
    }
}