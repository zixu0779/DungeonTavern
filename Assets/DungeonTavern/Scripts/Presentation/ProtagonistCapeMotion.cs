using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    // Matches the source rig's front-cloak lift after Humanoid retargeting.
    [DefaultExecutionOrder(250)]
    public sealed class ProtagonistCapeMotion : MonoBehaviour
    {
        [SerializeField] private Transform frontCape;
        [SerializeField] private Transform upperLeg;
        [SerializeField] private Transform lowerLeg;
        [SerializeField] private Quaternion capeRestRotation;
        [SerializeField] private Vector3 legRestDirection;
        [SerializeField] private Vector3 legBendAxis;
        [SerializeField] private Vector3 capeBendAxis;

        public void Configure(Transform cape, Transform thigh, Transform knee)
        {
            frontCape=cape; upperLeg=thigh; lowerLeg=knee;
            capeRestRotation=cape.localRotation;
            legRestDirection=thigh.parent.InverseTransformDirection(knee.position-thigh.position).normalized;
            legBendAxis=thigh.parent.InverseTransformDirection(transform.right);
            capeBendAxis=cape.parent.InverseTransformDirection(transform.right);
        }

        public void Evaluate()
        {
            if (!frontCape || !upperLeg || !lowerLeg) return;
            var direction=upperLeg.parent.InverseTransformDirection(lowerLeg.position-upperLeg.position);
            float bend=Vector3.SignedAngle(Vector3.ProjectOnPlane(legRestDirection,legBendAxis),
                Vector3.ProjectOnPlane(direction,legBendAxis),legBendAxis);
            // Forward thigh flexion lifts the cloth; backward swing leaves it hanging.
            frontCape.localRotation=Quaternion.AngleAxis(Mathf.Clamp(bend,-110f,0f)*.95f,capeBendAxis)*capeRestRotation;
        }
        private void LateUpdate() => Evaluate();
        private void OnDisable() { if(frontCape)frontCape.localRotation=capeRestRotation; }
    }
}
