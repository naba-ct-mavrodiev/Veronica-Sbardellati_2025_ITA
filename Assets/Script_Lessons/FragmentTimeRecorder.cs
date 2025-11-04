using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Records the movement of fragments during destruction and can reverse the process
/// </summary>
public class FragmentTimeRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private float recordingDuration = 3f;
    [SerializeField] private float samplingInterval = 0.1f;
    [SerializeField] private bool autoStartRecording = true;
    
    [Header("Reversal Settings")]
    [SerializeField] private float reversalDuration = 2f;
    [SerializeField] private AnimationCurve reversalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool freezeFragmentsOnReversal = true;
    
    [Header("Fragment Detection")]
    [SerializeField] private bool autoDetectFragments = true;
    [SerializeField] private List<Transform> fragments = new List<Transform>();
    
    [Header("Events")]
    public UnityEvent OnRecordingStarted;
    public UnityEvent OnRecordingComplete;
    public UnityEvent OnReversalStarted;
    public UnityEvent OnReversalComplete;
    
    private Dictionary<Transform, FragmentData> fragmentDataMap = new Dictionary<Transform, FragmentData>();
    private bool isRecording = false;
    private bool isReversing = false;
    private Coroutine recordingCoroutine;
    private Coroutine reversalCoroutine;
    
    /// <summary>
    /// Stores position and rotation data for a fragment over time
    /// </summary>
    [System.Serializable]
    private class FragmentData
    {
        public Transform fragment;
        public Vector3 initialPosition;
        public Quaternion initialRotation;
        public List<TransformSnapshot> snapshots = new List<TransformSnapshot>();
        public Rigidbody rigidBody;
        public bool wasKinematic;
        
        public FragmentData(Transform frag)
        {
            fragment = frag;
            initialPosition = frag.position;
            initialRotation = frag.rotation;
            rigidBody = frag.GetComponent<Rigidbody>();
            wasKinematic = rigidBody != null ? rigidBody.isKinematic : false;
        }
    }
    
    /// <summary>
    /// A snapshot of transform data at a specific time
    /// </summary>
    [System.Serializable]
    private struct TransformSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
        
        public TransformSnapshot(Transform transform, float time)
        {
            position = transform.position;
            rotation = transform.rotation;
            timestamp = time;
        }
    }
    
    private void Start()
    {
        if (autoDetectFragments)
        {
            DetectFragments();
        }
        
        InitializeFragmentData();
        
        if (autoStartRecording)
        {
            StartRecording();
        }
    }
    
    /// <summary>
    /// Automatically detects fragments with Rigidbody components
    /// </summary>
    private void DetectFragments()
    {
        fragments.Clear();
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        
        foreach (var rb in rigidbodies)
        {
            if (rb.transform != transform) // Exclude the parent object
            {
                fragments.Add(rb.transform);
            }
        }
        
        Debug.Log($"Auto-detected {fragments.Count} fragments");
    }
    
    /// <summary>
    /// Initializes the fragment data dictionary
    /// </summary>
    private void InitializeFragmentData()
    {
        fragmentDataMap.Clear();
        
        foreach (var fragment in fragments)
        {
            if (fragment != null)
            {
                fragmentDataMap[fragment] = new FragmentData(fragment);
            }
        }
    }
    
    /// <summary>
    /// Starts recording fragment positions
    /// </summary>
    public void StartRecording()
    {
        if (isRecording || isReversing)
        {
            Debug.LogWarning("Cannot start recording while already recording or reversing");
            return;
        }
        
        isRecording = true;
        OnRecordingStarted?.Invoke();
        
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        
        recordingCoroutine = StartCoroutine(RecordingCoroutine());
    }
    
    /// <summary>
    /// Stops recording and triggers completion event
    /// </summary>
    public void StopRecording()
    {
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        
        isRecording = false;
        OnRecordingComplete?.Invoke();
    }
    
    /// <summary>
    /// Coroutine that handles the recording process
    /// </summary>
    private IEnumerator RecordingCoroutine()
    {
        float startTime = Time.time;
        float nextSampleTime = startTime;
        
        while (Time.time - startTime < recordingDuration)
        {
            if (Time.time >= nextSampleTime)
            {
                RecordSnapshot();
                nextSampleTime += samplingInterval;
            }
            
            yield return null;
        }
        
        // Record final snapshot
        RecordSnapshot();
        
        isRecording = false;
        OnRecordingComplete?.Invoke();
    }
    
    /// <summary>
    /// Records a snapshot of all fragment positions
    /// </summary>
    private void RecordSnapshot()
    {
        float currentTime = Time.time;
        
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Key != null)
            {
                var snapshot = new TransformSnapshot(kvp.Key, currentTime);
                kvp.Value.snapshots.Add(snapshot);
            }
        }
    }
    
    /// <summary>
    /// Reverses the destruction by animating fragments back to their original positions
    /// </summary>
    [ContextMenu("Reverse Destruction")] // [ContextMenu("Reverse Destruction", false, 100)]
    public void ReverseDestruction()
    {
        if (isReversing || isRecording)
        {
            Debug.LogWarning("Cannot reverse while recording or already reversing");
            return;
        }
        
        if (fragmentDataMap.Count == 0)
        {
            Debug.LogWarning("No fragment data available for reversal");
            return;
        }
        
        isReversing = true;
        OnReversalStarted?.Invoke();
        
        if (reversalCoroutine != null)
        {
            StopCoroutine(reversalCoroutine);
        }
        
        reversalCoroutine = StartCoroutine(ReversalCoroutine());
    }
    
    /// <summary>
    /// Coroutine that handles the reversal animation
    /// </summary>
    private IEnumerator ReversalCoroutine()
    {
        // Freeze fragments if required
        if (freezeFragmentsOnReversal)
        {
            FreezeAllFragments();
        }
        
        float startTime = Time.time;
        
        // Store current positions as reversal start points
        Dictionary<Transform, Vector3> startPositions = new Dictionary<Transform, Vector3>();
        Dictionary<Transform, Quaternion> startRotations = new Dictionary<Transform, Quaternion>();
        
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Key != null)
            {
                startPositions[kvp.Key] = kvp.Key.position;
                startRotations[kvp.Key] = kvp.Key.rotation;
            }
        }
        
        while (Time.time - startTime < reversalDuration)
        {
            float progress = (Time.time - startTime) / reversalDuration;
            float curveProgress = reversalCurve.Evaluate(progress);
            
            foreach (var kvp in fragmentDataMap)
            {
                if (kvp.Key != null)
                {
                    var fragmentData = kvp.Value;
                    
                    // Interpolate from current position to initial position
                    Vector3 targetPosition = Vector3.Lerp(startPositions[kvp.Key], fragmentData.initialPosition, curveProgress);
                    Quaternion targetRotation = Quaternion.Lerp(startRotations[kvp.Key], fragmentData.initialRotation, curveProgress);
                    
                    kvp.Key.position = targetPosition;
                    kvp.Key.rotation = targetRotation;
                }
            }
            
            yield return null;
        }
        
        // Ensure final positions are exactly the initial positions
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Key != null)
            {
                kvp.Key.position = kvp.Value.initialPosition;
                kvp.Key.rotation = kvp.Value.initialRotation;
            }
        }
        
        // Restore rigidbody states
        RestoreRigidbodyStates();
        
        isReversing = false;
        OnReversalComplete?.Invoke();
    }
    
    /// <summary>
    /// Reverses destruction following the recorded path in reverse
    /// </summary>
    [ContextMenu("Reverse Destruction With Path")] // [ContextMenu("Reverse Destruction With Path", false, 100)]
    public void ReverseDestructionWithPath()
    {
        if (isReversing || isRecording)
        {
            Debug.LogWarning("Cannot reverse while recording or already reversing");
            return;
        }
        
        isReversing = true;
        OnReversalStarted?.Invoke();
        
        if (reversalCoroutine != null)
        {
            StopCoroutine(reversalCoroutine);
        }
        
        reversalCoroutine = StartCoroutine(PathReversalCoroutine());
    }
    
    /// <summary>
    /// Reversal coroutine that follows the recorded path in reverse
    /// </summary>
    private IEnumerator PathReversalCoroutine()
    {
        if (freezeFragmentsOnReversal)
        {
            FreezeAllFragments();
        }
        
        // Find the maximum number of snapshots for timing
        int maxSnapshots = 0;
        foreach (var kvp in fragmentDataMap)
        {
            maxSnapshots = Mathf.Max(maxSnapshots, kvp.Value.snapshots.Count);
        }
        
        if (maxSnapshots <= 1)
        {
            // Fallback to simple reversal
            yield return StartCoroutine(ReversalCoroutine());
            yield break;
        }
        
        float startTime = Time.time;
        
        while (Time.time - startTime < reversalDuration)
        {
            float progress = (Time.time - startTime) / reversalDuration;
            float curveProgress = reversalCurve.Evaluate(progress);
            
            // Reverse progress (1 to 0)
            float reverseProgress = 1f - curveProgress;
            
            foreach (var kvp in fragmentDataMap)
            {
                if (kvp.Key != null && kvp.Value.snapshots.Count > 0)
                {
                    var snapshots = kvp.Value.snapshots;
                    
                    // Calculate which snapshot index to use
                    float snapshotIndex = reverseProgress * (snapshots.Count - 1);
                    int lowerIndex = Mathf.FloorToInt(snapshotIndex);
                    int upperIndex = Mathf.CeilToInt(snapshotIndex);
                    
                    lowerIndex = Mathf.Clamp(lowerIndex, 0, snapshots.Count - 1);
                    upperIndex = Mathf.Clamp(upperIndex, 0, snapshots.Count - 1);
                    
                    if (lowerIndex == upperIndex)
                    {
                        // Exact snapshot
                        kvp.Key.position = snapshots[lowerIndex].position;
                        kvp.Key.rotation = snapshots[lowerIndex].rotation;
                    }
                    else
                    {
                        // Interpolate between snapshots
                        float t = snapshotIndex - lowerIndex;
                        Vector3 pos = Vector3.Lerp(snapshots[lowerIndex].position, snapshots[upperIndex].position, t);
                        Quaternion rot = Quaternion.Lerp(snapshots[lowerIndex].rotation, snapshots[upperIndex].rotation, t);
                        
                        kvp.Key.position = pos;
                        kvp.Key.rotation = rot;
                    }
                }
            }
            
            yield return null;
        }
        
        // Ensure final positions are initial positions
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Key != null)
            {
                kvp.Key.position = kvp.Value.initialPosition;
                kvp.Key.rotation = kvp.Value.initialRotation;
            }
        }
        
        RestoreRigidbodyStates();
        
        isReversing = false;
        OnReversalComplete?.Invoke();
    }
    
    /// <summary>
    /// Freezes all fragments by making their rigidbodies kinematic
    /// </summary>
    private void FreezeAllFragments()
    {
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Value.rigidBody != null)
            {
                kvp.Value.rigidBody.isKinematic = true;
                kvp.Value.rigidBody.linearVelocity = Vector3.zero;
                kvp.Value.rigidBody.angularVelocity = Vector3.zero;
            }
        }
    }
    
    /// <summary>
    /// Restores rigidbody states to their original configuration
    /// </summary>
    private void RestoreRigidbodyStates()
    {
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Value.rigidBody != null)
            {
                kvp.Value.rigidBody.isKinematic = kvp.Value.wasKinematic;
                kvp.Value.rigidBody.linearVelocity = Vector3.zero;
                kvp.Value.rigidBody.angularVelocity = Vector3.zero;
            }
        }
    }
    
    /// <summary>
    /// Clears all recorded data
    /// </summary>
    public void ClearRecordedData()
    {
        foreach (var kvp in fragmentDataMap)
        {
            kvp.Value.snapshots.Clear();
        }
    }
    
    /// <summary>
    /// Resets fragments to their initial positions immediately
    /// </summary>
    public void ResetToInitialPositions()
    {
        foreach (var kvp in fragmentDataMap)
        {
            if (kvp.Key != null)
            {
                kvp.Key.position = kvp.Value.initialPosition;
                kvp.Key.rotation = kvp.Value.initialRotation;
                
                if (kvp.Value.rigidBody != null)
                {
                    kvp.Value.rigidBody.linearVelocity = Vector3.zero;
                    kvp.Value.rigidBody.angularVelocity = Vector3.zero;
                }
            }
        }
    }
    
    // Public getters for status
    public bool IsRecording => isRecording;
    public bool IsReversing => isReversing;
    public int FragmentCount => fragments.Count;
    public float RecordingProgress => isRecording && recordingCoroutine != null ? 
        Mathf.Clamp01((Time.time - Time.time) / recordingDuration) : 0f;
}