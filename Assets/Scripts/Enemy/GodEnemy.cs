using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GodEnemy : MonoBehaviour
{
    [SerializeField] private GameObject godPrefab;
    public GameObject player;
    public float speed = 5;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnEnable()
    {
        GameTimer.OnFiveMinutesPassed += SpawnGod;
    }

    private void OnDisable()
    {
        GameTimer.OnFiveMinutesPassed -= SpawnGod;

    }

    [SerializeField] private float spawnRadius = 15f;

    private void SpawnGod()
    {
        Vector2 randomOffset2D = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPosition = player.transform.position +
                                new Vector3(randomOffset2D.x, 0f, randomOffset2D.y);

        Instantiate(godPrefab, spawnPosition, Quaternion.identity);
    }

    private void Update()
    {

        transform.LookAt(player.transform);


        transform.position = Vector3.MoveTowards(
     transform.position,
     player.transform.position,
     speed * Time.deltaTime);
    }
}
