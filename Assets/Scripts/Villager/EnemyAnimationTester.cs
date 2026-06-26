using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationTester : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private EnemiesDataSO enemiesData;
    [SerializeField] private EnemyVisual enemyPrefab;
    [SerializeField] private Transform root;

    [Header("Grid Layout")]
    [SerializeField] private int columns = 5;
    [SerializeField] private float xSpacing = 2f;
    [SerializeField] private float ySpacing = 2f;

    [Header("Playback")]
    [SerializeField] private string stateName = "Side Idle";

    private readonly List<Animator> animators = new List<Animator>();
    private Transform Parent => root != null ? root : transform;

    private void Start()
    {
        SpawnGrid();
    }

    [ContextMenu("Respawn Grid")]
    public void SpawnGrid()
    {
        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(Parent.GetChild(i).gameObject);
            else Destroy(Parent.GetChild(i).gameObject);
#else
            Destroy(Parent.GetChild(i).gameObject);
#endif
        }

        animators.Clear();

        if (enemiesData == null || enemyPrefab == null)
        {
            Debug.LogError("EnemyAnimationTester: Missing EnemiesDataSO or enemyPrefab.");
            return;
        }

        for (int i = 0; i < enemiesData.enemiesData.Count; i++)
        {
            EnemyData data = enemiesData.enemiesData[i];

            int row = i / columns;
            int col = i % columns;
            Vector3 pos = new Vector3(col * xSpacing, -row * ySpacing, 0f);

            EnemyVisual instance = Instantiate(enemyPrefab, pos, Quaternion.identity, Parent);
            instance.Initialize(data);

            if (instance.Animator != null)
                animators.Add(instance.Animator);
        }
    }

    [ContextMenu("Play State On All")]
    public void PlayOnAll()
    {
        foreach (var anim in animators)
        {
            if (anim == null) continue;
            anim.Play(stateName, 0, 0f);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayOnAll();
        }
    }
}
