using UnityEngine;
using UnityEngine.UI;

public class CombatPreviewUI : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panel;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action onConfirm;
    private System.Action onCancel;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
                Hide();
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Hide();
            });
        }
    }

    public void Show(System.Action confirmAction, System.Action cancelAction = null)
    {
        if (panel == null)
            return;

        onConfirm = confirmAction;
        onCancel = cancelAction;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        onConfirm = null;
        onCancel = null;
    }

    public bool IsVisible => panel != null && panel.activeInHierarchy;
}