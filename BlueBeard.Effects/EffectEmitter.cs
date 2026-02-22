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
                foreach (var connection in recipients)
                    UnturnedEffectManager.sendEffectReliable(Definition.EffectId, connection, position);
            }
            if (!Definition.OneShot) yield return new WaitForSeconds(Definition.Interval);
        } while (!Definition.OneShot);
        Completed?.Invoke(this);
    }

    private void OnDestroy() { End(); }
}
