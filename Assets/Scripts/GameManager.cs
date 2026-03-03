using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Range(1f, 20f)] public float timeScale = 10f;
    public float testingDurationMinutes = 10f;
    public bool isTesting;

    [Header("Folders")]
    private Transform folderDecor;
    private Transform folderRoad;
    private Transform folderWalls;
    private Transform folderObstacles;
    private Transform folderCheckpoints;
    private Transform folderSpawnpoints;
    private Transform folderCars;
    private Transform folderTracks;

    [Header("Lists")]
    private Transform[] cameraPivots;
    private CarController[] carControllers;
    private CarAgent[] carAgents;
    private Transform[] checkpoints;

    [Header("Components")]
    private Transform groundTransform;
    private CameraController cameraController;
    private Lidar scanner;

    [Header("Variables")]
    [SerializeField] private int startTrackIndex;
    private int trackIndex;
    private int checkpointCount;
    private int carCount;
    private int carIndex;
    private int trackCount;

    [Header("Spawn Settings")]
    [SerializeField] private bool clearObstacles = true;
    [SerializeField] private bool spawnObstacles = false;
    [SerializeField] private int obstacleCount = 3;
    [SerializeField] private bool clearDecorations = true;
    [SerializeField] private bool spawnDecorations = true;
    [SerializeField] [Range(0f, 1f)] private float decorSpawnChance = 0.1f;

    void Awake()
    {
        SetupSingletons();
        SetupManager();
        ResetManager();
        Time.timeScale = timeScale;
        if (isTesting) Invoke("QuitGame", testingDurationMinutes * 60f * timeScale);
    }

    void OnValidate()
    {
        Time.timeScale = timeScale;
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
    }

    void SetupSingletons()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        GameObject.FindWithTag("UI").GetComponent<UI>().DoAwake();
        GetComponent<StatsManager>().DoAwake();
    }

    void SetupManager()
    {
        // Tracks
        trackIndex = startTrackIndex;
        folderTracks = GameObject.FindWithTag("Environment").transform;
        trackCount = folderTracks.childCount;
        foreach (Transform t in folderTracks) t.gameObject.SetActive(true);

        // Cars
        folderCars = GameObject.FindWithTag("folder_cars").transform;
        carCount = folderCars.childCount;

        // Lists
        cameraPivots = new Transform[carCount];
        carControllers = new CarController[carCount];
        carAgents = new CarAgent[carCount];

        // Populate lists
        for (int i = 0; i < carCount; i++)
        {
            cameraPivots[i] = folderCars.GetChild(i).transform.GetChild(1).GetChild(0);
            carControllers[i] = folderCars.GetChild(i).GetComponent<CarController>();
            carAgents[i] = folderCars.GetChild(i).GetComponent<CarAgent>();
        }

        // Scanner
        scanner = FindObjectOfType<Lidar>();

        // Checkpoints
        SpawnCheckpoints[] allCheckpointsSpawners = FindObjectsOfType<SpawnCheckpoints>();
        foreach (SpawnCheckpoints spawner in allCheckpointsSpawners) spawner.DoSpawn();

        // Walls
        WallSpawner[] wallWallSpawners = FindObjectsOfType<WallSpawner>();
        foreach (WallSpawner spawner in wallWallSpawners) spawner.DoSpawn();

        // Camera
        KeepOneCamera();
        cameraController = Camera.main.gameObject.GetComponent<CameraController>();
    }

    void ResetManager()
    {
        // Setup track
        foreach (Transform t in folderTracks) t.gameObject.SetActive(false);
        folderTracks.GetChild(trackIndex).gameObject.SetActive(true);

        // Stats manager
        StatsManager.Instance.SetMap(folderTracks.GetChild(trackIndex).gameObject.name.Split("_")[0]);
        StatsManager.Instance.ResetStats();

        // Folders
        folderDecor = FindChildWithTag(folderTracks.GetChild(trackIndex), "folder_decorations");
        folderRoad = FindChildWithTag(folderTracks.GetChild(trackIndex), "folder_road");
        folderWalls = FindChildWithTag(folderTracks.GetChild(trackIndex), "folder_walls");
        folderObstacles = FindChildWithTag(folderTracks.GetChild(trackIndex), "folder_obstacles");
        folderCheckpoints = FindChildWithTag(folderTracks.GetChild(trackIndex), "folder_checkpoints");
        folderSpawnpoints = FindChildWithTag(folderTracks.GetChild(trackIndex), "folder_spawnpoints");

        // Checkpoints
        checkpointCount = folderCheckpoints.childCount;
        ActivateCheckpoints();
        checkpoints = new Transform[checkpointCount];
        for (int i = 0; i < checkpointCount; i++) checkpoints[i] = folderCheckpoints.GetChild(i);

        // Ground
        groundTransform = FindChildWithTag(folderTracks.GetChild(trackIndex), "Ground");

        // Scanner
        scanner.SetTarget(carControllers[carIndex].transform);
        scanner.ResetMap(groundTransform);

        // Decorations
        folderDecor.GetComponent<SpawnDecor>().DoSpawn(groundTransform, spawnDecorations, clearDecorations, decorSpawnChance);

        // Obstacles
        folderObstacles.GetComponent<StaticObstacles>().DoSpawn(folderCheckpoints, spawnObstacles, clearObstacles, obstacleCount);

        // Camera
        cameraController.SetStartValues(cameraPivots, 0);

        // Randomise spawnpoints
        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < folderSpawnpoints.childCount; i++)
        {
            spawnPoints.Add(folderSpawnpoints.GetChild(i));
        }
        System.Random rng = new System.Random();
        spawnPoints = spawnPoints.OrderBy(x => rng.Next()).ToList();

        // Setup cars
        for (int i = 0; i < carCount; i++)
        {
            carControllers[i].SetStartPosition(spawnPoints[i].position, folderSpawnpoints.localScale.x < 0f ? -90f : 90f);
            carControllers[i].gameObject.SetActive(true);
        }

        //UI
        UI.Instance.SetMap(0);
        UI.Instance.SwitchCar(carIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) carAgents[carIndex].SwitchControlMode();
        else if (Input.GetKeyDown(KeyCode.N)) carAgents[carIndex].alwaysAccelerate = !carAgents[carIndex].alwaysAccelerate;
        else if (Input.GetKeyDown(KeyCode.B)) SwitchCar(1);
        else if (Input.GetKeyDown(KeyCode.V)) LoadTrack(1);
        else if (Input.GetKeyDown(KeyCode.C)) LoadTrack(-1);
        else if (Input.GetKeyDown(KeyCode.R)) ResetRun();
    }

    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag)) return child;
        }
        return null;
    }

    void LoadTrack(int direction)
    {
        cameraController.transform.parent = null;
        foreach (Transform c in folderCars) c.gameObject.SetActive(false);
        trackIndex = (trackIndex + direction + trackCount) % trackCount;
        ResetManager();
    }

    void SwitchCar(int dir)
    {
        carAgents[carIndex].SwitchControlMode(true);
        carIndex = (carIndex + dir + carCount) % carCount;
        cameraController.SwitchToCar(carIndex);
        UI.Instance.SwitchCar(carIndex);
    }

    void KeepOneCamera()
    {
        Camera[] cameras = Camera.allCameras;
        Camera cameraToKeep = cameras[0];
        foreach (Camera cam in cameras)
        {
            if (cam != cameraToKeep) Destroy(cam.gameObject);
        }
    }

    void ResetRun()
    {
        foreach (CarController c in carControllers) c.ResetPosition();
    }

    public void ActivateCheckpoints()
    {
        foreach (Transform t in folderCheckpoints) t.gameObject.SetActive(true);
    }

    public void EndEpisode()
    {

    }

    public Transform GetCheckpoint(int index) => checkpoints[index];
    public int GetCheckpointCount() => checkpointCount;
}
