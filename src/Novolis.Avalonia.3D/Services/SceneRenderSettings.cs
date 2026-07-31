using System.Numerics;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Runtime shaded-render settings (preview window + GPU uniforms). Not persisted in .nov3djson.</summary>
public sealed class SceneRenderSettings
{
    private Vector3 _ambientColor = new(0.14f, 0.16f, 0.2f);
    private float _ambientStrength = 1f;
    private float _exposure = 1.15f;
    private Vector3 _clearColor = new(0.07f, 0.09f, 0.13f);
    private Vector3 _baseColor = new(0.72f, 0.76f, 0.8f);
    private float _lightScale = 1f;
    private bool _wireOverlay;
    private bool _twoSided = true;

    public Vector3 AmbientColor
    {
        get => _ambientColor;
        set { _ambientColor = value; Raise(); }
    }

    public float AmbientStrength
    {
        get => _ambientStrength;
        set { _ambientStrength = System.Math.Clamp(value, 0f, 4f); Raise(); }
    }

    public float Exposure
    {
        get => _exposure;
        set { _exposure = System.Math.Clamp(value, 0.1f, 8f); Raise(); }
    }

    public Vector3 ClearColor
    {
        get => _clearColor;
        set { _clearColor = value; Raise(); }
    }

    public Vector3 BaseColor
    {
        get => _baseColor;
        set { _baseColor = value; Raise(); }
    }

    public float LightScale
    {
        get => _lightScale;
        set { _lightScale = System.Math.Clamp(value, 0f, 8f); Raise(); }
    }

    public bool WireOverlay
    {
        get => _wireOverlay;
        set { _wireOverlay = value; Raise(); }
    }

    public bool TwoSided
    {
        get => _twoSided;
        set { _twoSided = value; Raise(); }
    }

    public event Action? Changed;

    public void ResetDefaults()
    {
        _ambientColor = new Vector3(0.14f, 0.16f, 0.2f);
        _ambientStrength = 1f;
        _exposure = 1.15f;
        _clearColor = new Vector3(0.07f, 0.09f, 0.13f);
        _baseColor = new Vector3(0.72f, 0.76f, 0.8f);
        _lightScale = 1f;
        _wireOverlay = false;
        _twoSided = true;
        Raise();
    }

    public Vector3 EffectiveAmbient => _ambientColor * _ambientStrength;

    private void Raise() => Changed?.Invoke();
}
