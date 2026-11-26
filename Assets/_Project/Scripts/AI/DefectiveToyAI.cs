using Boreatek.Player;
using Boreatek.Audio;
using UnityEngine;

namespace Boreatek.AI
{
    /// <summary>
    /// IA del Soldado de Juguete Defectuoso (concepto_juguete_de_plomo.jpg) y Gólems de Escarcha:
    /// - Patrulla el Cementerio de Trineos, los Túneles de Ventilación y las cavernas profundas.
    /// - Detecta al Duende Ingeniero a mayor distancia si éste tiene encendido el Faro Frontal LED del casco.
    /// - Ataca cuerpo a cuerpo y puede ser derrotado con el Pico Industrial de Cristal Energético o Petardos.
    /// </summary>
    public class DefectiveToyAI : MonoBehaviour
    {
        public enum AIState { Patrol, AlertChase, Attack, Disabled }

        [Header("Enemy Identity")]
        [SerializeField] private string _enemyDisplayName = "Soldado de Juguete Defectuoso";
        [SerializeField] private bool _isFrostGolemGuardian;
        [SerializeField] private float _maxHealth = 75f;

        [Header("Detection & Movement")]
        [SerializeField] private float _patrolSpeed = 2.4f;
        [SerializeField] private float _chaseSpeed = 4.6f;
        [SerializeField] private float _baseDetectionRadius = 11f;
        [SerializeField] private float _headlampDetectionBonus = 7f;
        [SerializeField] private float _attackRange = 2.1f;
        [SerializeField] private float _attackDamage = 14f;
        [SerializeField] private float _attackCooldownSeconds = 1.4f;

        private float _currentHealth;
        private AIState _currentState = AIState.Patrol;
        private Vector3 _spawnOrigin;
        private Vector3 _currentPatrolTarget;
        private float _attackCooldownTimer;
        private float _respawnTimer;
        private PlayerController _targetPlayer;
        private TemperatureSystem _targetPlayerTemp;
        private EnemyPose _pose;
        private float _attackElapsed, _collapseElapsed, _hurtRemaining, _wakeRemaining;
        private float _stepDistance;
        private bool _attacking, _collapsing;
        private float AttackDuration => _isFrostGolemGuardian ? 1.0f : 0.68f;
        private float CollapseDuration => _isFrostGolemGuardian ? 1.13f : 0.94f;
        private const float ImpactPhase = 0.51f;
        private const float HurtDuration = 0.28f;
        private const float WakeDuration = 0.57f;

        internal void BindPose(EnemyPose pose) => _pose = pose;

        public string EnemyDisplayName => _enemyDisplayName;
        public AIState CurrentState => _currentState;

        [System.Serializable]
        internal struct SaveState
        {
            public Vector3 position, patrolTarget;
            public Quaternion rotation;
            public AIState state;
            public float health, attackCooldown, respawnSeconds;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            position = transform.position, rotation = transform.rotation, patrolTarget = _currentPatrolTarget,
            state = _currentState, health = _currentHealth, attackCooldown = _attackCooldownTimer,
            respawnSeconds = _respawnTimer
        };

        internal void RestoreSave(SaveState state)
        {
            transform.SetPositionAndRotation(state.position, state.rotation);
            _currentPatrolTarget = state.patrolTarget;
            _currentState = state.state;
            _currentHealth = state.health;
            _attackCooldownTimer = state.attackCooldown;
            _respawnTimer = state.respawnSeconds;
            _attacking = _collapsing = false;
            _attackElapsed = _collapseElapsed = _hurtRemaining = _wakeRemaining = 0f;
            _stepDistance = 0f;
            bool active = _currentState != AIState.Disabled;
            SetRenderersActive(active);
            SetCollidersActive(active);
            if (active) _pose?.Animate(0f, 0f, 0f, 0f, 0f);
        }

        private void Awake()
        {
            _spawnOrigin = transform.position;
            _currentHealth = _maxHealth;
            PickNextPatrolPoint();
        }

        public void InitializeRuntime(string enemyName, bool isFrostGolem, float maxHp, float speed)
        {
            _enemyDisplayName = enemyName;
            _isFrostGolemGuardian = isFrostGolem;
            _maxHealth = maxHp;
            _currentHealth = maxHp;
            _chaseSpeed = speed;
            _spawnOrigin = transform.position;
            PickNextPatrolPoint();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            if (_currentState == AIState.Disabled)
            {
                AdvanceCollapse(dt);
                _respawnTimer -= dt;
                if (_respawnTimer <= 0f)
                {
                    RespawnAutomaton();
                }
                return;
            }

            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= dt;
            }

