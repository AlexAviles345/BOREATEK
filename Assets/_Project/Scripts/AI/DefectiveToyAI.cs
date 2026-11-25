using Boreatek.Player;
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

        [Header("Visual Eyes & Sparks")]
        [SerializeField] private Renderer[] _amberEyeRenderers;

        private float _currentHealth;
        private AIState _currentState = AIState.Patrol;
        private Vector3 _spawnOrigin;
        private Vector3 _currentPatrolTarget;
        private float _attackCooldownTimer;
        private float _respawnTimer;
        private PlayerController _targetPlayer;
        private TemperatureSystem _targetPlayerTemp;

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
            SetVisualActive(_currentState != AIState.Disabled);
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

            switch (_currentState)
            {
                case AIState.Patrol:
                    UpdatePatrol(dt);
                    break;
                case AIState.AlertChase:
                    UpdateChase(dt);
                    break;
                case AIState.Attack:
                    UpdateAttack();
                    break;
            }
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
            if (new Vector2(playerPos.x, playerPos.z).magnitude < 20f && playerPos.y > -1.5f)
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

        private void UpdateAttack()
        {
            if (_targetPlayer == null || _attackCooldownTimer > 0f)
            {
                return;
            }

            _attackCooldownTimer = _attackCooldownSeconds;
            if (_targetPlayerTemp != null)
            {
                _targetPlayerTemp.ApplyCombatDamage(_attackDamage);
                _targetPlayer.PublishStatus($"⚠️ ¡{_enemyDisplayName} te atacó (-{_attackDamage} HP)! Usa tu Pico [Clic Izq] o Bastón de Menta [3].");
            }
        }

        public void TakePickaxeDamage(float damage, int pickaxeTier, PlayerController attacker)
        {
            if (_currentState == AIState.Disabled)
            {
                return;
            }

            if (_isFrostGolemGuardian && pickaxeTier < 2)
            {
                attacker?.PublishStatus($"🛡️ La coraza de {_enemyDisplayName} rebota el Pico Nivel 1. Usa Pico Nivel 2+ o Petardos Festivos [Q].");
                return;
            }

            _currentHealth -= damage;
            if (_currentHealth <= 0f)
            {
                _currentState = AIState.Disabled;
                _respawnTimer = 75f;
                SetVisualActive(false);
                attacker?.PublishStatus($"💥 ¡{_enemyDisplayName} neutralizado!");
            }
            else
            {
                attacker?.PublishStatus($"⚔️ Golpeaste a {_enemyDisplayName} ({Mathf.CeilToInt(_currentHealth)}/{Mathf.CeilToInt(_maxHealth)} HP).");
            }
        }

        private void RespawnAutomaton()
        {
            _currentHealth = _maxHealth;
            transform.position = _spawnOrigin;
            _currentState = AIState.Patrol;
            SetVisualActive(true);
            PickNextPatrolPoint();
        }

        private void PickNextPatrolPoint()
        {
            Vector2 offset = Random.insideUnitCircle * 9f;
            _currentPatrolTarget = _spawnOrigin + new Vector3(offset.x, 0f, offset.y);
        }

        private void SetVisualActive(bool active)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = active;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = active;
            }
        }
    }
}
