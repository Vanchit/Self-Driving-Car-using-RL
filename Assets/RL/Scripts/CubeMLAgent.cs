using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// A simpler ML-Agent that drives a standard Cube using Rigidbody physics.
/// </summary>
public class CubeMLAgent : Agent
{
    [Header("Cube Tuning")]
    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float turnSpeed = 150f;

    [Header("RL Settings")]
    [SerializeField] private float maxEpisodeTime = 60f;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float episodeTimer;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = startRotation;

        episodeTimer = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVelocity.z / moveSpeed); 
        sensor.AddObservation(localVelocity.x / moveSpeed); 
        sensor.AddObservation(rb.angularVelocity.y / 5f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float turnInput = actionBuffers.ContinuousActions[0]; // -1 to 1
        float moveInput = actionBuffers.ContinuousActions[1]; // -1 to 1

        // 1. Movement Logic
        rb.AddForce(transform.forward * moveInput * moveSpeed, ForceMode.Acceleration);

        // 2. Turning Logic
        rb.AddTorque(transform.up * turnInput * turnSpeed, ForceMode.Acceleration);

        // --- REWARDS ---
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Reward for moving mostly forward
        AddReward(forwardSpeed * 0.005f);

        // Penalize moving strictly backward
        if (forwardSpeed < -1f)
        {
            AddReward(-0.01f);
        }

        // Penalize standing still
        if (Mathf.Abs(forwardSpeed) < 0.2f)
        {
            AddReward(-0.002f);
        }

        // 3. Episode timeout
        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer >= maxEpisodeTime)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Horizontal"); // A/D or Left/Right
        actions[1] = Input.GetAxis("Vertical");   // W/S or Up/Down
    }
}
