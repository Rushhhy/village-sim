using UnityEngine;

public class EnemyVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void Initialize(EnemyData data)
    {
        if (data == null) return;

        if (data.Icon != null)
            spriteRenderer.sprite = data.Icon;

        if (data.Animator == null)
        {
            Debug.LogWarning($"EnemyVisual: No Animator controller on enemy '{data.Name}' (ID {data.ID})");
            return;
        }

        animator.runtimeAnimatorController = data.Animator;
    }

    public Animator Animator => animator;
}
