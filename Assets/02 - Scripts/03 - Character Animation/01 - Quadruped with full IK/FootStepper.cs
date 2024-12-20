using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepper : MonoBehaviour
{
    [Header("Stepper Settings")]
    public Transform homeTransform; // The position and rotation from which we want to stay in range (represented as the blue chip).

    [Header("Raycast Settings")]
    public LayerMask groundRaycastMask = ~0; // Ground layer that you need to detect by raycasting.

    // Flag to define when a leg is moving.
    public bool Moving;
    protected Terrain terrain;
    public CustomTerrain cterrain;
    private ParameterManager parameters;
    public Transform targetRoot;

    // void Start() {
    // }

    // Awake is called when the script instance is being loaded.
    public void Setup(int toLog)
    {
        if (targetRoot == null) {
            Debug.LogWarning($"TargetRoot is null for animal {toLog}.");
            return;
        }
        parameters = ParameterManager.Instance;
        terrain = Terrain.activeTerrain;
        cterrain = terrain.GetComponent<CustomTerrain>();
        // We put the steppers at the top of the hierarchy, to avoid other influences from the parent transforms and to see them better.
        transform.SetParent(targetRoot);
        // Adapt the legs just after starting the script.
        MoveLeg();
    }

    /// <summary>
    /// Method that set the preconditions for moving a leg and exectutes the coroutine process.
    /// </summary>
    public void MoveLeg()
    {
        // If we are already moving, don't start another move.
        if (Moving)
        {
            return;
        }

        if (parameters == null) {
            Debug.Log("Can't move leg");
            return;
            // Setup();
        }

        /*
         * First, we want to calculate the distance from the GameObject where this script is attached (target, red sphere) to the home position of the respective leg (blue chip).
         * We also calculate the quaternion between the current rotation and the home rotation.
         * If such distance is larger than the threshold step, OR the angle difference is larger than the angle threshold, we call the coroutine to move the leg.
         */

        // START TODO ###################

        float distFromHome = Vector3.Distance(transform.position, homeTransform.position);
        float angleFromHome = Quaternion.Angle(transform.rotation, homeTransform.rotation);

        // Change condition!
        if (distFromHome > parameters.distanceThreshold || angleFromHome > parameters.angleThreshold)
        {
            // END TODO ###################

            // Get the grounded location for the feet. It can return false - in that case, it won't move.
            // This method modifies the values by reference, which are used later.
            if (GetGroundedEndPosition(out Vector3 endPos, out Vector3 endNormal))
            {
                // The foot rotation will be defined by:
                // Forward direction: Forward normalized vector of home, but projected on the same plane where the grounded position was detected.
                // Upward direction: Normal of the plane where the grounded position was detected.
                Quaternion endRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(homeTransform.forward, endNormal), endNormal);

                // Start MoveFoot coroutine with the target position, rotation and duration of the movement.
                StartCoroutine(MoveFoot(endPos, endRot, parameters.moveDuration));
            }
        }
    }

    /// <summary>
    /// Method that saves the ground location information where the foot should be located.
    /// Returns false if no grounded position was found.
    /// </summary>
    /// <param name="endPos"></param>
    /// <param name="endNormal"></param>
    /// <returns></returns>
    bool GetGroundedEndPosition(out Vector3 endPos, out Vector3 endNormal)
    {
        /*
         * First, we calculate the normalized vector that goes from the current position to the home position. It will describe the direction of our overshoot (offset) vector.
         * Then, you can create a factor by taking the distance threshold and modifying it by a fraction [0, 1].
         * Finally, you can multiply such new factor by the normalized distance previously calculated.
         * The result is a vector in the direction of the movement when the foot is coming back to the home position, with a small magnitude.
         * Summing such vector to the real home position, you will get a new home position slighly moved.
         */

        Vector3 towardsHome = (homeTransform.position - transform.position).normalized;
        float overshootDistance = parameters.distanceThreshold * parameters.stepOvershootFraction;
        Vector3 overshootVector = towardsHome * overshootDistance;

        /*
         * Now, we build a raycast system. Check: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
         * The ray will detect a collision with the terrain and use such information to place the foot accordingly on the ground.
         * First, you create the origin (Vector3) of your ray, which will be in the home position (remember to add the overshoot vector calculated before).
         * You can also add some vertical y displacement to let the ray having more space when going down and avoiding undesired collisions.
         * Then, throw the ray downwards, and save the position and normal vector of the hit in "endPos" and "endNormal" respectively.
         * If there is a collision, return true. Otherwise, you can return false.
         */

        // START TODO ###################
        
        float verticalDisplacement = 0.01f;
        Vector3 raycastOrigin = homeTransform.position + overshootVector + new Vector3(0, verticalDisplacement, 0);

        Vector3 scale = terrain.terrainData.heightmapScale;
        endPos = homeTransform.position + overshootVector + new Vector3(0, verticalDisplacement, 0);
        endPos.y = cterrain.getInterp(endPos.x/scale.x, endPos.z/scale.z);
        endNormal = cterrain.getNormal(endPos.x/scale.x, endPos.z/scale.z);
        return true;
        // END TODO ###################
    }

    /// <summary>
    /// Coroutine that performs the motion part.
    /// </summary>
    /// <param name="endPos"></param>
    /// <param name="endRot"></param>
    /// <param name="moveTime"></param>
    /// <returns></returns>
    IEnumerator MoveFoot(Vector3 endPos, Quaternion endRot, float moveTime)
    {
        // We are in the coroutine, meaning that a moving action is taking place.
        Moving = true;

        // Store the initial, current position and rotation for the interpolation.
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // Apply the height offset to the home position, where we will move towards.
        // You can change the heightOffset to see the effect. It just changes the vertical component to modify where the IK target is.
        endPos += homeTransform.up * parameters.heightOffset;

        // Initialize the time.
        float timeElapsed = 0;

        do
        {
            /*
             * First, you need to keep the record of the total elapsed time.
             * Then, you can normalized by using the moveTime variable.
             */

            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveTime;

            // We could also apply some animation curve(e.g.Easing.EaseInOutCubic) to make the foot go smoother.
            normalizedTime = Easing.EaseInOutCubic(normalizedTime);

            /*
             * We know startPos and endPos. We could interpolate directly from the starting point to the end point, but we have a problem: The movement would be straight and flat on the terrain. Try it out!
             * transform.position = Vector3.Lerp(startPoint, endPoint, normalizedTime);
             * We need to find a way to guide the foot from the ground to a lifted position, and then put it back on the ground. 
             * Any idea? Just a tip: https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Constructing_B.C3.A9zier_curves
             */

            // START TODO ###################

            float stepHeight = 0.5f;
            Vector3 middlePos = (startPos + endPos) / 2 + Vector3.up * stepHeight;
            transform.position = Mathf.Pow(1 - normalizedTime, 2) * startPos +
                         2 * (1 - normalizedTime) * normalizedTime * middlePos +
                         Mathf.Pow(normalizedTime, 2) * endPos;

            // END TODO ###################

            /*
             * You just need now to also move from the starting rotation and the ending rotation.
             */

            // START TODO ###################
            
            transform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);

            // END TODO ###################

            // Wait for one frame
            yield return null;
        }
        while (timeElapsed < parameters.moveDuration);

        // Motion has finished.
        Moving = false;
    }

    /// <summary>
    /// Function for better visualization.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (Moving)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, homeTransform.position);
        Gizmos.DrawWireCube(homeTransform.position, Vector3.one * 0.1f);
    }
}
