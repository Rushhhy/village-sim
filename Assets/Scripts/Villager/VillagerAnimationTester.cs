using System.Collections.Generic;
using UnityEngine;

public enum TestAnimatorType
{
    Villager,
    Battle,
    BattleHorse
}

public class VillagerAnimationTester : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private VillagersDataSO villagersData;
    [SerializeField] private VillagerVisual villagerPrefab;   // <— swap type here
    [SerializeField] private Transform root;

    [Header("Grid Layout")]
    [SerializeField] private int columns = 5;
    [SerializeField] private float xSpacing = 2f;
    [SerializeField] private float ySpacing = 2f;

    [Header("Animator Selection")]
    [SerializeField] private TestAnimatorType animatorType = TestAnimatorType.Villager;

    [Header("Playback")]
    [SerializeField] public string stateName = "Mine";

    private readonly List<Animator> animators = new List<Animator>();
    private Transform Parent => root != null ? root : transform;

    private void Start()
    {
        SpawnGrid();
    }

    [ContextMenu("Respawn Grid")]
    public void SpawnGrid()
    {
        // clear children
        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(Parent.GetChild(i).gameObject);
            else
                Destroy(Parent.GetChild(i).gameObject);
#else
            Destroy(Parent.GetChild(i).gameObject);
#endif
        }

        animators.Clear();

        if (villagersData == null || villagerPrefab == null)
        {
            Debug.LogError("VillagerAnimationTester: Missing VillagersDataSO or villagerPrefab.");
            return;
        }

        for (int i = 0; i < villagersData.villagersData.Count; i++)
        {
            VillagerData data = villagersData.villagersData[i];

            int row = i / columns;
            int col = i % columns;
            Vector3 pos = new Vector3(col * xSpacing, -row * ySpacing, 0f);

            VillagerVisual instance = Instantiate(villagerPrefab, pos, Quaternion.identity, Parent);
            instance.Initialize(data, animatorType);

            animators.Add(instance.Animator);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Tester: Space pressed. Attempting to play state: " + stateName);
            PlayOnAll();
        }
    }

    public void PlayOnAll()
    {
        Debug.Log("Tester: PlayOnAll called. Animators count = " + animators.Count);

        foreach (var anim in animators)
        {
            if (anim == null)
            {
                Debug.LogWarning("Tester: Found null animator in list.");
                continue;
            }

            Debug.Log("Tester: Playing state '" + stateName + "' on " + anim.gameObject.name);
            anim.Play(stateName, 0, 0f);
        }
    }
}
