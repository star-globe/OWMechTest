using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class AutoFootRigger : MonoBehaviour
    {
        [SerializeField]
        Transform waist;

        [SerializeField]
        Transform legJoint;

        [SerializeField]
        Transform leg1;

        [SerializeField]
        Transform leg2;

        [SerializeField]
        Transform foot;

        [SerializeField]
        Vector3 hintOffset;

#if UNITY_EDITOR
        public void SetRig()
        {
            var list = new List<GameObject>();
            foreach (Transform t in transform) {
                list.Add(t.gameObject);
            }

            foreach (var g in list)
                DestroyImmediate(g);

            var legRigGo = CreateRigGameObject("Leg", transform);
            var bone = legRigGo.AddComponent<TwoBoneIKConstraint>();

            var targetGo = CreateRigGameObject("Target", legRigGo.transform);
            var footIK = targetGo.AddComponent<FootIK>();

            var moveTargetGo = CreateRigGameObject("MoveTarget", legRigGo.transform);

            var hintGo = CreateRigGameObject("Hint", targetGo.transform, hintOffset);

            var local = targetGo.transform.InverseTransformPoint(waist.position);

            var aimTargetGo = CreateRigGameObject("AimTarget", targetGo.transform, new Vector3(local.x, 0, local.z));

            var aimGo = CreateRigGameObject("Cylinder", transform);
            var aim = aimGo.AddComponent<MultiAimConstraint>();

            bone.data.root = leg1;
            bone.data.mid = leg2;
            bone.data.tip = foot;
            bone.data.target = targetGo.transform;
            bone.data.hint = hintGo.transform;

            footIK.SetTargetTrans(legJoint, foot, moveTargetGo.transform);

            aim.data.aimAxis = MultiAimConstraintData.Axis.Y_NEG;
            aim.data.upAxis = MultiAimConstraintData.Axis.Z_NEG;
            aim.data.constrainedObject = waist;
            var sources = aim.data.sourceObjects;
            sources.Clear();
            sources.Add(new WeightedTransform(aimTargetGo.transform, 1.0f));
            aim.data.sourceObjects = sources;
            aim.data.constrainedXAxis = false;
            aim.data.constrainedYAxis = false;
            aim.data.constrainedZAxis = true;
        }

        private GameObject CreateRigGameObject(string name, Transform parent)
        {
            return CreateRigGameObject(name, parent, Vector3.zero);
        }

        private GameObject CreateRigGameObject(string name, Transform parent, Vector3 localOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localOffset;
            return go;
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AutoFootRigger))]
    public class AutoFootRiggerEditor : UnityEditor.Editor
    {
        AutoFootRigger rigger = null;

        private void OnEnable()
        {
            rigger = target as AutoFootRigger;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("SetRig"))
                rigger.SetRig();
        }
    }
#endif
}
