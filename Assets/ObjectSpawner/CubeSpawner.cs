using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    public GameObject cubePrefab; // Assign this in the inspector
    private float timer = 0f;
    private float spawnInterval = 4f;
    private float minDistance = 3f;
    private float maxDistance = 6f;
    private Vector3 gridCenter;
    private Vector3 pyramidCenter;
    private float safeRadius = 12f; // Adjust as needed for your scene
    public float cubeSpacing = 1f; // Set this in the Inspector to match your cube's size

    // Start is called once before the first execution of Update after the MonoBehaviour is created 
    void Start()
    {
        SpawnGrid(); // Comment this out if you don't want the grid to spawn at start
        //SpawnPyramid(); // Spawns the pyramid next to the grid
        // Set centers for spawn checks
        gridCenter = transform.position;
        float gridWidth = (4 - 1) * 5f; // 4 is gridCols, 5f is spacing
        pyramidCenter = transform.position + new Vector3(gridWidth / 2f + 5f * 1.5f, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCube();
        }
    }

    void SpawnCube()
    {
        // Pick a random direction on the XZ plane
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minDistance, maxDistance);
        Vector3 spawnPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y) * distance;
        // Check if spawnPos is too close to grid or pyramid
        if (Vector3.Distance(spawnPos, gridCenter) < safeRadius || Vector3.Distance(spawnPos, pyramidCenter) < safeRadius)
        {
            // Too close, skip spawning
            return;
        }
        Instantiate(cubePrefab, spawnPos, Quaternion.identity);
    }

    void SpawnGrid()
    {
        int gridRows = 4;
        int gridCols = 4;
        float spacing = 1.05f;
        float distanceFromCamera = 7f;
        float gridWidth = (gridCols - 1) * spacing;
        float gridHeight = (gridRows - 1) * spacing;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 centerInFront = cam.transform.position + cam.transform.forward * distanceFromCamera;
        Vector3 startPos = centerInFront - new Vector3(gridWidth / 2f, gridHeight / 2f, 0);

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Vector3 spawnPos = startPos + new Vector3(col * spacing, row * spacing, 0);
                Instantiate(cubePrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    void SpawnPyramid()
    {
        int[] layerSizes = {4, 3, 2, 1}; // Number of cubes per side for each layer
        float spacing = cubeSpacing; // Use the public variable
        float epsilon = 0.001f; // Tiny offset to avoid physics overlap
        float distanceFromCamera = 7f;
        float offsetFromCenter = 7f;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 baseCenter = cam.transform.position + cam.transform.forward * distanceFromCamera + cam.transform.right * offsetFromCenter;
        baseCenter.y = 0;

        float y = spacing / 2f;
        for (int l = 0; l < layerSizes.Length; l++)
        {
            int layerSize = layerSizes[l];
            float layerOffset = (layerSize - 1) * spacing / 2f;
            for (int x = 0; x < layerSize; x++)
            {
                for (int z = 0; z < layerSize; z++)
                {
                    Vector3 spawnPos = baseCenter + new Vector3(x * spacing - layerOffset, y, z * spacing - layerOffset);
                    GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);
                    Rigidbody rb = cube.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = cube.AddComponent<Rigidbody>();
                    }
                    rb.useGravity = false;
                }
            }
            y += spacing + epsilon; // Next layer up, add epsilon to avoid overlap
        }
    }
}
