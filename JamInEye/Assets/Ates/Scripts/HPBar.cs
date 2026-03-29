using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class ChargeBar : MonoBehaviour
{
    [Header("Fill Images")]
    [SerializeField] private Image _baseChargeFillImage;
    [SerializeField] private Image _overChargeFillImage;

    [Header("Colors")]
    [SerializeField] private Color _baseHealthyColor = Color.green;
    [SerializeField] private Color _baseDangerColor = Color.yellow;
    [SerializeField] private Color _overchargeColor = new Color(0.6f, 0f, 1f, 1f);
    [SerializeField] private Color _overchargeCritColor = Color.red;

    //[Header("Vignette")]
    //[SerializeField] private Volume _globalVolume;
    //[SerializeField] private Color _vignetteBaseDangerColor = Color.red;
    //[SerializeField] private Color _vignetteOverchargeColor = new Color(0.6f, 0f, 1f, 1f);
    //[SerializeField] private float _vignetteMaxIntensity = 0.55f;
    //[SerializeField] private float _vignettePulseSpeed = 3f;
    //[SerializeField] private float _vignetteAnimationSpeed = 5f;

    //[Header("Vignette Safe Transition")]
    //[SerializeField] private float _vignetteFadeOutDuration = 0.8f;
    //[SerializeField] private Ease _vignetteFadeOutEase = Ease.OutQuad;

    [Header("HP Danger Threshold")]
    [SerializeField, Range(0f, 1f)] private float _hpDangerThreshold = 0.3f;

    [Header("UI Tweening")]
    [SerializeField] private float _baseFillTweenDuration = 0.12f;
    [SerializeField] private float _overFillTweenDuration = 0.12f;
    [SerializeField] private float _colorTweenDuration = 0.12f;
    [SerializeField] private Ease _uiTweenEase = Ease.OutQuad;

    private PlayerStats _ps;
    private Vignette _vignette;

    private float _baseIntensity;
    private Color _baseColor;

    private float _smoothIntensity;
    private Color _smoothColor;

    private enum VignetteState { Safe, Active, FadingOut }
    private VignetteState _vignetteState = VignetteState.Safe;

    private Sequence _fadeOutSequence;

    private Tween _baseFillTween;
    private Tween _baseColorTween;
    private Tween _overFillTween;
    private Tween _overColorTween;
    //private Tween _vignetteIntensityTween;
    //private Tween _vignetteColorTween;

    private float _currentBaseFill;
    private float _currentOverFill;

    private Color _currentBaseFillColor;
    private Color _currentOverFillColor;

    private float _lastBaseFillTarget = -999f;
    private float _lastOverFillTarget = -999f;
    private Color _lastBaseColorTarget;
    private Color _lastOverColorTarget;

    void Awake()
    {
        _ps = FindFirstObjectByType<PlayerStats>();

        //if (_globalVolume == null)
        //    _globalVolume = FindFirstObjectByType<Volume>();

        //if (_globalVolume != null && _globalVolume.profile.TryGet(out _vignette))
        //{
        //    _baseIntensity = _vignette.intensity.value;
        //    _baseColor = _vignette.color.value;

        //    _smoothIntensity = _baseIntensity;
        //    _smoothColor = _baseColor;

        //    _vignette.intensity.overrideState = true;
        //    _vignette.color.overrideState = true;
        //    _vignette.intensity.Override(_smoothIntensity);
        //    _vignette.color.Override(_smoothColor);
        //}

        if (_baseChargeFillImage != null)
        {
            _currentBaseFill = _baseChargeFillImage.fillAmount;
            _currentBaseFillColor = _baseChargeFillImage.color;
            _lastBaseColorTarget = _currentBaseFillColor;
        }

        if (_overChargeFillImage != null)
        {
            _currentOverFill = _overChargeFillImage.fillAmount;
            _currentOverFillColor = _overChargeFillImage.color;
            _lastOverColorTarget = _currentOverFillColor;
        }
    }

    void OnDestroy()
    {
        KillAllTweens();
    }

    void Update()
    {
        if (_ps == null) return;

        UpdateBaseBar();
        UpdateOverchargeBar();
        //UpdateVignette();
    }

    private void UpdateBaseBar()
    {
        if (_baseChargeFillImage == null) return;

        float targetFill = _ps.maxHP > 0f
            ? _ps.currentHP / _ps.maxHP
            : 0f;

        Color targetColor = _ps.inDanger
            ? _baseDangerColor
            : _baseHealthyColor;

        if (!Mathf.Approximately(targetFill, _lastBaseFillTarget))
        {
            _lastBaseFillTarget = targetFill;

            _baseFillTween?.Kill();
            _baseFillTween = DOTween.To(
                    () => _currentBaseFill,
                    x =>
                    {
                        _currentBaseFill = x;
                        _baseChargeFillImage.fillAmount = x;
                    },
                    targetFill,
                    _baseFillTweenDuration)
                .SetEase(_uiTweenEase)
                .SetLink(gameObject);
        }

        if (targetColor != _lastBaseColorTarget)
        {
            _lastBaseColorTarget = targetColor;

            _baseColorTween?.Kill();
            _baseColorTween = DOTween.To(
                    () => _currentBaseFillColor,
                    c =>
                    {
                        _currentBaseFillColor = c;
                        _baseChargeFillImage.color = c;
                    },
                    targetColor,
                    _colorTweenDuration)
                .SetEase(_uiTweenEase)
                .SetLink(gameObject);
        }
    }

    private void UpdateOverchargeBar()
    {
        if (_overChargeFillImage == null) return;

        float t = _ps.maxOvercharge > 0f
            ? _ps.currentOvercharge / _ps.maxOvercharge
            : 0f;

        Color targetColor = t > 0.75f ? _overchargeCritColor : _overchargeColor;
        bool shouldEnable = _ps.currentOvercharge > 0f;

        if (_overChargeFillImage.enabled != shouldEnable)
            _overChargeFillImage.enabled = shouldEnable;

        if (!Mathf.Approximately(t, _lastOverFillTarget))
        {
            _lastOverFillTarget = t;

            _overFillTween?.Kill();
            _overFillTween = DOTween.To(
                    () => _currentOverFill,
                    x =>
                    {
                        _currentOverFill = x;
                        _overChargeFillImage.fillAmount = x;
                    },
                    t,
                    _overFillTweenDuration)
                .SetEase(_uiTweenEase)
                .SetLink(gameObject);
        }

        if (targetColor != _lastOverColorTarget)
        {
            _lastOverColorTarget = targetColor;

            _overColorTween?.Kill();
            _overColorTween = DOTween.To(
                    () => _currentOverFillColor,
                    c =>
                    {
                        _currentOverFillColor = c;
                        _overChargeFillImage.color = c;
                    },
                    targetColor,
                    _colorTweenDuration)
                .SetEase(_uiTweenEase)
                .SetLink(gameObject);
        }
    }

    //private void UpdateVignette()
    //{
    //    if (_vignette == null) return;

    //    float hpT = _ps.maxHP > 0f ? _ps.currentHP / _ps.maxHP : 0f;
    //    bool hpInDanger = hpT <= _hpDangerThreshold;
    //    bool anyDanger = _ps.currentOvercharge > 0f || hpInDanger || _ps.inDanger;

    //    if (anyDanger)
    //    {
    //        if (_vignetteState != VignetteState.Active)
    //        {
    //            KillFadeOutTweens();
    //            _vignetteState = VignetteState.Active;
    //        }

    //        float targetIntensity;
    //        Color targetColor;

    //        float hpDangerDepth = hpInDanger
    //            ? Mathf.Clamp01(1f - (hpT / _hpDangerThreshold))
    //            : 0f;

    //        if (_ps.currentOvercharge > 0f)
    //        {
    //            float overchargeT = _ps.maxOvercharge > 0f
    //                ? _ps.currentOvercharge / _ps.maxOvercharge
    //                : 0f;

    //            float basePulse = DOVirtual.EasedValue(0.3f, _vignetteMaxIntensity, overchargeT, Ease.Linear);
    //            float pulse = Mathf.Sin(Time.time * _vignettePulseSpeed * (1f + overchargeT)) * 0.08f;
    //            targetIntensity = Mathf.Clamp01(basePulse + pulse);

    //            targetColor = Color.Lerp(_vignetteBaseDangerColor, _vignetteOverchargeColor, overchargeT);
    //        }
    //        else if (hpInDanger)
    //        {
    //            float pulse = Mathf.Sin(Time.time * _vignettePulseSpeed * 0.5f) * 0.05f;
    //            targetIntensity = Mathf.Clamp01(
    //                DOVirtual.EasedValue(0.15f, _vignetteMaxIntensity, hpDangerDepth, Ease.Linear) + pulse
    //            );
    //            targetColor = _vignetteBaseDangerColor;
    //        }
    //        else
    //        {
    //            float pulse = Mathf.Sin(Time.time * _vignettePulseSpeed * 0.5f) * 0.05f;
    //            targetIntensity = Mathf.Clamp01(0.25f + pulse);
    //            targetColor = _vignetteBaseDangerColor;
    //        }

    //        TweenVignetteTo(targetIntensity, targetColor);
    //    }
    //    else if (_vignetteState == VignetteState.Active)
    //    {
    //        BeginFadeOut();
    //    }
    //}

    //private void TweenVignetteTo(float targetIntensity, Color targetColor)
    //{
    //    float duration = Mathf.Max(0.01f, 1f / Mathf.Max(0.01f, _vignetteAnimationSpeed));

    //    _vignetteIntensityTween?.Kill();
    //    _vignetteIntensityTween = DOTween.To(
    //            () => _smoothIntensity,
    //            v =>
    //            {
    //                _smoothIntensity = v;
    //                _vignette.intensity.Override(v);
    //            },
    //            targetIntensity,
    //            duration)
    //        .SetEase(Ease.OutQuad)
    //        .SetUpdate(true)
    //        .SetLink(gameObject);

    //    _vignetteColorTween?.Kill();
    //    _vignetteColorTween = DOTween.To(
    //            () => _smoothColor,
    //            c =>
    //            {
    //                _smoothColor = c;
    //                _vignette.color.Override(c);
    //            },
    //            targetColor,
    //            duration)
    //        .SetEase(Ease.OutQuad)
    //        .SetUpdate(true)
    //        .SetLink(gameObject);
    //}

    private void KillFadeOutTweens()
    {
        if (_fadeOutSequence != null && _fadeOutSequence.IsActive())
            _fadeOutSequence.Kill();

        _fadeOutSequence = null;
    }

    //private void BeginFadeOut()
    //{
    //    if (_vignetteState == VignetteState.FadingOut) return;

    //    _vignetteIntensityTween?.Kill();
    //    _vignetteColorTween?.Kill();
    //    KillFadeOutTweens();

    //    _vignetteState = VignetteState.FadingOut;

    //    _fadeOutSequence = DOTween.Sequence();
    //    _fadeOutSequence.SetUpdate(true);
    //    _fadeOutSequence.SetLink(gameObject);

    //    _fadeOutSequence.Join(
    //        DOTween.To(
    //            () => _smoothIntensity,
    //            v =>
    //            {
    //                _smoothIntensity = v;
    //                _vignette.intensity.Override(v);
    //            },
    //            _baseIntensity,
    //            _vignetteFadeOutDuration
    //        ).SetEase(_vignetteFadeOutEase)
    //    );

    //    _fadeOutSequence.Join(
    //        DOTween.To(
    //            () => _smoothColor,
    //            c =>
    //            {
    //                _smoothColor = c;
    //                _vignette.color.Override(c);
    //            },
    //            _baseColor,
    //            _vignetteFadeOutDuration
    //        ).SetEase(_vignetteFadeOutEase)
    //    );

    //    _fadeOutSequence.OnComplete(() =>
    //    {
    //        if (_vignetteState == VignetteState.FadingOut)
    //        {
    //            _smoothIntensity = _baseIntensity;
    //            _smoothColor = _baseColor;
    //            _vignette.intensity.Override(_baseIntensity);
    //            _vignette.color.Override(_baseColor);
    //            _vignetteState = VignetteState.Safe;
    //        }

    //        _fadeOutSequence = null;
    //    });
    //}

    private void KillAllTweens()
    {
        _baseFillTween?.Kill();
        _baseColorTween?.Kill();
        _overFillTween?.Kill();
        _overColorTween?.Kill();
        //_vignetteIntensityTween?.Kill();
        //_vignetteColorTween?.Kill();
        KillFadeOutTweens();
    }

    //public void ForceResetVignette()
    //{
    //    //_vignetteIntensityTween?.Kill();
    //    //_vignetteColorTween?.Kill();
    //    KillFadeOutTweens();

    //    _smoothIntensity = _baseIntensity;
    //    _smoothColor = _baseColor;
    //    _vignetteState = VignetteState.Safe;

    //    if (_vignette == null) return;

    //    _vignette.intensity.Override(_baseIntensity);
    //    _vignette.color.Override(_baseColor);
    //}
}