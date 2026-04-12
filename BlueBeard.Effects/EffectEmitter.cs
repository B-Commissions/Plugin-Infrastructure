using System;
using System.Collections;
using System.Linq;
using BlueBeard.Core.Helpers;
using BlueBeard.Effects.Audiences;
using SDG.Unturned;
using UnityEngine;
using UnturnedEffectManager = SDG.Unturned.EffectManager;

namespace BlueBeard.Effects;

public class EffectEmitter : MonoBehaviour
{
    public event Action<EffectEmitter> Completed;
    public EffectDefinition Definition { get; set; }
    public IEffectAudience Audience { get; set; }
    private Coroutine _coroutine;

    public void Begin()
    {
        if (_coroutine != null) return;
        _coroutine = StartCoroutine(EmitLoop());
    }

    public void End()
    {
        if (_coroutine == null) return;
        StopCoroutine(_coroutine);
        _coroutine = null;
    }

    private IEnumerator EmitLoop()
    {
        do
        {
            var recipients = Audience.GetRecipients().ToList();
            foreach (var point in Definition.Pattern.GetPoints())
            {
                var position = Definition.Origin + point;
                if (Definition.SnapToSurface)
                    position = SurfaceHelper.SnapPositionToSurface(position, RayMasks.GROUND);

                if (Definition.Scale.HasValue || Definition.Rotation.HasValue)
                {
                    // Extended path: build a TriggerEffectParameters so the client applies
                    // the requested scale and/or rotation. Recipients are supplied via
                    // SetRelevantTransportConnections.
                    var parameters = new TriggerEffectParameters(Definition.EffectId)
                    {
                        position = position,
                        reliable = true,
                    };
                    if (Definition.Scale.HasValue)
                        parameters.scale = Definition.Scale.Value;
                    if (Definition.Rotation.HasValue)
                        parameters.SetRotation(Definition.Rotation.Value);
                    parameters.SetRelevantTransportConnections(recipients);
                    UnturnedEffectManager.triggerEffect(parameters);
                }
                else
                {
                    // Fast path: unchanged from the original implementation.
                    foreach (var connection in recipients)
                        UnturnedEffectManager.sendEffectReliable(Definition.EffectId, connection, position);
                }
            }
            if (!Definition.OneShot) yield return new WaitForSeconds(Definition.Interval);
        } while (!Definition.OneShot);
        Completed?.Invoke(this);
    }

    private void OnDestroy() { End(); }
}
