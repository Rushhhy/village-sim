using UnityEngine;
using UnityEngine.UI;

public class DeploymentUnitSlot : MonoBehaviour
{
    [Header("Wiring")]
    public Unit unit; // assign the Unit reference (MVP)
    [SerializeField] private Image portrait;
    [SerializeField] private GameObject deployedCheck;
    [SerializeField] private GameObject highlightFrame;

    [Header("Colors")]
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color deployedColor = new Color(1f, 1f, 1f, 0.35f);

    public bool IsDeployed { get; private set; }

    private void Awake()
    {
        RefreshPortraitFromUnit();
        SetDeployed(false);
        SetSelected(false);
    }

    public void RefreshPortraitFromUnit()
    {
        if (portrait == null || unit == null) return;

        var sr = unit.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        portrait.sprite = sr.sprite;
        portrait.preserveAspect = true;
    }

    public void SetDeployed(bool deployed)
    {
        IsDeployed = deployed;

        if (portrait != null)
            portrait.color = deployed ? deployedColor : availableColor;

        if (deployedCheck != null)
            deployedCheck.SetActive(deployed);
    }

    public void SetSelected(bool selected)
    {
        if (highlightFrame != null)
            highlightFrame.SetActive(selected);
    }
}