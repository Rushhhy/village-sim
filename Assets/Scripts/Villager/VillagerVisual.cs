using UnityEngine;

public class VillagerVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Initialize(VillagerData data, TestAnimatorType type)
    {
        if (data == null)
        {
            Debug.LogError("VillagerVisual.Initialize called with null VillagerData");
            return;
        }

        // Pick which controller to use
        RuntimeAnimatorController ctrl = null;
        switch (type)
        {
            case TestAnimatorType.Villager:
                ctrl = data.villagerAnimatorController;
                if (data.villagerIcon != null)
                    spriteRenderer.sprite = data.villagerIcon;
                break;

            case TestAnimatorType.Battle:
                ctrl = data.battleAnimatorController;
                if (data.villagerIcon != null)
                    spriteRenderer.sprite = data.villagerIcon;
                break;

            case TestAnimatorType.BattleHorse:
                ctrl = data.battleHorseAnimatorController;
                if (data.horseIcon != null)
                    spriteRenderer.sprite = data.horseIcon;
                break;
        }

        if (ctrl == null)
        {
            Debug.LogWarning(
                $"VillagerVisual: No controller for {type} on villager '{data.Name}' (ID {data.ID})"
            );
            return;
        }

        animator.runtimeAnimatorController = ctrl;
    }

    public Animator Animator => animator;
}
