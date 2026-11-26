using System;
using Boreatek.Audio;
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
    public partial class PlayerController : MonoBehaviour
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
        [SerializeField] private Vector3 _firstPersonCameraOffset = new Vector3(0f, 0f, 0.13f);

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
        private Vector3 _knockbackVelocity;
        private float _cameraPitch;
        private float _playerYaw;
        private float _swingCooldownTimer;
        private float _footstepDistance;
        private SnowFootprints _snowFootprints;
        private float _gingerbreadCookieBuffTimer;
        private float _gingerbreadSpeedMultiplier = 1f;
        private bool _isCursorLocked = true;
        private bool _isShopMenuOpen;
        private bool _gameplayEnabled = true;
        private Transform _characterVisual;
        private ElfPose _elfPose;
        private Renderer _thirdPersonHead;
        private Renderer _headlampLens;
        private MaterialPropertyBlock _pickaxeFinish;
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
            if (_playerCamera != null) _standardFieldOfView = _playerCamera.fieldOfView;

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
            bool hudPointer = Keyboard.current?.leftAltKey.isPressed == true ||
                Keyboard.current?.rightAltKey.isPressed == true;
            if (!_isShopMenuOpen && _isCursorLocked == hudPointer) SetCursorLock(!hudPointer);

            if (_mountedSled != null)
            {
                UpdateSled(dt, !_isShopMenuOpen && !hudPointer);
                return;
            }
            if (!_isShopMenuOpen && _isCursorLocked)
            {
                HandleLookRotation();
            }
            HandleMovement(dt);
            UpdateCameraObstruction();
            UpdateInteractionFocus();
            AdvancePickaxeSwing(dt);
            AdvanceHeatPlacement(dt);

            if (!_isShopMenuOpen && !hudPointer)
            {
                HandleEquipmentSelection();
                HandleActionInputs();
            }
            UpdatePlacementGuide(!_isShopMenuOpen && !hudPointer);

            UpdateCharacterAnimation(dt);
            RefreshHeldEquipment(dt);
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

            if (kb.tabKey.wasPressedThisFrame)
            {
                ToggleShopMenu();
            }

            // F: Encender / Apagar Faro Frontal LED del Casco Minero
            if (kb.fKey.wasPressedThisFrame && _helmetHeadlamp != null)
            {
                SetHeadlampEnabled(!_helmetHeadlamp.enabled);
                BoreatekSoundscape.Play(SoundCue.Click, strength: 0.65f);
                PublishStatus(_helmetHeadlamp.enabled
                    ? "Faro Frontal LED del casco ENCENDIDO."
                    : "Faro Frontal LED del casco APAGADO (menor detección enemiga).");
            }

            if (kb.f5Key.wasPressedThisFrame || kb.vKey.wasPressedThisFrame)
            {
                CyclePerspective();
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

            Vector3 startPosition = transform.position;
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
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
                BoreatekSoundscape.Play(SoundCue.Jump, strength: 0.28f);
            }

            _verticalVelocity.y += _gravity * dt;
            _characterController.Move((moveDirection * targetSpeed + _verticalVelocity + _knockbackVelocity) * dt);
            _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero, dt * 21f);
            if (dt > 0f)
            {
                Vector3 travel = Vector3.ProjectOnPlane(transform.position - startPosition, Vector3.up);
                float distance = travel.magnitude;
                float pace = distance / (Mathf.Max(targetSpeed, 0.01f) * dt);
                bool onGround = _characterController.isGrounded;
                _elfPose?.UpdateTravel(transform.InverseTransformDirection(travel.normalized), pace, isSprinting, onGround, dt);
                PlayFootsteps(moveInput.sqrMagnitude > 0.01f ? distance : 0f, isGrounded, onGround, isSprinting);
            }
        }

        private void PlayFootsteps(float distance, bool wasGrounded, bool grounded, bool sprinting)
        {
            if (!grounded)
            {
                _footstepDistance = 0f;
                return;
            }
            if (!wasGrounded && _verticalVelocity.y < -1.7f)
            {
                BoreatekSoundscape.Play(SoundCue.Land, strength: Mathf.Clamp(-_verticalVelocity.y * 0.045f, 0.09f, 0.38f));
                return;
            }
            float stride = sprinting ? 1.55f : 1.20f;
            _footstepDistance += distance;
            if (_footstepDistance < stride) return;
            _footstepDistance = Mathf.Repeat(_footstepDistance, stride);
            bool foundGround = Physics.Raycast(transform.position + Vector3.up * 0.28f, Vector3.down, out RaycastHit ground,
                0.72f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            SoundCue surface = FootstepSurface(foundGround ? ground.collider : null);
            BoreatekSoundscape.Play(surface, strength: sprinting ? 0.22f : 0.15f);
            if (foundGround) _snowFootprints?.Step(ground, surface == SoundCue.StepSnow && IsSnowfield(ground.collider), transform.forward);
        }

        internal void BindFootprints(SnowFootprints footprints) => _snowFootprints = footprints;

        private static bool IsSnowfield(Collider ground) => ground.name is "ArcticSnowfield" or "ArcticSnowfields";

        private SoundCue FootstepSurface(Collider ground)
        {
            if (ground != null)
            {
                switch (ground.name)
                {
                    case "Entrance_Bridge": return SoundCue.StepPlanks;
                    case "ArcticSnowfield":
                    case "ArcticSnowfields": return _temperatureSystem.CurrentBiome == BiomeType.GlacialFlats ? SoundCue.StepIce : SoundCue.StepSnow;
                    case "Cueva_Procedural_Rampa_Y_Camara": return SoundCue.StepIce;
                    case "Tunnel_FloorRamp":
                    case "Village_ThermalPlatform":
                    case "Foundation_Walls_And_Roof":
                    case "Shell_And_Walkable_Floor":
                    case "Walkway_To_Steam_Room":
                    case "Village_Paved_Lanes":
                    case "Mine_Entrance_Arch_And_Approach": return SoundCue.StepStone;
                }
            }
            return _temperatureSystem.CurrentBiome switch
            {
                BiomeType.SantaIndustrialVillage or BiomeType.VentilationTunnels => SoundCue.StepStone,
                var biome when BiomeRules.IsUnderground(biome) || biome == BiomeType.GlacialFlats => SoundCue.StepIce,
                _ => SoundCue.StepSnow
            };
        }

        private void UpdateInteractionFocus()
        {
            _currentFocusedInteractable = null;
            _aimHit = default;
            if (_playerCamera == null)
            {
                return;
            }

            Transform frame = AimFrame;
            Ray ray = new Ray(frame.position, frame.forward);
            if (TryGetAimHit(ray, CameraAimReach(frame), out RaycastHit hit))
            {
                _aimHit = hit;
                _currentFocusedInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }
        }

        private void HandleActionInputs()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb?.eKey.wasPressedThisFrame == true && _currentFocusedInteractable != null &&
                _currentFocusedInteractable.CanInteract(gameObject))
            {
                if (_currentFocusedInteractable is HarvestableNode node)
                {
                    SelectEquipment(EquipmentSlot.Pickaxe);
                    if (CanSwingPickaxe) PerformPickaxeSwing(node);
                }
                else
                {
                    PlayInteraction(ElfPose.Gesture.Reach, AimFrame.forward);
                    _currentFocusedInteractable.Interact(gameObject);
                }
                return;
            }

            bool usePressed = mouse?.rightButton.wasPressedThisFrame == true || kb?.enterKey.wasPressedThisFrame == true ||
                Gamepad.current?.rightTrigger.wasPressedThisFrame == true;
            if (SelectedEquipment == EquipmentSlot.Pickaxe)
            {
                if ((mouse?.leftButton.isPressed == true || usePressed) && CanSwingPickaxe) PerformPickaxeSwing();
            }
            else if (mouse?.leftButton.wasPressedThisFrame == true || usePressed) UseEquipment(SelectedEquipment);

            if (kb?.gKey.wasPressedThisFrame == true) UseEquipment(EquipmentSlot.Garland);
            if (kb?.hKey.wasPressedThisFrame == true) UseEquipment(EquipmentSlot.Beacon);
            if (kb?.qKey.wasPressedThisFrame == true) UseEquipment(EquipmentSlot.Firecracker);
            if (kb?.rKey.wasPressedThisFrame == true) UseEquipment(EquipmentSlot.Extractor);
        }

        public void ConfigureRigReferences(
            Transform cameraPivot,
            Camera cam,
            Light headlamp,
            Transform pickaxeRoot,
            Renderer pickaxeCrystalRenderer,
            Transform characterVisual,
            Renderer thirdPersonHead = null,
            Renderer headlampLens = null)
        {
            _characterController ??= GetComponent<CharacterController>();
            _inventoryAndShop ??= GetComponent<InventoryAndShop>();
            _cameraPivot = cameraPivot;
            _playerCamera = cam;
            if (cam != null) _standardFieldOfView = cam.fieldOfView;
            _helmetHeadlamp = headlamp;
            _pickaxeVisualRoot = pickaxeRoot;
            _pickaxeCrystalTipRenderer = pickaxeCrystalRenderer;
            _characterVisual = characterVisual;
            _bodyRenderers = _characterVisual != null
                ? Array.FindAll(_characterVisual.GetComponentsInChildren<Renderer>(true),
                    renderer => pickaxeRoot == null || !renderer.transform.IsChildOf(pickaxeRoot))
                : Array.Empty<Renderer>();
            _thirdPersonHead = thirdPersonHead;
            _headlampLens = headlampLens;

            if (_pickaxeVisualRoot != null)
            {
                _initialPickaxeLocalRot = _pickaxeVisualRoot.localRotation;
            }

            UpdateCameraPositionImmediate();
        }

        internal void ConfigureProceduralPose(ElfPose pose)
        {
            _elfPose = pose;
            _pickaxeGripParent = _pickaxeVisualRoot.parent;
            _pickaxeGripPosition = _pickaxeVisualRoot.localPosition;
            _pickaxeGripScale = _pickaxeVisualRoot.localScale;
            RefreshPickaxeView();
        }

        private void PlayInteraction(ElfPose.Gesture gesture, Vector3 direction)
        {
            _elfPose?.BeginGesture(gesture, transform.InverseTransformDirection(direction));
            _heldGesture = gesture;
            _heldGestureTime = ElfPose.GestureDuration;
        }

        internal void ReactToCombatDamage(Vector3? attackerPosition, float strength)
        {
            Vector3 away = attackerPosition.HasValue
                ? Vector3.ProjectOnPlane(transform.position - attackerPosition.Value, Vector3.up).normalized
                : -transform.forward;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            _knockbackVelocity = away * Mathf.Lerp(2.3f, 4.8f, strength);
            _elfPose?.ReactToHit(transform.InverseTransformDirection(away));
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
                _pickaxeFinish ??= new MaterialPropertyBlock();
                _pickaxeCrystalTipRenderer.GetPropertyBlock(_pickaxeFinish);
                _pickaxeFinish.SetColor("_BaseColor", crystalColor);
                _pickaxeFinish.SetColor("_Color", crystalColor);
                _pickaxeFinish.SetColor("_EmissionColor", crystalColor * 2.1f);
                _pickaxeCrystalTipRenderer.SetPropertyBlock(_pickaxeFinish);
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
            BoreatekSoundscape.Play(SoundCue.Click, strength: 0.65f);
            if (_isShopMenuOpen)
            {
                CancelPickaxeSwing();
                CancelHeatPlacement();
            }
            SetCursorLock(!_isShopMenuOpen);
        }

        internal void SetGameplayEnabled(bool active)
        {
            _gameplayEnabled = active;
            _isShopMenuOpen = false;
            _currentFocusedInteractable = null;
            if (!active) CancelHeatPlacement();
            SetCursorLock(active);
        }

        internal void SetCharacterPresent(bool present)
        {
            if (!present && _mountedSled != null) LeaveSled(force: true);
            _verticalVelocity = Vector3.zero;
            _knockbackVelocity = Vector3.zero;
            _footstepDistance = 0f;
            _snowFootprints?.ResetBoots();
            CancelPickaxeSwing();
            _elfPose?.Reset();
            _characterController.enabled = present;
            _heldGestureTime = 0f;
            if (_characterVisual != null)
            {
                _characterVisual.gameObject.SetActive(present);
            }
            RefreshHeldEquipment();
        }

        [Serializable]
        internal struct SaveState
        {
            public Vector3 position;
            public Vector3 verticalVelocity;
            public float yaw, pitch, cookieSeconds, swingCooldown;
            public int pickaxeTier, pickaxeDamage;
            public int cameraPerspective;
            public int equipmentSlot;
            public bool thirdPerson, headlamp;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            position = SavePosition(),
            verticalVelocity = _verticalVelocity,
            yaw = _playerYaw,
            pitch = _cameraPitch,
            cookieSeconds = _gingerbreadCookieBuffTimer,
            swingCooldown = _swingCooldownTimer,
            pickaxeTier = _currentPickaxeTier,
            pickaxeDamage = _pickaxeDamage,
            thirdPerson = !InFirstPerson,
            cameraPerspective = (int)CurrentPerspective,
            equipmentSlot = (int)SelectedEquipment,
            headlamp = IsHeadlampEnabled
        };

        internal void RestoreSave(SaveState state)
        {
            _characterController.enabled = false;
            CancelPickaxeSwing();
            _elfPose?.Reset();
            _playerYaw = state.yaw;
            _cameraPitch = state.pitch;
            transform.SetPositionAndRotation(state.position, Quaternion.Euler(0f, _playerYaw, 0f));
            _characterController.enabled = true;
            _verticalVelocity = state.verticalVelocity;
            _knockbackVelocity = Vector3.zero;
            _footstepDistance = 0f;
            _snowFootprints?.ResetBoots();
            RestorePerspective(state);
            SelectEquipment((EquipmentSlot)Mathf.Clamp(state.equipmentSlot, 0, (int)EquipmentSlot.Sword));
            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
            SetHeadlampEnabled(state.headlamp);
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

        private void SetHeadlampEnabled(bool enabled)
        {
            if (_helmetHeadlamp != null) _helmetHeadlamp.enabled = enabled;
            RefreshHeadlampLens();
        }

        private void RefreshHeadlampLens()
        {
            if (_headlampLens != null) _headlampLens.enabled = !InFirstPerson && IsHeadlampEnabled &&
                (_thirdPersonHead == null || _thirdPersonHead.enabled);
        }
    }
}
