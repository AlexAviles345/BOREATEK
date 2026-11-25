using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    public sealed class FoundryWorker : MonoBehaviour
    {
        internal enum Job { Smith, Packer, Mechanic }

        private ElfPose _pose;
        private Job _job;
        private Transform _valve;
        private Vector3 _waist;
        private float _time;

        internal void Initialize(ElfPose pose, Job job, Transform valve, float offset)
        {
            _pose = pose;
            _job = job;
            _valve = valve;
            _waist = pose.Waist.localPosition;
            _time = offset;
            UpdatePose();
        }

        private void Update()
        {
            if (_pose == null) return;
            _time += Time.deltaTime;
            UpdatePose();
        }

        private void UpdatePose()
        {
            switch (_job)
            {
                case Job.Smith:
                {
                    float cycle = Mathf.Repeat(_time / 1.38f, 1f);
                    float raised = Ease(0.03f, 0.50f, cycle) - Ease(0.52f, 0.67f, cycle);
                    float strike = Ease(0.52f, 0.67f, cycle) - Ease(0.68f, 0.94f, cycle);
                    _pose.Waist.localPosition = _waist - Vector3.up * (strike * 0.048f);
                    _pose.Waist.localRotation = Quaternion.Euler(6f + strike * 19f - raised * 8f, -raised * 12f, raised * 9f);
                    _pose.Neck.localRotation = Quaternion.Euler(14f + strike * 16f, -7f, 0f);
                    AimArm(_pose.LeftArm, -45f, 17f, 0f, 8f);
                    AimArm(_pose.RightArm, -28f - raised * 110f + strike * 17f, 9f, 17f + raised * 17f, 18f - raised * 46f);
                    break;
                }
                case Job.Packer:
                {
                    float lean = Mathf.Sin(_time * 2.8f);
                    float reach = Mathf.Sin(_time * 5.6f);
                    _pose.Waist.localRotation = Quaternion.Euler(11f + 5f * reach, 16f * lean, 6f * lean);
                    _pose.Neck.localRotation = Quaternion.Euler(13f, 10f * lean, -2f * lean);
                    AimArm(_pose.LeftArm, -47f - 18f * reach, 18f + lean * 20f, 0f, -17f);
                    AimArm(_pose.RightArm, -57f + 22f * reach, -17f + lean * 21f, 5f, -20f + lean * 19f);
                    break;
                }
                case Job.Mechanic:
                {
                    float turn = Mathf.Sin(_time * 2.6f);
                    float lift = Mathf.Cos(_time * 2.6f);
                    _pose.Waist.localRotation = Quaternion.Euler(7f, turn * 7f, -lift * 6f);
                    _pose.Neck.localRotation = Quaternion.Euler(-8f + lift * 5f, turn * 7f, 0f);
                    AimArm(_pose.LeftArm, -76f - turn * 23f, 14f, -6f, lift * 23f);
                    AimArm(_pose.RightArm, -76f + turn * 23f, -14f, 6f, -lift * 23f);
                    if (_valve != null) _valve.localRotation = Quaternion.AngleAxis(turn * 62f, Vector3.forward);
                    break;
                }
            }
        }

        private static void AimArm(ElfPose.Arm arm, float pitch, float yaw, float roll, float elbow)
        {
            arm.Shoulder.localRotation = Quaternion.Euler(pitch, yaw, roll);
            arm.Elbow.localRotation = Quaternion.Euler(elbow, 0f, 0f);
        }

        private static float Ease(float start, float end, float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
    }
}
