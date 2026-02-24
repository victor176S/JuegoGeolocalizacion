
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class GPSTracker : MonoBehaviour
{
    public static GPSTracker Instance;

    [Header("AR")]
        public ARRaycastManager raycastManager;
        public GameObject[] monsterPrefabs; 
        private GameObject spawnedMonster;

    [Header("UI")]
        public TextMeshProUGUI gpsStatusText;
        public TextMeshProUGUI distanceText;
        public GameObject combatUI;
        public GameObject panelVictoria;

    [Header("Music")]
        public AudioSource combatMusic;
        public AudioSource spawnMusic;

    private GameObject spawnedObject;



    public double currentLat;
    public double currentLon;
    private bool isSpawned = false;
    private float detectionRadius = 15f; 

    public List<Monster> monsters = new List<Monster>();
    public int currentMonsterIndex = 0;
    private MonsterController mb;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panelVictoria.SetActive(false);
        combatUI.SetActive(false);
        StartGPS();
        LoadMonster();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGPS();

        if(Input.location.status == LocationServiceStatus.Running)
        {
            CheckMonsterDistance();
        }

    }

    #region GPS

    void StartGPS()
    {
        if (!Input.location.isEnabledByUser)
        {
            gpsStatusText.text = "GPS no habilitado";
            return;
        }

        Input.location.Start();
    }

    void UpdateGPS()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            currentLat = Input.location.lastData.latitude;
            currentLon = Input.location.lastData.longitude;
            gpsStatusText.text = $"Lat: {currentLat}\nLon: {currentLon}";
        }
    }

    #endregion

    #region Monsters
    void LoadMonster()
    {
        /*Cordenadas clase probar*/ 
        //monsters.Add(new Monster { mosnterName = "Fenix", latitude = 37.19224901303922, longitude = -3.6166398805055926, health = 100, defeated = false });


        monsters.Add(new Monster { mosnterName = "Fenix", latitude = 37.19222686616466, longitude = -3.616983154096119, health = 100, defeated = false });
        monsters.Add(new Monster { mosnterName = "Minotauro", latitude = 37.191878754572755, longitude = -3.617152208305987, health = 150, defeated = false });
        monsters.Add(new Monster { mosnterName = "Hidra", latitude = 37.1922275014041, longitude = -3.6169823566711927, health = 130, defeated = false });
        monsters.Add(new Monster { mosnterName = "Quimera", latitude = 37.192095371646886, longitude = -3.616837225226872, health = 120, defeated = false });
    }

    #endregion

    #region Distance

    void CheckMonsterDistance()
    {
        if (currentMonsterIndex >= monsters.Count) return;

        Monster currentMonster = monsters[currentMonsterIndex];

        double distance = CalculateDistance(currentLat, currentLon, currentMonster.latitude, currentMonster.longitude);
        distanceText.text = $"Distancia al {currentMonster.mosnterName}: {distance:F2} metros";

        if (distance <= detectionRadius && !isSpawned)
        {
            SpawnMonsterInAr();
        }
    }
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000; // Radio de la Tierra en metros
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Mathf.Sin((float)dLat / 2) * Mathf.Sin((float)dLat / 2) +
                   Mathf.Cos((float)ToRadians(lat1)) * Mathf.Cos((float)ToRadians(lat2)) *
                   Mathf.Sin((float)dLon / 2) * Mathf.Sin((float)dLon / 2);

        double c = 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a)));
        return R * c;
    }

    double ToRadians(double degrees) => degrees * Mathf.PI / 180;

    #endregion


    #region Spawn Monster

    void SpawnMonsterInAr()
    {
        if (isSpawned) return;

         isSpawned = true;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if(raycastManager.Raycast(
            new Vector2(Screen.width / 2, Screen.height / 2),
            hits,
            UnityEngine.XR.ARSubsystems.TrackableType.Planes))
        {
            Vector3 spawnPosition = hits[0].pose.position;
            Quaternion spawnRotation = hits[0].pose.rotation;

            spawnMusic.Play();

            spawnedMonster = Instantiate(
                monsterPrefabs[currentMonsterIndex],
                spawnPosition,
                spawnRotation
            );


            mb = spawnedMonster.GetComponent<MonsterController>();
            mb.Initialize(monsters[currentMonsterIndex].health, combatUI.GetComponentInChildren<TextMeshProUGUI>());

            if (combatMusic != null && !combatMusic.isPlaying)
            {
                combatMusic.Play();
            }


            //isSpawned = true;
            combatUI.SetActive(true);

        }
        else
        {
            isSpawned = false;
        }
    }

    #endregion

    #region Combat

    public void Shoot()
    {
        if(spawnedMonster != null)
        {
            spawnedMonster.GetComponent<MonsterController>().TakeDamage(20);
        }
    }

    public void MonsterDefeated()
    {
        monsters[currentMonsterIndex].defeated = true;

        Destroy(spawnedMonster);
        spawnedMonster = null;

        isSpawned = false;
        combatUI.SetActive(false);

        currentMonsterIndex++;

        if(currentMonsterIndex >= monsters.Count)
        {
            panelVictoria.SetActive(true);
        }

    }

    #endregion
}
