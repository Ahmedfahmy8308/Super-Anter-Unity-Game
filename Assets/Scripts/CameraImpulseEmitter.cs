using System;
using System.Reflection;
using UnityEngine;

public class CameraImpulseEmitter : MonoBehaviour
{
    [SerializeField] private Component impulseSource;
    [SerializeField] private CameraFollow2D fallbackCameraShake;
    [SerializeField] private float fallbackAmplitudeMultiplier = 0.35f;
    [SerializeField] private float fallbackDuration = 0.18f;

    public void Emit(float force)
    {
        if (TryEmitCinemachineImpulse(force))
        {
            return;
        }

        if (fallbackCameraShake != null)
        {
            fallbackCameraShake.Shake(force * fallbackAmplitudeMultiplier, fallbackDuration);
        }
    }

    private bool TryEmitCinemachineImpulse(float force)
    {
        if (impulseSource == null)
        {
            return false;
        }

        Type sourceType = impulseSource.GetType();
        if (!sourceType.Name.Contains("CinemachineImpulseSource", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        MethodInfo forceMethod = sourceType.GetMethod("GenerateImpulseWithForce", new[] { typeof(float) });
        if (forceMethod != null)
        {
            forceMethod.Invoke(impulseSource, new object[] { force });
            return true;
        }

        MethodInfo defaultMethod = sourceType.GetMethod("GenerateImpulse", Type.EmptyTypes);
        if (defaultMethod != null)
        {
            defaultMethod.Invoke(impulseSource, null);
            return true;
        }

        return false;
    }
}