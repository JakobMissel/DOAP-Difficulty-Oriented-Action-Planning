using System;
using Unity.Cinemachine;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    // Tutorial
    public static Action<bool> canInteract;
    public static void OnCanInteract(bool state) => canInteract?.Invoke(state);

    public static Action<bool> canThrow;
    public static void OnCanThrow(bool state) => canThrow?.Invoke(state);

    public static Action removeAllThrowables;
    public static void OnLoseThrowables() => removeAllThrowables?.Invoke();

    public static Action tutorialCompletion;
    public static void OnTutorialCompletion() => tutorialCompletion?.Invoke();

    // Movement actions
    public static Action<bool> moveStatus;
    public static void OnMoveStatus(bool isMoving) => moveStatus?.Invoke(isMoving);

    // Climb actions
    public static Action<bool> climbStatus;
    public static void OnClimbStatus(bool isClimbing) => climbStatus?.Invoke(isClimbing);

    // Sneak actions
    public static Action<bool> sneakStatus;
    public static void OnSneakStatus(bool isSneaking) => sneakStatus?.Invoke(isSneaking);

    public static Action<bool> isSneaking;
    public static void OnIsSneaking(bool sneak) => isSneaking?.Invoke(sneak);

    // Interaction actions
    public static Action<InputAction.CallbackContext> playerInteract;
    public static void OnPlayerInteract(InputAction.CallbackContext cxt) => playerInteract?.Invoke(cxt);

    public static Action<Pickup> addPickup;
    public static void OnAddPickupToInteractableList(Pickup pickup) => addPickup?.Invoke(pickup);

    public static Action<Pickup> removePickup;
    public static void OnRemovePickupFromInteractableList(Pickup pickup) => removePickup?.Invoke(pickup);

    // Pickup actions
    public static Action<Pickup> pickedUpItem;
    public static void OnPickedUpItem(Pickup item) => pickedUpItem?.Invoke(item);

    public static Action<StealablePickup, bool> stealItem;
    public static void OnStealItem(StealablePickup item, bool isNew) => stealItem?.Invoke(item, isNew);

    public static Action paintingDelivered;
    public static void OnPaintingDelivered() => paintingDelivered?.Invoke();

    // Aim & Throw actions
    public static Action<bool> aimStatus;
    public static void OnAimStatus(bool isAiming) => aimStatus?.Invoke(isAiming);

    public static Action<bool> isAiming;
    public static void OnIsAiming(bool aim) => isAiming?.Invoke(aim);

    public static Action<CinemachineCamera> changedCamera;
    public static void OnChangedCamera(CinemachineCamera newCamera) => changedCamera?.Invoke(newCamera);

    public static Action<GameObject> sethitArea;
    public static void OnSetHitArea(GameObject hitArea) => sethitArea?.Invoke(hitArea);

    public static Action playerThrow;
    public static void OnPlayerThrow() => playerThrow?.Invoke();

    public static Action<int> ammoUpdate;
    public static void OnAmmoUpdate(int ammo) => ammoUpdate?.Invoke(ammo);

    public static Action<object> spriteUpdate;
    public static void OnImageUpdate(object image) => spriteUpdate?.Invoke(image);

    // Caught by guard action
    public static Action playerCaught;
    public static void OnPlayerCaught() => playerCaught?.Invoke();

    public static Action<bool> gameOverState;
    public static void OnGameOverState(bool isBlocked) => gameOverState?.Invoke(isBlocked);

    // Win game action
    public static Action playerEscaped;
    public static void OnPlayerEscaped() => playerEscaped?.Invoke();

    //========================================================================//

    public static PlayerActions Instance;
    [SerializeField] bool debugEvents;
    bool previousMoveStatus;
    bool previousClimbStatus;
    bool previousSneakStatus;
    public bool carriesPainting;
    public bool canEscape;
    public bool isOnWall;



    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        canEscape = false;
        carriesPainting = false;
        isOnWall = false;
    }

    void OnEnable()
    {
        if (debugEvents)
        {
            tutorialCompletion += DebugTutorialCompleted;
            moveStatus += DebugMove;
            climbStatus += DebugClimb;
            sneakStatus += DebugSneak;
            pickedUpItem += DebugPickup;
            stealItem += DebugStole;
            paintingDelivered += DebugPaintingDelivered;
            aimStatus += DebugAim;
            ammoUpdate += DebugAmmoUpdate;
        }
        CheckpointManager.loadCheckpoint += CheckpointLoaded;
    }

    void OnDisable()
    {
        if (debugEvents)
        {
            tutorialCompletion -= DebugTutorialCompleted;
            moveStatus -= DebugMove;
            climbStatus -= DebugClimb;
            sneakStatus -= DebugSneak;
            pickedUpItem -= DebugPickup;
            stealItem -= DebugStole;
            paintingDelivered -= DebugPaintingDelivered;
            aimStatus -= DebugAim;
            ammoUpdate -= DebugAmmoUpdate;
        }
        CheckpointManager.loadCheckpoint -= CheckpointLoaded;
    }

    void CheckpointLoaded()
    {
        canEscape = false;
        carriesPainting = false;
        isOnWall = false;
    }

    void DebugTutorialCompleted()
    {
        print($"[{Time.time}] Tutorial completed");
    }

    void DebugMove(bool obj)
    {
        if(obj != previousMoveStatus)
        {
            print($"[{Time.time}] Moving: {obj}");
            previousMoveStatus = obj;
        }
    }

    void DebugClimb(bool obj)
    {
        if(obj != previousClimbStatus)
        {
            print($"[{Time.time}] Climbing: {obj}");
            previousClimbStatus = obj;
        }
    }

    void DebugSneak(bool obj)
    {
        if(obj != previousSneakStatus)
        {
            print($"[{Time.time}] Sneaking: {obj}");
            previousSneakStatus = obj;
        }
    }

    void DebugPickup(Pickup pickup)
    {
        print($"[{Time.time}] Picked up item: {pickup.name}");
    }

    void DebugStole(Pickup pickup, bool isNew)
    {
        print($"[{Time.time}] Stole item: {pickup.name}");
    }

    void DebugPaintingDelivered()
    {
        print($"[{Time.time}] Stolen item delivered"); // might want a pickup reference here too
    }

    void DebugAim(bool status)
    {
        print($"[{Time.time}] Aiming: {status}");
    }

    void DebugAmmoUpdate(int ammo)
    {
        print($"[{Time.time}] Ammo updated: {ammo}");
    }
}
