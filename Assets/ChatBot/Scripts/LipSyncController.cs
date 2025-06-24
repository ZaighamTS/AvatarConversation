using UnityEngine;

public class LipSyncController : MonoBehaviour
{
    public AudioSource audioSource;
    public SkinnedMeshRenderer faceRenderer;
    public string blendShapeName = "mouthOpen"; // Might be "JawOpen" or similar
    public float blendShapeMultiplier = 100f; // Scale to match your blendshape range

    private int blendShapeIndex = -1;
    private float[] samples = new float[64];

    void Start()
    {
        if (faceRenderer != null && !string.IsNullOrEmpty(blendShapeName))
        {
            blendShapeIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
            if (blendShapeIndex == -1)
                Debug.LogError($"BlendShape '{blendShapeName}' not found.");
        }
    }

    void Update()
    {
        if (audioSource != null && audioSource.isPlaying && blendShapeIndex != -1)
        {
            audioSource.GetOutputData(samples, 0);
            float level = 0f;
            foreach (var s in samples)
                level += Mathf.Abs(s);

            level = Mathf.Clamp01(level / samples.Length * 10f); // Normalize
            float blendValue = level * blendShapeMultiplier;
            faceRenderer.SetBlendShapeWeight(blendShapeIndex, blendValue);
        }
        else if (blendShapeIndex != -1)
        {
            // Reset when not talking
            faceRenderer.SetBlendShapeWeight(blendShapeIndex, 0f);
        }
    }
}
