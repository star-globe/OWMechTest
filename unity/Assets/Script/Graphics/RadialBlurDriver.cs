using AdvancedGears;
using UnityEngine;
using UnityEngine.Rendering;

public class RadialBlurDriver : MonoBehaviour
{
    [SerializeField]
    float hyperBoostIns = 1.0f;
    [SerializeField]
    float quickBoostIns = 0.6f;
    [SerializeField]
    float boostIns = 0.3f;

    [SerializeField]
    float _riseSpeed = 1.0f;
    [SerializeField]
    float _fallSpeed = 0.4f;

    [SerializeField]
    bool reflectBoost = true;

    [SerializeField] Volume _volume;
    RadialBlurVolumeComponent _radialBlur;

    float _current = 0f;

    long playerId
    {
        get
        {
            return BattleUIManager.Instance.CurrentPlayerId;
        }
    }

    void Start()
    {
        _volume.profile.TryGet(out _radialBlur);
        _radialBlur.intensity.overrideState = true;
        _radialBlur.sampleCount.overrideState = true;
        _radialBlur.downsample.overrideState = true;
        _radialBlur.centerRadius.overrideState = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (reflectBoost == false)
            return;

        float target = 0;

        var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
        if (param != null)
        {
            target = param switch
            {
                { IsHyperBoost: true } => hyperBoostIns,
                { IsQuickBoost: true } => quickBoostIns,
                { IsBoost: true } => boostIns,
                _ => 0f,
            };
        }

        // 立ち上がりは速く、戻りは緩やかにすると気持ちいい
        _current = Mathf.MoveTowards(_current, target, (target > _current ? _riseSpeed : _fallSpeed) * Time.deltaTime);
        _radialBlur.intensity.value = _current;
    }
}
