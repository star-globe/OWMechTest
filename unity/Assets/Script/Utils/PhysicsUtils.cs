using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdvancedGears;

public static class PhysicsUtils
{
    private static int? footLayer = null;
    public static int FootLayer
    {
        get
        {
            if (footLayer == null)
            {
                footLayer = LayerMask.GetMask("Ground", "Building");
            }

            return footLayer.Value;
        }
    }

    private static TagHandle? _playerTag = null;
    public static TagHandle PlayerTag
    {
        get
        {
            if (_playerTag == null)
            {
                _playerTag = TagHandle.GetExistingTag("Player");
            }

            return _playerTag.Value;
        }
    }

    private readonly static Collider[] colliders = new Collider[256];

    /// <summary>
    /// 指定した除外マスク（UnitSide ビットマスク）に一致しない最近傍の BaseObject の座標を返す。
    /// excludeSideMask は UnitSideExtensions.ToExcludeMask() で生成する。
    /// </summary>
    public static bool CheckOverlapShpereOthers(Vector3 pos, float radius, int excludeSideMask, int layerMask, TagHandle tag, out Vector3 targetPos)
    {
        targetPos = Vector3.zero;
        float length = float.MaxValue;
        var count = Physics.OverlapSphereNonAlloc(pos, radius, colliders, layerMask);
        for (int i = 0; i < count; i++)
        {
            var col = colliders[i];
            if (col.CompareTag(tag) == false)
                continue;

            var baseObject = col.gameObject.GetComponent<BaseObject>();
            if (baseObject == null)
                continue;

            if ((excludeSideMask & (1 << (int)baseObject.Side)) != 0)
                continue;

            var colPos = col.gameObject.transform.position;
            var diff = colPos - pos;
            var len = diff.sqrMagnitude;
            if (len > length)
                continue;

            length = len;
            targetPos = colPos;
        }

        return length < float.MaxValue;
    }

    /// <summary>
    /// 指定した除外マスク（UnitSide ビットマスク）に一致しない BaseObject を results に格納し、件数を返す。
    /// excludeSideMask は UnitSideExtensions.ToExcludeMask() で生成する。
    /// </summary>
    public static int OverlapShpereOthers(Vector3 pos, float radius, int excludeSideMask, int layerMask, TagHandle? tag, BaseObject[] results)
    {
        int objectCount = 0;
        var count = Physics.OverlapSphereNonAlloc(pos, radius, colliders, layerMask);
        for (int i = 0; i < count; i++)
        {
            var col = colliders[i];
            if (tag != null && col.CompareTag(tag.Value) == false)
                continue;

            var baseObject = col.gameObject.GetComponent<BaseObject>();
            if (baseObject == null)
                continue;

            if ((excludeSideMask & (1 << (int)baseObject.Side)) != 0)
                continue;

            if (objectCount < results.Length)
            {
                results[objectCount] = baseObject;
                objectCount++;
            }
        }

        return objectCount;
    }


    public static bool CheckOverlapScorn(Vector3 start, Vector3 forward, float angleRad, UnitSide selfSide, int layerMask, TagHandle tag, out Vector3 targetPos)
    {
        targetPos = Vector3.zero;
        float length = float.MaxValue;

        var sin = Mathf.Sin(angleRad);

        var end = start + forward;
        var radius = forward.magnitude * Mathf.Tan(angleRad);
        var count = Physics.OverlapCapsuleNonAlloc(start, end, radius, colliders, layerMask);
        for (int i = 0; i < count; i++)
        {
            var col = colliders[i];
            if (col.CompareTag(tag) == false)
                continue;

            if (selfSide != UnitSide.None)
            {
                var character = col.gameObject.GetComponent<PlayerCharacter>();
                if (character == null || character.Side == selfSide)
                    continue;
            }

            var colPos = col.gameObject.transform.position;
            var diff = colPos - start;

            var cross = Vector3.Cross(diff.normalized, forward.normalized);
            if (cross.sqrMagnitude > sin * sin)
                continue;

            var len = diff.sqrMagnitude;
            if (len > length)
                continue;

            length = len;
            targetPos = colPos;
        }

        return length < float.MaxValue;
    }
}
