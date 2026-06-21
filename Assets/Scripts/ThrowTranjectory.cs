using UnityEngine;
public class ThrowTrajectory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private BuilderController builderController;

    [Header("Dots")]
    [SerializeField] private int dotCount = 18;
    [SerializeField] private float timeStep = 0.07f; // seconds of flight time between each dot
    [SerializeField] private float dotWorldSize = 0.15f;
    [SerializeField] private Color dotColor = Color.white;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100; // high so it draws above most things
    [SerializeField] private float gravityMultiplier = 1.05f;

    [Header("Visibility (for later split-screen co-op)")]
    [Tooltip("Optional: create a Layer (Project Settings > Tags and Layers) just for this, e.g. \"PlayerOnlyVFX\", " +
             "then on the Builder's camera, untick that layer in its Culling Mask so the Builder's screen never renders it. " +
             "Leave as \"Default\" if you're not doing split-screen yet — doesn't matter for solo testing.")]
    [SerializeField] private string dotLayerName = "Default";

    [Header("Behaviour")]
    [Tooltip("Stop drawing dots past the first point that's already inside the ground, so the line doesn't poke through walls/floors.")]
    [SerializeField] private bool stopAtGround = true;

    private Transform[] _dots;
    private Sprite _dotSprite;

    private void Awake()
    {
        _dotSprite = GenerateDotSprite();
        BuildDotPool();
    }

    private void Update()
    {
        if (playerController == null || builderController == null)
        {
            HideAllDots();
            return;
        }

        bool charging = playerController.ThrowChargeRatio > 0f && playerController.IsCarryingBuilder;

        if (!charging)
        {
            HideAllDots();
            return;
        }

        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        Vector2 origin = playerController.BuilderCarrySlot.position;
        Vector2 velocity = playerController.PreviewThrowVelocity;
        float gravity = builderController.FallAcceleration;
        LayerMask groundLayer = builderController.GroundLayer;

        for (int i = 0; i < _dots.Length; i++)
        {
            float t = (i + 1) * timeStep; // skip t=0, that's just the origin/Builder's current spot

            Vector2 point =
                origin +
                velocity * t +
                0.5f * new Vector2(0f, -gravity * gravityMultiplier) * (t * t);

            if (stopAtGround && Physics2D.OverlapPoint(point, groundLayer) != null)
            {
                // Hide this dot and everything after it — the arc "ends" here.
                for (int j = i; j < _dots.Length; j++)
                    _dots[j].gameObject.SetActive(false);
                return;
            }

            _dots[i].gameObject.SetActive(true);
            _dots[i].position = point;
        }
    }

    private void HideAllDots()
    {
        if (_dots == null) return;
        foreach (var dot in _dots)
            dot.gameObject.SetActive(false);
    }

    private void BuildDotPool()
    {
        _dots = new Transform[dotCount];

        for (int i = 0; i < dotCount; i++)
        {
            var dotObj = new GameObject($"TrajectoryDot_{i}");
            dotObj.transform.SetParent(transform);
            dotObj.transform.localScale = Vector3.one * dotWorldSize;

            var sr = dotObj.AddComponent<SpriteRenderer>();
            sr.sprite = _dotSprite;
            sr.color = dotColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;

            dotObj.SetActive(false);
            _dots[i] = dotObj.transform;
        }
    }

    // Draws a small filled white circle into a Texture2D — this is the
    // "no asset needed" part. Runs once at startup, result is reused by
    // every dot.
    private Sprite GenerateDotSprite()
    {
        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}