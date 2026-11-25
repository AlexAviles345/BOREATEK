using System;
using Boreatek.Core;
using Boreatek.Economy;
using Boreatek.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Boreatek.Player
{
    /// <summary>
    /// Controlador 3D del protagonista (Duende Ingeniero / Minero):
    /// - Movimiento en nieve con soporte de Primera / Tercera persona estilizada.
    /// - Casco Minero Metálico con Faro Frontal LED para cuevas oscuras y túneles.
    /// - Pico Industrial con Cristal Energético Rojo para extracción y defensa.
    /// - Despliegue de Guirnaldas Luminosas, Balizas Térmicas y Petardos Festivos.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(TemperatureSystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion Settings")]
        [SerializeField] private float _walkSpeed = 6.0f;
        [SerializeField] private float _sprintSpeed = 9.2f;
        [SerializeField] private float _jumpHeight = 1.45f;
        [SerializeField] private float _gravity = -20.0f;
        [SerializeField] private float _mouseSensitivity = 0.16f;

        [Header("Camera & Third-Person / First-Person Rig")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private bool _useThirdPersonView = true;
        [SerializeField] private Vector3 _thirdPersonCameraOffset = new Vector3(0.55f, 0.65f, -3.4f);
        [SerializeField] private Vector3 _firstPersonCameraOffset = new Vector3(0f, 0.45f, 0.15f);

        [Header("Elf Engineer Equipment")]
        [Tooltip("Faro LED frontal del casco minero metálico.")]
        [SerializeField] private Light _helmetHeadlamp;

        [Tooltip("Transform visual del Pico Industrial con Cristal Energético Rojo para animación de golpe.")]
        [SerializeField] private Transform _pickaxeVisualRoot;

        [Tooltip("Renderer de la punta de Cristal de Caramelo rojo del pico.")]
        [SerializeField] private Renderer _pickaxeCrystalTipRenderer;

        [Range(1, 3)]
        [SerializeField] private int _currentPickaxeTier = 1;
        [SerializeField] private int _pickaxeDamage = 1;
        [SerializeField] private float _miningReachDistance = 4.2f;
        [SerializeField] private float _swingCooldownSeconds = 0.35f;

        private CharacterController _characterController;
        private TemperatureSystem _temperatureSystem;
        private InventoryAndShop _inventoryAndShop;

        private Vector3 _verticalVelocity;
        private float _cameraPitch;
        private float _playerYaw;
        private float _swingCooldownTimer;
        private float _swingAnimationTimer;
        private float _gingerbreadCookieBuffTimer;
        private float _gingerbreadSpeedMultiplier = 1f;
        private bool _isCursorLocked = true;
        private bool _isShopMenuOpen;
        private bool _gameplayEnabled = true;
        private Transform _characterVisual;
        private IInteractable _currentFocusedInteractable;
        private Quaternion _initialPickaxeLocalRot = Quaternion.identity;

        public int CurrentPickaxeTier => _currentPickaxeTier;
        public int PickaxeDamage => _pickaxeDamage;
        public bool IsHeadlampEnabled => _helmetHeadlamp != null && _helmetHeadlamp.enabled;
        public bool IsGingerbreadCookieActive => _gingerbreadCookieBuffTimer > 0f;
        public bool IsShopMenuOpen => _isShopMenuOpen;
        public IInteractable CurrentFocusedInteractable => _currentFocusedInteractable;
        public string LastStatusMessage { get; private set; } = "Duende Ingeniero listo en la Aldea Industrial.";

        public event Action<string> OnStatusMessagePublished;
        public event Action<int> OnPickaxeTierChanged;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _temperatureSystem = GetComponent<TemperatureSystem>();
            _inventoryAndShop = GetComponent<InventoryAndShop>();
            _playerYaw = transform.eulerAngles.y;

            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }

            if (_pickaxeVisualRoot != null)
            {
                _initialPickaxeLocalRot = _pickaxeVisualRoot.localRotation;
            }
        }

        private void Start()
        {
            SetCursorLock(_gameplayEnabled);
            UpdateCameraPositionImmediate();
        }

        private void Update()
        {
            if (!_gameplayEnabled)
            {
                return;
            }

            float dt = Time.deltaTime;
            UpdateTimers(dt);
            HandleUtilityShortcuts();

            if (!_isShopMenuOpen && _isCursorLocked)
            {
                HandleLookRotation();
            }

            HandleMovement(dt);
            UpdateInteractionFocus();

            if (!_isShopMenuOpen)
            {
                HandleActionInputs();
            }

            UpdatePickaxeSwingVisual(dt);
        }

        private void UpdateTimers(float dt)
        {
            if (_swingCooldownTimer > 0f)
            {
                _swingCooldownTimer -= dt;
            }

            if (_gingerbreadCookieBuffTimer > 0f)
            {
                _gingerbreadCookieBuffTimer -= dt;
                if (_gingerbreadCookieBuffTimer <= 0f)
                {
                    _gingerbreadSpeedMultiplier = 1f;
                }
            }
        }

        private void HandleUtilityShortcuts()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            // Tab o B: Abrir / Cerrar Tienda del Taller
            if (kb.tabKey.wasPressedThisFrame || kb.bKey.wasPressedThisFrame)
            {
                ToggleShopMenu();
            }

            // F: Encender / Apagar Faro Frontal LED del Casco Minero
            if (kb.fKey.wasPressedThisFrame && _helmetHeadlamp != null)
            {
                _helmetHeadlamp.enabled = !_helmetHeadlamp.enabled;
                PublishStatus(_helmetHeadlamp.enabled
                    ? "Faro Frontal LED del casco ENCENDIDO."
                    : "Faro Frontal LED del casco APAGADO (menor detección enemiga).");
            }

            // V: Alternar vista Tercera Persona Estilizada / Primera Persona
            if (kb.vKey.wasPressedThisFrame)
            {
                _useThirdPersonView = !_useThirdPersonView;
                UpdateCameraPositionImmediate();
                PublishStatus(_useThirdPersonView
                    ? "Cámara: Tercera Persona (Duende Ingeniero visible)."
                    : "Cámara: Primera Persona (Minería de precisión).");
            }
        }

        private void HandleLookRotation()
        {
            Vector2 lookDelta = Vector2.zero;
            if (Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue() * _mouseSensitivity;
            }

            if (Gamepad.current != null)
            {
                Vector2 stickLook = Gamepad.current.rightStick.ReadValue() * (120f * Time.deltaTime);
                lookDelta += stickLook;
            }

            _playerYaw += lookDelta.x;
            _cameraPitch = Mathf.Clamp(_cameraPitch - lookDelta.y, -75f, 75f);

            transform.rotation = Quaternion.Euler(0f, _playerYaw, 0f);
            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
        }

        private void HandleMovement(float dt)
        {
            Vector2 moveInput = Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb != null && !_isShopMenuOpen)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
                moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            }

            if (Gamepad.current != null && !_isShopMenuOpen)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > moveInput.sqrMagnitude)
                {
                    moveInput = stick;
                }
            }

            bool isSprinting = (kb != null && kb.leftShiftKey.isPressed) ||
                               (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);

            float targetSpeed = isSprinting ? _sprintSpeed : _walkSpeed;
            targetSpeed *= _gingerbreadSpeedMultiplier;

            // Penalización por nieve profunda en el Cementerio de Trineos si el abrigo/botas aún son Nivel 1
            if (_temperatureSystem != null)
            {
                if (_temperatureSystem.CurrentBiome == BiomeType.SleighGraveyard &&
                    _temperatureSystem.ThermalCoatTier < 2)
                {
                    targetSpeed *= 0.75f;
                }

                // Penalización leve cuando hay advertencia de escarcha (< 30 °T)
                if (_temperatureSystem.CurrentTemperature < 30f)
                {
                    targetSpeed *= 0.85f;
                }
            }

            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            _characterController.Move(moveDirection * (targetSpeed * dt));

            bool isGrounded = _characterController.isGrounded;
            if (isGrounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2.5f;
            }

            bool jumpPressed = (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                               (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (jumpPressed && isGrounded && !_isShopMenuOpen)
            {
                _verticalVelocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _verticalVelocity.y += _gravity * dt;
            _characterController.Move(_verticalVelocity * dt);
        }

        private void UpdateInteractionFocus()
        {
            _currentFocusedInteractable = null;
            if (_playerCamera == null)
            {
                return;
            }

            float rayDist = _useThirdPersonView ? _miningReachDistance + 3.5f : _miningReachDistance;
            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, rayDist))
            {
                _currentFocusedInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }
        }

        private void HandleActionInputs()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            // Clic Izquierdo: Golpear con el Pico Industrial de Cristal Energético Rojo
            bool attackPressed = (mouse != null && mouse.leftButton.isPressed) ||
                                 (kb != null && kb.enterKey.wasPressedThisFrame) ||
                                 (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame);

            if (attackPressed && _swingCooldownTimer <= 0f)
            {
                if (!_isCursorLocked)
                {
                    SetCursorLock(true);
                }
                else
                {
                    PerformPickaxeSwing();
                }
            }

            if (kb == null || _inventoryAndShop == null)
            {
                return;
            }

            // E: Interactuar con el objeto apuntado (Tolva Central, Cofre Congelado, Veta, Tienda)
            if (kb.eKey.wasPressedThisFrame && _currentFocusedInteractable != null)
            {
                _currentFocusedInteractable.Interact(gameObject);
            }

            // G: Desplegar Guirnalda Luminosa Navideña (Burbuja de calor de 6m)
            if (kb.gKey.wasPressedThisFrame)
            {
                _inventoryAndShop.TryDeployStringLight(transform.position + transform.forward * 1.6f);
            }

            // H: Desplegar Baliza Térmica Portátil (Burbuja de 10m + derrite Cofres Congelados)
            if (kb.hKey.wasPressedThisFrame)
            {
                _inventoryAndShop.TryDeployThermalBeacon(transform.position + transform.forward * 2.0f);
            }

            // Q: Lanzar Petardo Festivo (Explosivo contra roca/hielo duro y Gólems)
            if (kb.qKey.wasPressedThisFrame)
            {
                _inventoryAndShop.TryDetonateFestiveFirecracker(transform.position + transform.forward * 2.5f);
            }

            // R: Instalar Duende Mecánico Extractor sobre una veta cercana
            if (kb.rKey.wasPressedThisFrame)
            {
                _inventoryAndShop.TryDeployMechanicalElfExtractor(transform.position);
            }

            // 1, 2, 3: Consumibles Rápidos (Chocolate Caliente, Galleta de Jengibre, Bastón de Menta)
            if (kb.digit1Key.wasPressedThisFrame)
            {
                _inventoryAndShop.TryUseHotChocolateThermos();
            }

            if (kb.digit2Key.wasPressedThisFrame)
            {
                _inventoryAndShop.TryUseGingerbreadCookie();
            }

            if (kb.digit3Key.wasPressedThisFrame)
            {
                _inventoryAndShop.TryUsePeppermintCane();
            }
        }

        private void PerformPickaxeSwing()
        {
            float effectiveCooldown = IsGingerbreadCookieActive ? _swingCooldownSeconds * 0.65f : _swingCooldownSeconds;
            _swingCooldownTimer = effectiveCooldown;
            _swingAnimationTimer = 0.25f;

            if (_playerCamera == null)
            {
                return;
            }

            float rayDist = _useThirdPersonView ? _miningReachDistance + 3.5f : _miningReachDistance;
            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, rayDist))
            {
                HarvestableNode node = hit.collider.GetComponentInParent<HarvestableNode>();
                if (node != null)
                {
                    node.HitWithPickaxe(_currentPickaxeTier, _pickaxeDamage, _inventoryAndShop, this);
                    return;
                }

                AI.DefectiveToyAI enemy = hit.collider.GetComponentInParent<AI.DefectiveToyAI>();
                if (enemy != null)
                {
                    enemy.TakePickaxeDamage(_pickaxeDamage * 25f, _currentPickaxeTier, this);
                }
            }
        }

        private void UpdatePickaxeSwingVisual(float dt)
        {
            if (_pickaxeVisualRoot == null)
            {
                return;
            }

            if (_swingAnimationTimer > 0f)
            {
                _swingAnimationTimer -= dt;
                float t = Mathf.Sin((1f - (_swingAnimationTimer / 0.25f)) * Mathf.PI);
                _pickaxeVisualRoot.localRotation = _initialPickaxeLocalRot * Quaternion.Euler(t * 58f, 0f, 0f);
            }
            else
            {
                _pickaxeVisualRoot.localRotation = Quaternion.Slerp(
                    _pickaxeVisualRoot.localRotation,
                    _initialPickaxeLocalRot,
                    dt * 12f);
            }
        }

        public void ConfigureRigReferences(
            Transform cameraPivot,
            Camera cam,
            Light headlamp,
            Transform pickaxeRoot,
            Renderer pickaxeCrystalRenderer,
            Transform characterVisual)
        {
            _cameraPivot = cameraPivot;
            _playerCamera = cam;
            _helmetHeadlamp = headlamp;
            _pickaxeVisualRoot = pickaxeRoot;
            _pickaxeCrystalTipRenderer = pickaxeCrystalRenderer;
            _characterVisual = characterVisual;

            if (_pickaxeVisualRoot != null)
            {
                _initialPickaxeLocalRot = _pickaxeVisualRoot.localRotation;
            }

            UpdateCameraPositionImmediate();
        }

        public void UpgradePickaxeTier(int newTier)
        {
            _currentPickaxeTier = Mathf.Clamp(newTier, 1, 3);
            _pickaxeDamage = _currentPickaxeTier == 1 ? 1 : (_currentPickaxeTier == 2 ? 2 : 4);

            if (_pickaxeCrystalTipRenderer != null)
            {
                Color crystalColor = _currentPickaxeTier == 1
                    ? new Color(0.90f, 0.22f, 0.27f)
                    : (_currentPickaxeTier == 2 ? new Color(1.0f, 0.45f, 0.12f) : new Color(1.0f, 0.85f, 0.25f));
                _pickaxeCrystalTipRenderer.material.color = crystalColor;
            }

            OnPickaxeTierChanged?.Invoke(_currentPickaxeTier);
            PublishStatus($"¡Pico Industrial con Cristal Energético mejorado a NIVEL {_currentPickaxeTier}!");
        }

        public void ApplyGingerbreadCookieBuff(float durationSeconds = 45f)
        {
            _gingerbreadCookieBuffTimer = Mathf.Max(_gingerbreadCookieBuffTimer, durationSeconds);
            _gingerbreadSpeedMultiplier = 1.32f;
            PublishStatus("Galleta de Jengibre consumida: +30% velocidad en nieve y +40% velocidad de picado (45s).");
        }

        public void ToggleShopMenu()
        {
            if (!_gameplayEnabled)
            {
                return;
            }

            _isShopMenuOpen = !_isShopMenuOpen;
            SetCursorLock(!_isShopMenuOpen);
        }

        internal void SetGameplayEnabled(bool active)
        {
            _gameplayEnabled = active;
            _isShopMenuOpen = false;
            _currentFocusedInteractable = null;
            SetCursorLock(active);
        }

        internal void SetCharacterPresent(bool present)
        {
            _verticalVelocity = Vector3.zero;
            _characterController.enabled = present;
            if (_characterVisual != null)
            {
                _characterVisual.gameObject.SetActive(present);
            }
        }

        [Serializable]
        internal struct SaveState
        {
            public Vector3 position;
            public Vector3 verticalVelocity;
            public float yaw, pitch, cookieSeconds, swingCooldown;
            public int pickaxeTier, pickaxeDamage;
            public bool thirdPerson, headlamp;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            position = transform.position,
            verticalVelocity = _verticalVelocity,
            yaw = _playerYaw,
            pitch = _cameraPitch,
            cookieSeconds = _gingerbreadCookieBuffTimer,
            swingCooldown = _swingCooldownTimer,
            pickaxeTier = _currentPickaxeTier,
            pickaxeDamage = _pickaxeDamage,
            thirdPerson = _useThirdPersonView,
            headlamp = IsHeadlampEnabled
        };

        internal void RestoreSave(SaveState state)
        {
            _characterController.enabled = false;
            _playerYaw = state.yaw;
            _cameraPitch = state.pitch;
            transform.SetPositionAndRotation(state.position, Quaternion.Euler(0f, _playerYaw, 0f));
            _characterController.enabled = true;
            _verticalVelocity = state.verticalVelocity;
            _useThirdPersonView = state.thirdPerson;
            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
            if (_helmetHeadlamp != null)
            {
                _helmetHeadlamp.enabled = state.headlamp;
            }
            UpdateCameraPositionImmediate();
            UpgradePickaxeTier(state.pickaxeTier);
            _pickaxeDamage = state.pickaxeDamage;
            _gingerbreadCookieBuffTimer = state.cookieSeconds;
            _gingerbreadSpeedMultiplier = state.cookieSeconds > 0f ? 1.32f : 1f;
            _swingCooldownTimer = state.swingCooldown;
            PublishStatus("Partida cargada.");
        }

        public void PublishStatus(string message)
        {
            LastStatusMessage = message;
            OnStatusMessagePublished?.Invoke(message);
        }

        private void SetCursorLock(bool locked)
        {
            _isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void UpdateCameraPositionImmediate()
        {
            if (_playerCamera != null && _cameraPivot != null)
            {
                _playerCamera.transform.SetParent(_cameraPivot, false);
                _playerCamera.transform.localPosition = _useThirdPersonView
                    ? _thirdPersonCameraOffset
                    : _firstPersonCameraOffset;
                _playerCamera.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