            EnsurePlayerReference();
            EvaluateSensors();
            Vector3 before = transform.position;
            if (_attacking)
            {
                AdvanceAttack(dt);
            }
            else if (_wakeRemaining <= 0f)
            {
                switch (_currentState)
                {
                    case AIState.Patrol:
                        UpdatePatrol(dt);
                        break;
                    case AIState.AlertChase:
                        UpdateChase(dt);
                        break;
                    case AIState.Attack:
                        UpdateAttack(dt);
                        break;
                }
            }
            float traveled = Vector3.Distance(before, transform.position);
            _stepDistance += traveled;
            float stride = _isFrostGolemGuardian ? 0.78f : 0.61f;
            if (_stepDistance >= stride)
            {
                _stepDistance = Mathf.Repeat(_stepDistance, stride);
                BoreatekSoundscape.Play(_isFrostGolemGuardian ? SoundCue.GolemStep : SoundCue.Servo, transform.position, 0.43f);
            }
            _pose?.Animate(dt, traveled,
                _attacking ? _attackElapsed / AttackDuration : 0f, 1f - _hurtRemaining / HurtDuration,
                _wakeRemaining / WakeDuration);
            _hurtRemaining = Mathf.Max(0f, _hurtRemaining - dt);
            _wakeRemaining = Mathf.Max(0f, _wakeRemaining - dt);
        }

        private void EnsurePlayerReference()
        {
            if (_targetPlayer == null)
            {
                _targetPlayer = FindAnyObjectByType<PlayerController>();
                if (_targetPlayer != null)
                {
                    _targetPlayerTemp = _targetPlayer.GetComponent<TemperatureSystem>();
                }
            }
        }

        private void EvaluateSensors()
        {
            if (_targetPlayer == null)
            {
                _currentState = AIState.Patrol;
                return;
            }

            // Los enemigos nunca entran al perímetro seguro de la Aldea Industrial de Santa
            Vector3 playerPos = _targetPlayer.transform.position;
            if (IsVillageSafe(playerPos))
            {
                _currentState = AIState.Patrol;
                return;
            }

            float detectionRadius = _baseDetectionRadius;
            if (_targetPlayer.IsHeadlampEnabled)
            {
                detectionRadius += _headlampDetectionBonus;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerPos);
            if (distToPlayer <= _attackRange)
            {
                _currentState = AIState.Attack;
            }
            else if (distToPlayer <= detectionRadius)
            {
                _currentState = AIState.AlertChase;
            }
            else if (distToPlayer > detectionRadius * 1.45f)
            {
                _currentState = AIState.Patrol;
            }
        }

        private void UpdatePatrol(float dt)
        {
            Vector3 toTarget = _currentPatrolTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 1.2f)
            {
                PickNextPatrolPoint();
                return;
            }

            Vector3 dir = toTarget.normalized;
            transform.position += dir * (_patrolSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
        }

