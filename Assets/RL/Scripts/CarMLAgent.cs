using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// Self-contained ML-Agent for the Racing Starter Kit car.
/// - Auto-finds all WheelColliders on startup (no manual Inspector wiring needed)
/// - Directly drives WheelColliders (bypasses CarController / CarFreeze entirely)
/// - Works with WASD/Arrows in HeuristicOnly mode, and with Python trainer in Default mode
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarMLAgent : Agent
{
    [Header("Car Tuning")]
    [SerializeField] private float motorForce    = 1500f;
    [SerializeField] private float brakeForce    = 3000f;
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float maxSpeed      = 7f;    // m/s cap

    [Header("Physics Fix")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("RL Settings")]
    [SerializeField] private float maxEpisodeTime = 90f;
    
    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 6f; // Matches RayPerceptionSensor
    [SerializeField] private float rayHeightOffset = 1.0f; // Increased to 1.0 so rays clear the car hood

    // ---- Runtime references ----
    private Rigidbody      rb;
    private WheelCollider  wheelFL, wheelFR, wheelRL, wheelRR;
    private Transform      meshFL,  meshFR,  meshRL,  meshRR;
    private Vector3        startPos;
    private Quaternion     startRot;
    private float          episodeTimer;

    // ---- UI Tracking ----
    private float lastStepReward = 0f;
    private float[] rayDistances = new float[7]; // L60, L40, L20, F, R20, R40, R60
    private LineRenderer[] rayLasers;

    // =========================================================
    // INITIALIZE  — called once on first episode
    // =========================================================
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        // --- Rigidbody safety ---
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.centerOfMass += centerOfMassOffset;

        // --- Disable conflicting legacy scripts ---
        DisableLegacyScript("CarFreeze");
        DisableLegacyScript("CarController");
        DisableLegacyScript("CarUserControl");
        DisableLegacyScript("CarAudio");
        DisableLegacyScript("AICarAvoidanceBehaviour");
        DisableLegacyScript("AICarAvoidanceSmartSteering");
        DisableLegacyScript("BrakeLight");

        // --- Auto-find WheelColliders ---
        FindWheels();

        // --- Fix ColliderBottom so it doesn't drag on ground ---
        FixBodyColliders();

        // --- Store spawn point ---
        startPos = transform.position;
        startRot = transform.rotation;

        // --- Setup LineRenderers for Visual Lasers ---
        SetupLasers();

        Debug.Log("[CarMLAgent] Initialized on: " + gameObject.name);
    }

    // =========================================================
    // EPISODE BEGIN  — called each reset
    // =========================================================
    public override void OnEpisodeBegin()
    {
        // Safe check in case Initialize failed during LazyInitialize
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        rb.isKinematic  = false;
        rb.constraints  = RigidbodyConstraints.None;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = startPos;
        transform.rotation = startRot;

        // Zero out wheels
        foreach (var w in new[] { wheelFL, wheelFR, wheelRL, wheelRR })
        {
            if (w == null) continue;
            w.motorTorque = 0f;
            w.brakeTorque = 0f;
            w.steerAngle  = 0f;
        }

        episodeTimer = 0f;
        lastStepReward = 0f;

        // Reset UI Checkpoints so the race layout tracker doesn't get confused
        var cc = GetComponentInChildren<SpinMotion.CheckpointTracker>();
        if (cc != null) cc.ResetCheckpoints();
    }
    
    // =========================================================
    // UPDATE - Raycasts for Visualization & Sensors
    // =========================================================
    private void Update()
    {
        PerformRaycasts();
    }

    private void SetupLasers()
    {
        rayLasers = new LineRenderer[7];
        Material laserMat = new Material(Shader.Find("Sprites/Default")); // Safe unlit material

        for (int i = 0; i < 7; i++)
        {
            GameObject go = new GameObject("RayLaser_" + i);
            go.transform.SetParent(transform);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = laserMat;
            lr.startWidth = 0.1f; // Made thicker so they are easily visible
            lr.endWidth = 0.1f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            rayLasers[i] = lr;
        }
    }

    private void PerformRaycasts()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeightOffset;
        
        // Ray Angles: -60, -40, -20, 0, 20, 40, 60 (7 rays)
        float[] angles = { -60f, -40f, -20f, 0f, 20f, 40f, 60f };
        
        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 dir = Quaternion.Euler(0, angles[i], 0) * transform.forward;
            rayLasers[i].SetPosition(0, origin);

            RaycastHit[] hits = Physics.RaycastAll(origin, dir, rayDistance);
            bool hitSomething = false;
            float closestDistance = rayDistance;
            Vector3 hitPoint = origin + dir * rayDistance;

            foreach (var hit in hits)
            {
                // Ignore our own car colliders entirely
                if (hit.collider.transform.IsChildOf(transform)) continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitPoint = hit.point;
                    hitSomething = true;
                }
            }

            rayDistances[i] = closestDistance;
            rayLasers[i].SetPosition(1, hitPoint);

            if (hitSomething)
            {
                rayLasers[i].startColor = Color.red;
                rayLasers[i].endColor = Color.red;
            }
            else
            {
                rayLasers[i].startColor = Color.white;
                rayLasers[i].endColor = Color.white;
            }
        }
    }

    // =========================================================
    // OBSERVATIONS  — Values sent to the neural network
    // =========================================================
    public override void CollectObservations(VectorSensor sensor)
    {
        if (rb == null) return;
        
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / maxSpeed);          // forward speed (normalised)
        sensor.AddObservation(localVel.x / maxSpeed);          // lateral speed (normalised)
        sensor.AddObservation(rb.angularVelocity.y / 5f);      // yaw rate
        sensor.AddObservation(rb.linearVelocity.magnitude / maxSpeed); // total speed
        
        // Feed our manual raycasts to the Agent (7 parameters)
        for (int i = 0; i < rayDistances.Length; i++)
        {
            sensor.AddObservation(rayDistances[i] / rayDistance);
        }
    }

    // =========================================================
    // ACTIONS  — called every FixedUpdate step
    // =========================================================
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (rb == null) return;
        
        float steer  = actionBuffers.ContinuousActions[0]; // -1..1
        float accel  = actionBuffers.ContinuousActions[1]; // -1..1

        // Keep rigidbody unfrozen (safety guard vs legacy scripts)
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        // --- Steering (front wheels only) ---
        float steerAngle = steer * maxSteerAngle;
        if (wheelFL != null) wheelFL.steerAngle = steerAngle;
        if (wheelFR != null) wheelFR.steerAngle = steerAngle;

        // --- Motor / Brakes ---
        float motor = 0f, brake = 0f;
        if (accel > 0f)
        {
            // Speed cap
            motor = (rb.linearVelocity.magnitude < maxSpeed) ? accel * motorForce : 0f;
        }
        else if (accel < 0f)
        {
            brake = -accel * brakeForce;
        }

        // Rear-wheel drive
        if (wheelRL != null) { wheelRL.motorTorque = motor; wheelRL.brakeTorque = brake; }
        if (wheelRR != null) { wheelRR.motorTorque = motor; wheelRR.brakeTorque = brake; }

        // Front brakes only
        if (wheelFL != null) { wheelFL.motorTorque = 0f; wheelFL.brakeTorque = brake; }
        if (wheelFR != null) { wheelFR.motorTorque = 0f; wheelFR.brakeTorque = brake; }

        // --- Sync wheel meshes ---
        SyncMesh(wheelFL, meshFL);
        SyncMesh(wheelFR, meshFR);
        SyncMesh(wheelRL, meshRL);
        SyncMesh(wheelRR, meshRR);

        // --- Rewards ---
        lastStepReward = 0f; // Reset tracking variable for UI
        float fwdSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        
        // 1. Reward forward speed more heavily to learn faster
        float r1 = fwdSpeed * 0.005f;
        AddReward(r1); lastStepReward += r1;
        
        // 2. Penalise reversing and idling to force movement
        if (fwdSpeed < -1f)  { AddReward(-0.01f);  lastStepReward += -0.01f; }
        if (fwdSpeed < 0.5f) { AddReward(-0.005f); lastStepReward += -0.005f; }
        
        // 3. Penalise excessive steering to encourage smooth driving
        float rSteer = -Mathf.Abs(steer) * 0.001f;
        AddReward(rSteer); lastStepReward += rSteer;

        // --- Timeout ---
        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer >= maxEpisodeTime) EndEpisode();
    }

    // =========================================================
    // HEURISTIC  — WASD / Arrow Keys control
    // =========================================================
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxis("Horizontal"); // A/D or Left/Right → steer
        ca[1] = Input.GetAxis("Vertical");   // W/S or Up/Down   → accel/brake
    }

    // =========================================================
    // COLLISION  — wall penalty
    // =========================================================
    private void OnCollisionEnter(Collision col)
    {
        foreach (ContactPoint cp in col.contacts)
        {
            if (Mathf.Abs(cp.normal.y) < 0.5f) // horizontal = wall
            {
                SetReward(-2f); // larger penalty for crashing to stay on track
                lastStepReward = -2f; // Update UI tracking
                EndEpisode();   // trigger respawn
                break;
            }
        }
    }

    // =========================================================
    // ONGUI - Render Parameters, Rewards & Penalties
    // =========================================================
    private void OnGUI()
    {
        // Setup styling for dark semi-transparent box
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = Texture2D.whiteTexture; // We will tint it
        
        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 20;
        textStyle.normal.textColor = Color.white;
        textStyle.fontStyle = FontStyle.Bold;

        // Draw Box
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.Box(new Rect(10, 10, 360, 260), "", boxStyle);
        GUI.color = Color.white;

        // Build string with values
        string info = "--- Agent Status ---\n\n";
        
        if (rb != null)
        {
            info += $"Speed: {rb.linearVelocity.magnitude:F1} m/s\n";
        }
        
        info += $"Cumul. Reward: {GetCumulativeReward():F2}\n";
        
        // Color code the last step reward (Green for positive, Red for negative)
        string rColor = lastStepReward >= 0 ? "green" : "red";
        info += $"Step Reward/Penalty: <color={rColor}>{lastStepReward:F4}</color>\n";
        
        info += $"Episode Time: {episodeTimer:F1}s / {maxEpisodeTime}s\n\n";
        
        info += "📡 Ray Distances:\n";
        info += $"L60: {rayDistances[0]:F1}m | L40: {rayDistances[1]:F1}m\n";
        info += $"L20: {rayDistances[2]:F1}m | Fwd: {rayDistances[3]:F1}m\n";
        info += $"R20: {rayDistances[4]:F1}m | R40: {rayDistances[5]:F1}m\n";
        info += $"R60: {rayDistances[6]:F1}m";

        // Draw text (using rich text for colors)
        textStyle.richText = true;
        GUI.Label(new Rect(20, 15, 340, 240), info, textStyle);
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================
    private void FindWheels()
    {
        var allWheels = GetComponentsInChildren<WheelCollider>();
        Debug.Log("[CarMLAgent] Found " + allWheels.Length + " WheelColliders.");

        foreach (var w in allWheels)
        {
            string n = w.gameObject.name.ToLower();
            if      (n.Contains("front") && (n.Contains("left")  || n.Contains("fl"))) { wheelFL = w; }
            else if (n.Contains("front") && (n.Contains("right") || n.Contains("fr"))) { wheelFR = w; }
            else if (n.Contains("rear")  && (n.Contains("left")  || n.Contains("rl"))) { wheelRL = w; }
            else if (n.Contains("rear")  && (n.Contains("right") || n.Contains("rr"))) { wheelRR = w; }
        }

        // Fallback: assign by index if name matching fails
        if (wheelFL == null && allWheels.Length >= 1) wheelFL = allWheels[0];
        if (wheelFR == null && allWheels.Length >= 2) wheelFR = allWheels[1];
        if (wheelRL == null && allWheels.Length >= 3) wheelRL = allWheels[2];
        if (wheelRR == null && allWheels.Length >= 4) wheelRR = allWheels[3];

        // Try to find wheel mesh visuals by matching sibling transforms in CarParts
        var carParts = transform.Find("CarParts");
        if (carParts != null)
        {
            foreach (Transform child in carParts)
            {
                string n = child.name.ToLower();
                if      (n.Contains("front") && (n.Contains("left")  || n.Contains("fl"))) meshFL = child;
                else if (n.Contains("front") && (n.Contains("right") || n.Contains("fr"))) meshFR = child;
                else if (n.Contains("rear")  && (n.Contains("left")  || n.Contains("rl"))) meshRL = child;
                else if (n.Contains("rear")  && (n.Contains("right") || n.Contains("rr"))) meshRR = child;
            }
        }

        Debug.Log("[CarMLAgent] Wheels -> FL:" + (wheelFL != null) + " FR:" + (wheelFR != null)
                + " RL:" + (wheelRL != null) + " RR:" + (wheelRR != null));
    }

    private void FixBodyColliders()
    {
        var boxes = GetComponentsInChildren<BoxCollider>();
        foreach (var b in boxes)
        {
            if (b.isTrigger) continue; // leave triggers alone
            string n = b.gameObject.name.ToLower();
            if (n.Contains("bottom"))
            {
                // Raise the bottom collider so it doesn't scrape the road
                b.center = new Vector3(b.center.x, 0.40f, b.center.z);
                b.size   = new Vector3(b.size.x, 0.20f, b.size.z);
                Debug.Log("[CarMLAgent] Fixed ColliderBottom: " + b.gameObject.name);
            }
        }
    }

    private void DisableLegacyScript(string typeName)
    {
        var comps = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in comps)
        {
            if (c != null && c.GetType().Name == typeName)
            {
                c.enabled = false;
                Debug.Log("[CarMLAgent] Disabled legacy: " + typeName);
            }
        }
    }

    private void SyncMesh(WheelCollider wc, Transform mesh)
    {
        if (wc == null || mesh == null) return;
        Vector3 pos; Quaternion rot;
        wc.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}