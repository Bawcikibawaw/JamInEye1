using UnityEngine;
using UnityEngine.UI; // Using standard UI for Slider and Image

public class HPBar : MonoBehaviour
{
    private PlayerStats _ps;
    private Slider _slider;
    private Image _fillImage; // To store the reference to the color part

    void Awake()
    {
        _ps = FindFirstObjectByType<PlayerStats>();
        _slider = GetComponent<Slider>();

        // ── THE "NO INSPECTOR" TRICK ──
        // We look at the slider's fillRect (the part that grows/shrinks) 
        // and grab the Image component attached to it.
        if (_slider.fillRect != null)
        {
            _fillImage = _slider.fillRect.GetComponent<Image>();
        }
    }

    void Update()
    {
        if (_ps == null) return;

        // 1. Sync the Max Value
        // Since your PlayerStats already changes its own maxHP, 
        // we can just match it directly!
        _slider.maxValue = _ps.maxHP;

        // 2. Sync the Value
        _slider.value = _ps.currentHP;

        // 3. Change Fill Color based on Danger
        if (_fillImage != null)
        {
            // If inDanger is true, make it Red. Otherwise, keep it Green (or your healthy color).
            _fillImage.color = _ps.inDanger ? Color.red : Color.green;
        }
    }
}