using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class FootIK : ObjectBehaviour
    {
        [SerializeField]
        int id = 0;
        public int ID => id;

        [SerializeField]
        Transform rootTrans = null;

        [SerializeField]
        Transform footTrans = null;

        [SerializeField]
        float legHeight = 5.0f;

        [SerializeField]
        float walkLength = 3.0f;

        [SerializeField]
        float walkHeight = 0.5f;

        [SerializeField]
        float lerpTime = 0.5f;

        [SerializeField]
        Transform moveTargetTrans = null;

        Vector3? groundedPos = null;
        float lerp = 1.0f;
        Vector3 controllerVec = Vector3.zero;
        const float slideLength = 0.1f;

        Vector3 MoveTargetPosition
        {
            get
            {
                return moveTargetTrans.position + controllerVec;
            }
        }

        public bool IsFloat { get; private set; } = false;
        public bool IsHold { get; private set; } = false;
        bool isSlide = false;

        public bool IsGrounded
        {
            get
            {
                if (groundedPos == null)
                    return false;

                return lerp >= 1.0f;
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(rootTrans);
            Assert.IsNotNull(footTrans);
            Assert.IsNotNull(moveTargetTrans);
        }

#if UNITY_EDITOR
        const float rad = 30.0f;
        const float walkHeightRate = 0.1f;

        public void SetTargetTrans(Transform root, Transform foot, Transform moveTarget)
        {
            this.rootTrans = root;
            this.footTrans = foot;
            this.moveTargetTrans = moveTarget;

            legHeight = (rootTrans.position - footTrans.position).y;
            RecalcWalkSettings();
        }

        public void RecalcWalkSettings()
        {
            walkLength = Mathf.Tan(rad * Mathf.Deg2Rad) * legHeight;
            walkHeight = legHeight * walkHeightRate;
        }
#endif
        private void Update()
        {
            if (CheckGround() == false)
            {
                if (lerp >= 1.0f)
                {
                    FloatFoot();
                }
                else
                {
                    FloatSlerp();
                }
                return;
            }

            if (groundedPos == null)
            {
                SetGround();
                return;
            }

            var vec = this.footTrans.position - this.rootTrans.position;
            var dot = Vector3.Dot(this.rootTrans.right, vec);
            isSlide = dot * dot > slideLength * slideLength;

            if (this.IsHold && isSlide)
            {
                this.IsHold = false;
                lerp = 0;
            }

            if (this.IsHold)
            {
                this.SelfTrans.position = groundedPos.Value;

                lerp = 1;
            }
            else
            {
                if (lerp >= 1.0f)
                {
                    if (CheckMoveTarget(out var m_pos))
                    {
                        lerp = 0;
                    }
                    else
                    {
                        this.SelfTrans.position = groundedPos.Value;
                    }
                }
                else
                {
                    SlerpPos(groundedPos.Value);
                }
            }
        }

        private void FloatSlerp()
        {
            var grounded = GroundedPos(this.MoveTargetPosition);
            var pos = Vector3.Lerp(this.SelfTrans.position, grounded, lerp);
            lerp += Time.deltaTime / lerpTime;

            this.SelfTrans.position = pos;
        }

        private bool SlerpPos(Vector3 gpos)
        {
            var grounded = GroundedPos(this.MoveTargetPosition);
            var pos = Vector3.Lerp(gpos, grounded, lerp);

            if (!isSlide) {
                pos += Vector3.up * Mathf.Sin(Mathf.PI * lerp) * walkHeight;
            }

            lerp += Time.deltaTime / lerpTime;

            this.SelfTrans.position = pos;

            if (lerp > 1.0f)
            {
                this.groundedPos = grounded;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckMoveTarget(out Vector3 m_pos)
        {
            m_pos = Vector3.zero;
            if (groundedPos == null)
                return false;

            m_pos = GroundedPos(MoveTargetPosition);

            var diff = groundedPos.Value - m_pos;

            var mag = walkLength * walkLength;

            return diff.sqrMagnitude > mag;
        }

        private bool CheckGround()
        {
            return CheckGround(out var pos, out var origin, out var down);
        }

        private bool CheckGround(out Vector3 pos, out Vector3 origin, out Vector3 down)
        {
            pos = Vector3.zero;
            origin = Vector3.zero;
            down = Vector3.down;

            pos = this.SelfTrans.position;
            var rootPos = this.rootTrans.position;
            origin = this.rootTrans.position;
            down = -this.rootTrans.up;

            var ray = new Ray(origin, down);

            if (Physics.Raycast(ray, out var hit, legHeight, PhysicsUtils.FootLayer))
            {
                pos = hit.point;
                return true;
            }

            return false;
        }

        Vector3 GroundedPos(Vector3 pos)
        {
            var origin = pos + Vector3.up * legHeight;
            var ray = new Ray(origin, Vector3.down);
            if (Physics.Raycast(ray, out var hit, legHeight, PhysicsUtils.FootLayer))
            {
                return hit.point;
            }

            return pos;
        }

        private void SetGround()
        {
            if (CheckGround(out var pos, out var origin, out var down))
            {
                groundedPos = pos;
            }
            else
            {
                pos = origin + down * legHeight;
                groundedPos = pos;
            }
        }

        private void FloatFoot()
        {
            groundedPos = null;
            lerp = 0.0f;
        }

        public void SetFloat(bool isFloat)
        {
            this.IsFloat = isFloat;
        }

        public void SetHold(bool isHold)
        {
            this.IsHold = isHold;
        }

        public void SetControllerVec(Vector3 ctrlVec)
        {
            this.controllerVec = ctrlVec;
        }

        readonly Vector3 gizmosCube = new Vector3(3.0f, 0.05f, 3.0f);

        private void OnDrawGizmos()
        {
            if (this.SelfTrans != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(this.SelfTrans.position, 0.3f);
            }

            if (this.moveTargetTrans != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(this.MoveTargetPosition, 0.3f);
            }

            if (this.rootTrans != null)
            {
                Gizmos.color = Color.yellow;
                var pos = this.rootTrans.position + Vector3.down * legHeight;
                Gizmos.DrawCube(pos, gizmosCube);
            }

            if (groundedPos != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(groundedPos.Value, 0.3f);
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(FootIK))]
    public class FootIKEditor : UnityEditor.Editor
    {
        FootIK foot = null;

        private void OnEnable()
        {
            foot = target as FootIK;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (foot == null)
                return;

            if (foot.IsFloat)
            {
                if (GUILayout.Button("SetFloat:OFF"))
                    foot?.SetFloat(false);
            }
            else
            {
                if (GUILayout.Button("SetFloat:ON"))
                    foot?.SetFloat(true);
            }

            if (foot.IsHold)
            {
                if (GUILayout.Button("SetHold:OFF"))
                    foot?.SetHold(false);
            }
            else
            {
                if (GUILayout.Button("SetHold:ON"))
                    foot?.SetHold(true);
            }

            if (GUILayout.Button("RecalcWalkSettings"))
                foot?.RecalcWalkSettings();
        }
    }
#endif
}
