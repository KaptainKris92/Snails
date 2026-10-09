using UnityEngine;

/// Tuning values for the snail's body: the grapple rope and the push-off. One asset, shared by the
/// Snail prefab, so the numbers can be iterated in the inspector without touching code.
[CreateAssetMenu(fileName = "RopeSettings", menuName = "Snails/Rope Settings")]
public class RopeSettings : ScriptableObject
{
    [Header("Rope")]
    [Tooltip("Maximum total rope length in units (shell to first anchor, including wrapped segments).")]
    public float ropeMaxLength = 8f;
    [Tooltip("Shortest the joint can be reeled in to, in units.")]
    public float ropeMinLength = 0.6f;
    [Tooltip("Speed of the body tip while extending (grapple or push-off), units per second.")]
    public float ropeExtendSpeed = 60f;
    [Tooltip("Speed of the body tip while retracting after a miss (visual only), units per second.")]
    public float ropeRetractSpeed = 80f;
    [Tooltip("W/S reel in/out speed, units per second.")]
    public float reelSpeed = 4f;
    [Tooltip("While the shell is touching terrain, the rope yields to the real shell-to-anchor distance once the " +
             "joint error exceeds this (units). Stops the joint dragging the shell through the floor.")]
    public float ropeSlackTolerance = 0.05f;

    [Header("Push-off (Space)")]
    [Tooltip("How far the body can reach to push off something, in units.")]
    public float pushOffRange = 3f;
    [Tooltip("Velocity added away from the surface on a successful push-off, units per second.")]
    public float pushOffSpeed = 8f;

    [Header("Movement")]
    [Tooltip("A/D horizontal acceleration while attached, as a multiple of gravity.")]
    public float ropedHorizontalAccelG = 0.23f;
    [Tooltip("A/D horizontal acceleration while not attached (air or ground), as a multiple of gravity.")]
    public float airHorizontalAccelG = 0.10f;
    [Tooltip("Hard speed cap while attached, units per second.")]
    public float maxRopedSpeed = 22f;

    [Header("Input")]
    [Tooltip("How long a fire or push press stays buffered before it is consumed in FixedUpdate, seconds.")]
    public float inputBuffer = 0.08f;
    [Tooltip("True: hold LMB to stay attached, release to let go. False: press to fire, press again to release.")]
    public bool holdToStayAttached = true;

    [Header("Wrapping")]
    [Tooltip("Maximum number of pivots (including the anchor) the rope can bend around.")]
    public int maxPivots = 8;
    [Tooltip("How far a wrap pivot is pushed off the surface along its normal, in units.")]
    public float pivotSurfaceOffset = 0.02f;
    [Tooltip("Ignore wrap hits closer than this to the shell centre, so terrain the shell is touching does not become a pivot.")]
    public float minWrapDistance = 0.3f;
}
