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

    private readonly static Collider[] colliders = new Collider[256];
    public static bool CheckOverlapShpere(Vector3 pos, float radius, UnitSide selfSide, int layerMask, string tag, out Vector3 targetPos)
    {
        targetPos = Vector3.zero;
        float length = float.MaxValue;
        var count = Physics.OverlapSphereNonAlloc(pos, radius, colliders, layerMask);
        for (int i = 0; i < count; i++)
        {
            var col = colliders[i];
            if (string.Equals(col.gameObject.tag, tag) == false)
                continue;

            if (selfSide != UnitSide.None)
            {
                var character = col.gameObject.GetComponent<PlayerCharacter>();
                if (character == null || character.Side == selfSide)
                    continue;
            }

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

    public static int OverlapShpere(Vector3 pos, float radius, UnitSide selfSide, int layerMask, string tag, BaseCharacter[] results)
    {
        int characterCount = 0;
        var count = Physics.OverlapSphereNonAlloc(pos, radius, colliders, layerMask);
        for (int i = 0; i < count; i++)
        {
            var col = colliders[i];
            if (string.Equals(col.gameObject.tag, tag) == false)
                continue;

            BaseCharacter character = null;
            if (selfSide != UnitSide.None)
            {
                character = col.gameObject.GetComponent<BaseCharacter>();
                if (character == null || character.Side == selfSide)
                    continue;
            }

            if (character != null && characterCount < results.Length)
            {
                results[characterCount] = character;
                characterCount++;
            }
        }

        return characterCount;
    }


    public static bool CheckOverlapScorn(Vector3 start, Vector3 forward, float angleRad, UnitSide selfSide, int layerMask, string tag, out Vector3 targetPos)
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
            if (string.Equals(col.gameObject.tag, tag) == false)
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
