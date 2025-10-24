using System;
using Unity.Cinemachine;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
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

    public static Action<Pickup> stealItem;
    public static void OnStealItem(Pickup item) => stealItem?.Invoke(item);

    // Aim & Throw actions
    public static Action<bool> aimStatus;
    public static void OnAimStatus(bool isAiming) => aimStatus?.Invoke(isAiming);

    public static Action<bool> isAiming;
    public static void OnIsAiming(bool aim) => isAiming?.Invoke(aim);

    public static Action<CinemachineCamera> changedCamera;
    public static void OnChangedCamera(CinemachineCamera newCamera) => changedCamera?.Invoke(newCamera);

    public static Action<GameObject> sethitArea;
    public static void OnSetHitArea(GameObject hitArea) => sethitArea?.Invoke(hitArea);

    public static Action<int> ammoUpdate;
    public static void OnAmmoUpdate(int ammo) => ammoUpdate?.Invoke(ammo);

    public static Action<Sprite> spriteUpdate;
    public static void OnSpriteUpdate(Sprite sprite) => spriteUpdate?.Invoke(sprite);

    //========================================================================//

    [SerializeField] bool debugEvents;
    bool previousMoveStatus;
    bool previousClimbStatus;
    bool previousSneakStatus;

    void OnEnable()
    {
        if (debugEvents)
        {
            moveStatus += DebugMove;
            climbStatus += DebugClimb;
            sneakStatus += DebugSneak;
            pickedUpItem += DebugPickup;
            stealItem += DebugStole;
            aimStatus += DebugAim;
            ammoUpdate += DebugAmmoUpdate;
        }
    }

    void OnDisable()
    {
        if (debugEvents)
        {
            moveStatus -= DebugMove;
            climbStatus -= DebugClimb;
            sneakStatus -= DebugSneak;
            pickedUpItem -= DebugPickup;
            stealItem -= DebugStole;
            aimStatus -= DebugAim;
            ammoUpdate -= DebugAmmoUpdate;
        }
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

    void DebugStole(Pickup pickup)
    {
        print($"[{Time.time}] Stole item: {pickup.name}");
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