        private void UpdateChase(float dt)
        {
            if (_targetPlayer == null)
            {
                return;
            }

            Vector3 toPlayer = _targetPlayer.transform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.05f)
            {
                Vector3 dir = toPlayer.normalized;
                transform.position += dir * (_chaseSpeed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 10f);
            }
        }

        private static bool IsVillageSafe(Vector3 position) =>
            position.x * position.x + position.z * position.z < 400f && position.y > -1.5f;

        private void UpdateAttack(float dt)
        {
            FacePlayer(dt);
            if (_targetPlayer == null || _attackCooldownTimer > 0f || _wakeRemaining > 0f) return;
            _attackCooldownTimer = Mathf.Max(_attackCooldownSeconds, AttackDuration);
            _attackElapsed = 0f;
            _attacking = true;
            BoreatekSoundscape.Play(_isFrostGolemGuardian ? SoundCue.GolemReady : SoundCue.RobotReady, transform.position);
        }

        private void AdvanceAttack(float dt)
        {
            float impact = AttackDuration * ImpactPhase;
            float previous = _attackElapsed;
            if (previous < impact) FacePlayer(dt);
            _attackElapsed += dt;
            if (previous < impact && _attackElapsed >= impact) ApplyAttackImpact();
            if (_attackElapsed >= AttackDuration) _attacking = false;
        }

        private void FacePlayer(float dt)
        {
            if (_targetPlayer == null) return;
            Vector3 direction = Vector3.ProjectOnPlane(_targetPlayer.transform.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 12f);
        }

        private void ApplyAttackImpact()
        {
            BoreatekSoundscape.Play(_isFrostGolemGuardian ? SoundCue.GolemStrike : SoundCue.RobotStrike, transform.position);
            if (_targetPlayerTemp == null || _targetPlayerTemp.CurrentHealth <= 0f ||
                _currentState != AIState.Attack || IsVillageSafe(_targetPlayer.transform.position)) return;
            Vector3 origin = transform.position + Vector3.up * (_isFrostGolemGuardian ? 1.45f : 1.0f);
            Vector3 target = _targetPlayer.transform.position + Vector3.up * 0.58f;
            if (Physics.Linecast(origin, target, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<PlayerController>() != _targetPlayer) return;
            _targetPlayerTemp.ApplyCombatDamage(_attackDamage, transform.position);
            _targetPlayer.PublishStatus($"⚠️ ¡{_enemyDisplayName} te atacó (-{_attackDamage} HP)! Usa tu Pico [Clic Izq] o Bastón de Menta [3].");
        }

        private void AdvanceCollapse(float dt)
        {
            if (!_collapsing) return;
            float previous = _collapseElapsed;
            _collapseElapsed += dt;
            if (previous < CollapseDuration * 0.73f && _collapseElapsed >= CollapseDuration * 0.73f)
                BoreatekSoundscape.Play(_isFrostGolemGuardian ? SoundCue.GolemStep : SoundCue.RobotStrike, transform.position, 0.84f);
            _pose?.Collapse(Mathf.Clamp01(_collapseElapsed / CollapseDuration));
            if (_collapseElapsed < CollapseDuration + 0.33f) return;
            _collapsing = false;
            SetRenderersActive(false);
        }

        public void TakePickaxeDamage(float damage, int pickaxeTier, PlayerController attacker)
        {
            if (_currentState == AIState.Disabled)
            {
                return;
            }

            if (_isFrostGolemGuardian && pickaxeTier < 2)
            {
                BoreatekSoundscape.Play(SoundCue.Blocked, transform.position);
                attacker?.PublishStatus($"🛡️ La coraza de {_enemyDisplayName} rebota el Pico Nivel 1. Usa Pico Nivel 2+ o Petardos Festivos [Q].");
                return;
            }

            _currentHealth -= damage;
            BoreatekSoundscape.Play(_isFrostGolemGuardian ? SoundCue.Ice : SoundCue.Metal, transform.position);
            if (_currentHealth <= 0f)
            {
                _currentState = AIState.Disabled;
                _respawnTimer = 75f;
                _attacking = false;
                _collapsing = _pose != null;
                _collapseElapsed = 0f;
                BoreatekSoundscape.Play(_isFrostGolemGuardian ? SoundCue.GolemOff : SoundCue.RobotOff, transform.position);
                SetCollidersActive(false);
                if (!_collapsing) SetRenderersActive(false);
                attacker?.PublishStatus($"💥 ¡{_enemyDisplayName} neutralizado!");
            }
            else
            {
                _hurtRemaining = HurtDuration;
                attacker?.PublishStatus($"⚔️ Golpeaste a {_enemyDisplayName} ({Mathf.CeilToInt(_currentHealth)}/{Mathf.CeilToInt(_maxHealth)} HP).");
            }
        }

        private void RespawnAutomaton()
        {
            _currentHealth = _maxHealth;
            transform.position = _spawnOrigin;
            _currentState = AIState.Patrol;
            _attacking = _collapsing = false;
            _attackElapsed = _collapseElapsed = _hurtRemaining = 0f;
            _wakeRemaining = WakeDuration;
            _stepDistance = 0f;
            BoreatekSoundscape.Play(SoundCue.Reboot, transform.position, 0.84f);
            SetRenderersActive(true);
            SetCollidersActive(true);
            _pose?.Animate(0f, 0f, 0f, 0f, 1f);
            PickNextPatrolPoint();
        }

        private void PickNextPatrolPoint()
        {
            Vector2 offset = Random.insideUnitCircle * 9f;
            _currentPatrolTarget = _spawnOrigin + new Vector3(offset.x, 0f, offset.y);
        }

        private void SetRenderersActive(bool active)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = active;
            }
        }

        private void SetCollidersActive(bool active)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = active;
            }
        }
    }
}
