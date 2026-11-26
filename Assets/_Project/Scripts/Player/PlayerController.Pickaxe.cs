using Boreatek.AI;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.Player
{
    internal readonly struct PickaxeStroke
    {
        internal const float ImpactPhase = 0.54f;
        internal static readonly Vector3 BladeTip = new Vector3(0.355f, 0.316f, 0.048f);
        internal static readonly Vector3 BladeBase = new Vector3(0.214f, 0.380f, 0.027f);
        internal static readonly Quaternion BladeForward =
            Quaternion.Inverse(Quaternion.LookRotation(BladeTip - BladeBase, Vector3.up));

        internal readonly float Raise, Contact, Follow;
        internal float Weight => Raise + Contact + Follow;

        internal PickaxeStroke(float phase)
        {
            float load = Ease(0f, 0.31f, phase);
            float down = Ease(0.31f, 0.51f, phase);
            float through = Ease(0.57f, 0.73f, phase);
            float settle = Ease(0.73f, 1f, phase);
            Raise = load - down;
            Contact = down - through;
            Follow = through - settle;
        }

        private static float Ease(float start, float end, float phase) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, phase));
    }

    public partial class PlayerController
    {
        private static readonly Vector3 FirstPersonPickaxeRest = new Vector3(0.30f, -0.34f, 0.64f);
        private static readonly Quaternion FirstPersonPickaxeIdle =
            Quaternion.LookRotation(new Vector3(-0.39f, 0.10f, 1f), Vector3.up) * PickaxeStroke.BladeForward;
        private const float FirstPersonPickaxeScale = 0.63f;
        private float _swingElapsed, _swingDuration;
        private bool _strikeIsInteraction, _showImpactPose;
        private Vector3? _impactPoint;
        private HarvestableNode _strikeNode;
        private RaycastHit _aimHit;
        private Transform _pickaxeGripParent;
        private Vector3 _pickaxeGripPosition, _pickaxeGripScale;
        private bool CanSwingPickaxe => _swingCooldownTimer <= 0f && _swingDuration <= 0f;

        private void PerformPickaxeSwing(HarvestableNode interactionNode = null)
        {
            float cooldown = IsGingerbreadCookieActive ? _swingCooldownSeconds * 0.65f : _swingCooldownSeconds;
            _swingCooldownTimer = Mathf.Max(0.05f, cooldown);
            _swingDuration = Mathf.Min(0.33f, _swingCooldownTimer);
            _swingElapsed = 0f;
            _impactPoint = null;
            _strikeNode = interactionNode;
            _strikeIsInteraction = interactionNode != null;
        }

        private void AdvancePickaxeSwing(float dt)
        {
            if (_swingDuration <= 0f) return;
            float impact = _swingDuration * PickaxeStroke.ImpactPhase;
            float previous = _swingElapsed;
            _swingElapsed += dt;
            _showImpactPose = previous < impact && _swingElapsed >= impact;
            if (_showImpactPose)
            {
                _impactPoint = _aimHit.collider != null ? _aimHit.point : (Vector3?)null;
                ApplyPickaxeImpact();
            }
            else if (_swingElapsed >= _swingDuration)
            {
                CancelPickaxeSwing();
            }
        }

        private void ApplyPickaxeImpact()
        {
            if (_aimHit.collider == null) return;
            HarvestableNode node = _aimHit.collider.GetComponentInParent<HarvestableNode>();
            if (_strikeIsInteraction && (node == null || node != _strikeNode)) return;
            if (node != null)
            {
                node.HitWithPickaxe(_currentPickaxeTier, _pickaxeDamage, _inventoryAndShop, this);
            }
            else if (_aimHit.collider.GetComponentInParent<DefectiveToyAI>() is DefectiveToyAI enemy)
            {
                enemy.TakePickaxeDamage(_pickaxeDamage * 25f, _currentPickaxeTier, this);
            }
        }

        private bool TryGetAimHit(Ray ray, float reach, out RaycastHit hit)
        {
            if (!Physics.Raycast(ray, out hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            if (hit.collider != _characterController) return true;

            // Looking steeply down in first person can put the player's capsule in front of the reticle.
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit candidate in Physics.RaycastAll(ray, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (candidate.collider == _characterController || candidate.distance >= nearest) continue;
                hit = candidate;
                nearest = candidate.distance;
            }
            return nearest < float.PositiveInfinity;
        }

        private void UpdateCharacterAnimation(float dt)
        {
            float phase = _swingDuration > 0f ? _swingElapsed / _swingDuration : 0f;
            var stroke = new PickaxeStroke(_showImpactPose ? PickaxeStroke.ImpactPhase : phase);
            Vector3? point = phase >= PickaxeStroke.ImpactPhase
                ? _impactPoint
                : _aimHit.collider != null ? _aimHit.point : (Vector3?)null;
            Transform view = AimFrame;
            Vector3 aim = view.forward;
            if (stroke.Weight > 0f && point.HasValue)
            {
                Vector3 target = point.Value - transform.TransformPoint(new Vector3(0f, 0.78f, 0f));
                if (Vector3.Dot(target, view.forward) > 0.04f) aim = target.normalized;
            }
            _elfPose?.Update(dt, stroke, transform.InverseTransformDirection(aim));
            if (_pickaxeVisualRoot == null) return;

            if (InFirstPerson && _pickaxeGripParent != null && _playerCamera != null)
            {
                PoseFirstPersonPickaxe(stroke, view, point);
                return;
            }
            if (stroke.Weight <= 0f)
            {
                _pickaxeVisualRoot.localRotation = _initialPickaxeLocalRot;
                return;
            }

            if (point.HasValue)
            {
                Vector3 target = point.Value - _pickaxeVisualRoot.position;
                if (Vector3.Dot(target, view.forward) > 0.04f) aim = target.normalized;
            }
            Quaternion facing = Quaternion.LookRotation(aim, view.up) * PickaxeStroke.BladeForward;
            Quaternion swung = facing;
            if (stroke.Raise > 0f)
            {
                Quaternion raised = transform.rotation * Quaternion.Euler(-27f, -33f, -48f);
                swung = Quaternion.Slerp(facing, raised, stroke.Raise / stroke.Weight);
            }
            else if (stroke.Follow > 0f)
            {
                swung = Quaternion.AngleAxis(47f * stroke.Follow / stroke.Weight, view.right) * facing;
            }
            Transform mount = _pickaxeVisualRoot.parent;
            Quaternion restWorld = mount != null ? mount.rotation * _initialPickaxeLocalRot : _initialPickaxeLocalRot;
            _pickaxeVisualRoot.rotation = Quaternion.Slerp(restWorld, swung, stroke.Weight);
        }

        private void PoseFirstPersonPickaxe(PickaxeStroke stroke, Transform view, Vector3? point)
        {
            // Author the first-person arc in screen space so the hand rises, strikes inward, then settles.
            Vector3 grip = FirstPersonPickaxeRest * (1f - stroke.Weight) +
                new Vector3(0.36f, -0.18f, 0.61f) * stroke.Raise +
                new Vector3(0.16f, -0.225f, 0.83f) * stroke.Contact +
                new Vector3(0.22f, -0.38f, 0.75f) * stroke.Follow;
            Vector3 path = view.TransformDirection(grip + Vector3.forward * 0.19f);
            float clearance = Mathf.Clamp(ViewClearance(view.position, path, 0.095f) / path.magnitude, 0.30f, 1f);
            _pickaxeVisualRoot.localPosition = _firstPersonCameraOffset + grip * clearance;
            _pickaxeVisualRoot.localScale = _pickaxeGripScale * (FirstPersonPickaxeScale * clearance);

            Quaternion idle = view.rotation * FirstPersonPickaxeIdle;
            if (stroke.Weight <= 0f)
            {
                _pickaxeVisualRoot.rotation = idle;
                return;
            }

            Vector3 direction = view.forward;
            if (point.HasValue)
            {
                Vector3 target = point.Value - _pickaxeVisualRoot.position;
                if (Vector3.Dot(target, view.forward) > 0.05f) direction = target.normalized;
            }
            Quaternion impact = Quaternion.LookRotation(direction, view.up) * PickaxeStroke.BladeForward;
            Quaternion raised = view.rotation * Quaternion.LookRotation(new Vector3(-0.11f, 0.61f, 0.77f), Vector3.up) *
                PickaxeStroke.BladeForward;
            Quaternion pose = stroke.Raise > 0f
                ? Quaternion.Slerp(impact, raised, stroke.Raise / stroke.Weight)
                : Quaternion.AngleAxis(55f * stroke.Follow / stroke.Weight, view.right) * impact;
            _pickaxeVisualRoot.rotation = Quaternion.Slerp(idle, pose, stroke.Weight);
        }

        private void RefreshPickaxeView()
        {
            if (_pickaxeGripParent == null || _cameraPivot == null) return;
            _pickaxeVisualRoot.SetParent(InFirstPerson ? _cameraPivot : _pickaxeGripParent, false);
            _pickaxeVisualRoot.localScale = _pickaxeGripScale * (InFirstPerson ? FirstPersonPickaxeScale : 1f);
            _pickaxeVisualRoot.localPosition = InFirstPerson
                ? _firstPersonCameraOffset + FirstPersonPickaxeRest : _pickaxeGripPosition;
            _pickaxeVisualRoot.localRotation = InFirstPerson ? FirstPersonPickaxeIdle : _initialPickaxeLocalRot;
        }

        private void CancelPickaxeSwing()
        {
            _swingDuration = 0f;
            _swingElapsed = 0f;
            _strikeNode = null;
            _showImpactPose = false;
            _impactPoint = null;
        }
    }
}
