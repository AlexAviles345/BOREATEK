using Boreatek.Player;
using Boreatek.Audio;
using UnityEngine;
using UnityEngine.AI;

namespace Boreatek.World
{
    public sealed class FoundryWorker : MonoBehaviour
    {
        internal enum Job { Smith, Packer, Mechanic, Cook, Sweeper, Courier }
        private enum Routine { Work, Travel, Break }

        private ElfPose _pose;
        private Job _job;
        private Transform _valve;
        private GameObject _tool;
        private Vector3 _waist;
        private float _time;
        private Vector3 _work, _rest, _restAlternative, _destination;
        private Quaternion _workFacing;
        private NavMeshAgent _agent;
        private NavMeshQueryFilter _query;
        private NavMeshPath _route;
        private Routine _routine;
        private float _remaining, _phase;
        private bool _returning, _alternate;

        internal void Initialize(ElfPose pose, Job job, Transform valve, GameObject tool, float offset, Vector3 rest)
        {
            _pose = pose;
            _job = job;
            _valve = valve;
            _tool = tool;
            _waist = pose.Waist.localPosition;
            _time = offset;
            _phase = offset;
            _work = transform.position;
            _workFacing = transform.rotation;
            _rest = rest;
            _restAlternative = rest + new Vector3(rest.x < 0f ? -1.65f : 1.65f, 0f, -1.35f);
            _remaining = 7f + 15f * offset;
            UpdatePose();
        }

        internal void EnableRoute(int agentType)
        {
            _query = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(_work, out NavMeshHit start, 0.72f, _query)) return;
            transform.position = start.position;
            _work = start.position;
            _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.agentTypeID = agentType;
            _agent.radius = 0.26f;
            _agent.height = 1.24f;
            _agent.speed = _job == Job.Courier ? 1.83f : 1.36f + _phase * 0.33f;
            _agent.acceleration = 5f;
            _agent.avoidancePriority = 29 + Mathf.RoundToInt(_phase * 33f);
            _agent.stoppingDistance = 0.14f;
            _agent.updateRotation = false;
            _agent.Warp(start.position);
            _route = new NavMeshPath();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_pose == null || dt <= 0f) return;
            AdvanceRoutine(dt);
            float previous = _time;
            _time += dt;
            if (_tool != null) _tool.SetActive(_routine == Routine.Work || _job == Job.Courier);
            if (_routine == Routine.Work)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, _workFacing, 320f * dt);
                UpdatePose();
                if (_job == Job.Smith && Mathf.Floor((previous - 0.9246f) / 1.38f) < Mathf.Floor((_time - 0.9246f) / 1.38f))
                    BoreatekSoundscape.Play(SoundCue.Metal, transform.position, 0.48f);
                return;
            }
            Vector3 movement = _agent != null ? _agent.velocity : Vector3.zero;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.014f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(movement), 420f * dt);
            _pose.UpdateTravel(transform.InverseTransformDirection(movement.normalized), movement.magnitude * 0.64f, false, true, dt);
            _pose.Update(dt);
            if (_routine == Routine.Break)
                _pose.Neck.localRotation = Quaternion.Euler(3f, Mathf.Sin(_time * 0.83f + _phase * 4f) * 29f, 0f);
        }

        private void AdvanceRoutine(float dt)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            _remaining -= dt;
            if (_routine == Routine.Travel)
            {
                Vector3 delta = _destination - transform.position;
                delta.y = 0f;
                if (!_agent.pathPending && delta.sqrMagnitude <= 0.115f)
                {
                    _agent.ResetPath();
                    EnterRoutine(_returning ? Routine.Work : Routine.Break);
                }
                else if (_remaining <= 0f)
                {
                    _agent.ResetPath();
                    EnterRoutine(Routine.Break);
                }
                return;
            }
            if (_remaining > 0f) return;
            _returning = _routine == Routine.Break;
            Vector3 firstRest = _alternate ? _restAlternative : _rest;
            Vector3 secondRest = _alternate ? _rest : _restAlternative;
            if (_returning ? TryTravel(_work) : TryTravel(firstRest) || TryTravel(secondRest))
            {
                if (!_returning) _alternate = !_alternate;
                _pose.Reset();
                EnterRoutine(Routine.Travel);
                return;
            }
            _remaining = 3f;
        }

        private bool TryTravel(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out NavMeshHit goal, 1.3f, _query) ||
                !_agent.CalculatePath(goal.position, _route) || _route.status != NavMeshPathStatus.PathComplete ||
                !_agent.SetPath(_route)) return false;
            _destination = goal.position;
            return true;
        }

        private void EnterRoutine(Routine routine)
        {
            _routine = routine;
            _remaining = routine switch
            {
                Routine.Work => 15f + _phase * 11f,
                Routine.Break => 5f + _phase * 5f,
                _ => 38f
            };
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
                case Job.Cook:
                {
                    float stir = Mathf.Sin(_time * 4.1f);
                    _pose.Waist.localRotation = Quaternion.Euler(11f, stir * 9f, 0f);
                    _pose.Neck.localRotation = Quaternion.Euler(19f, -4f, 0f);
                    AimArm(_pose.LeftArm, -44f, 10f, -4f, -28f);
                    AimArm(_pose.RightArm, -50f + stir * 20f, stir * 22f, 4f, -23f);
                    break;
                }
                case Job.Sweeper:
                {
                    float sweep = Mathf.Sin(_time * 3.2f);
                    _pose.Waist.localRotation = Quaternion.Euler(18f, sweep * 23f, -5f);
                    _pose.Neck.localRotation = Quaternion.Euler(24f, sweep * 9f, 0f);
                    AimArm(_pose.LeftArm, -38f, sweep * 19f, 7f, -27f);
                    AimArm(_pose.RightArm, -35f + sweep * 26f, sweep * 22f, -15f, -14f);
                    break;
                }
                case Job.Courier:
                {
                    float sort = Mathf.Sin(_time * 3.5f);
                    _pose.Waist.localRotation = Quaternion.Euler(8f + sort * 6f, sort * 12f, 0f);
                    _pose.Neck.localRotation = Quaternion.Euler(15f, 7f, 0f);
                    AimArm(_pose.LeftArm, -54f, 9f, -4f, -40f);
                    AimArm(_pose.RightArm, -64f - sort * 10f, -11f, 7f, -32f);
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
