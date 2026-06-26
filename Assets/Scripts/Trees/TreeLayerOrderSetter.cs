using System.Collections;
using UnityEngine;

public class TreeLayerOrderSetter : MonoBehaviour
{
    private PlacementSystem placementManager;

    // Wait until placement manager is instantiated
    private IEnumerator WaitForPlacementManager()
    {
        while (placementManager == null)
        {
            placementManager = FindObjectOfType<PlacementSystem>();
            yield return null; // Wait until next frame
        }
        ProcessTrees();
    }

    // Scan trees at Start
    void Start()
    {
        StartCoroutine(WaitForPlacementManager());
    }

    // Scan trees
    private void ProcessTrees()
    {
        foreach (Transform tree in transform)
        {
            Vector3Int treeGridPosition = new Vector3Int(Mathf.FloorToInt(tree.position.x), Mathf.FloorToInt(tree.position.y), 0);

            if (!placementManager.gridData.positionHasNature.Contains(treeGridPosition))
            {
                placementManager.gridData.positionHasNature.Add(treeGridPosition);
            }

            SpriteRenderer sr = tree.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = Mathf.RoundToInt(-tree.position.y * 10);
                if (tree.CompareTag("Rock"))
                {
                    sr.sortingOrder += 1;
                }
            }
        }
    }
}
