using Hypersycos.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    public class SensSlider : MonoBehaviour
    {
        [SerializeField] TypedRegisteredValueSO<float> so;
        [SerializeField] Slider slider;

        private void Awake()
        {
            slider.SetValueWithoutNotify(so.Value);
            slider.onValueChanged.AddListener(SetValue);
        }

        void SetValue(float val)
        {
            so.Value = val;
        }
    }
}
