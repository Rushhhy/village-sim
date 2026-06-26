using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Ranged / Projectile")]
    [SerializeField] private ArrowProjectile arrowPrefab;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private float arrowTravelTime = 0.35f;
    [SerializeField] private float arrowArcHeight = 0.6f;

    [SerializeField] private float arrowReleaseDelay = 0.15f;

    [Header("Hit Feedback")]
    private float hitScaleMultiplier = 1.5f;
    private float hitSquashDuration = 0.08f;
    private float hitRecoverDuration = 0.1f;
    private Color hitFlashColor = new Color(1f, 0.7f, 0.7f, 1f);
    private float hitFlashIntensity = 1f;

    [Header("Death FX")]
    [SerializeField] private GameObject deathSkullPrefab;
    private float deathDuration = 0.5f;
    [SerializeField] private Color deathFlashColor = Color.white;
    [SerializeField] private float deathScaleShrink = 0.8f;

    [Header("Guard / Encirclement")]
    [SerializeField] private int maxGuardStacks = 3;
    [SerializeField] private float brokenDamageBonus = 0.25f;

    [Header("Special Attack")]
    [SerializeField] private bool hasSpecialAttack = true;
    [SerializeField] private int specialAttackDamage = 8;
    [SerializeField] private int specialAttackRange = 1;
    [SerializeField] private int specialCooldownTurns = 3;
    [SerializeField] private int currentSpecialCooldown = 0;

    public int CurrentGuardStacks { get; private set; }

    public bool IsGuardBroken => CurrentGuardStacks <= 0;
    public int MaxGuardStacks => maxGuardStacks;

    public bool HasSpecialAttack => hasSpecialAttack;
    public bool CanUseSpecial => hasSpecialAttack && currentSpecialCooldown <= 0;
    public int SpecialAttackDamage => specialAttackDamage;
    public int SpecialAttackRange => specialAttackRange;
    public int CurrentSpecialCooldown => currentSpecialCooldown;
    public int SpecialCooldownTurns => specialCooldownTurns;

    private Coroutine hitFeedbackRoutine;

    public enum Team { Player, Enemy }
    public Team team;

    [Header("Stats")]
    public int maxHP = 20;
    public int hp = 20;
    public int attackDamage = 5;
    public int attackRange = 1;
    public int moveRange = 5;

    [Header("Movement")]
    [Tooltip("Seconds per tile moved.")]
    public float moveTimePerTile = 0.18f;

    [Header("Turn State")]
    [SerializeField] private bool hasMoved;
    [SerializeField] private bool hasActed;

    [Header("Refs")]
    [SerializeField] private GridManager grid;

    [Header("UI")]
    public HealthBarUI healthBar;

    private Animator anim;
    private SpriteRenderer sr;

    public Tile CurrentTile { get; private set; }

    public bool HasMoved
    {
        get => hasMoved;
        set => hasMoved = value;
    }

    public bool HasActed
    {
        get => hasActed;
        set => hasActed = value;
    }

    public bool IsAlive => hp > 0;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        if (grid == null)
            grid = FindObjectOfType<GridManager>();

        hp = Mathf.Clamp(hp, 0, maxHP);

        CurrentGuardStacks = maxGuardStacks;
        currentSpecialCooldown = Mathf.Max(0, currentSpecialCooldown);

        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBarUI>();

        if (healthBar != null)
        {
            healthBar.Refresh(hp, maxHP);
            healthBar.RefreshGuard(CurrentGuardStacks, maxGuardStacks);
        }
    }

    // =========================================================
    // PUBLIC API USED BY OTHER SCRIPTS
    // =========================================================

    public IEnumerator MoveToTile(Tile tile)
    {
        if (CombatCameraController.Instance != null)
        {
            yield return CombatCameraController.Instance.RunWithFocus(
                transform,
                MoveToTileInternal(tile)
            );
        }
        else
        {
            yield return MoveToTileInternal(tile);
        }
    }

    public IEnumerator AttackUnit(Unit target)
    {
        if (CombatCameraController.Instance != null)
        {
            yield return CombatCameraController.Instance.RunWithFocus(
                transform,
                AttackUnitInternal(target, false)
            );
        }
        else
        {
            yield return AttackUnitInternal(target, false);
        }
    }

    public IEnumerator UseSpecialAttack(Unit target)
    {
        if (!CanUseSpecial)
            yield break;

        if (CombatCameraController.Instance != null)
        {
            yield return CombatCameraController.Instance.RunWithFocus(
                transform,
                AttackUnitInternal(target, true)
            );
        }
        else
        {
            yield return AttackUnitInternal(target, true);
        }
    }

    public bool IsTargetInNormalRange(Unit target)
    {
        if (target == null || CurrentTile == null || target.CurrentTile == null)
            return false;

        int dist = MovementService.Manhattan(CurrentTile.Coord, target.CurrentTile.Coord);
        return dist <= attackRange;
    }

    public bool IsTargetInSpecialRange(Unit target)
    {
        if (!HasSpecialAttack || target == null || CurrentTile == null || target.CurrentTile == null)
            return false;

        int dist = MovementService.Manhattan(CurrentTile.Coord, target.CurrentTile.Coord);
        return dist <= specialAttackRange;
    }

    public void TickSpecialCooldown()
    {
        if (currentSpecialCooldown > 0)
            currentSpecialCooldown--;
    }

    public void PutSpecialOnCooldown()
    {
        if (!hasSpecialAttack) return;
        currentSpecialCooldown = Mathf.Max(0, specialCooldownTurns);
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"{name} took {amount} dmg. HP={hp}/{maxHP}");

        if (healthBar != null)
            healthBar.Refresh(hp, maxHP);

        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.Spawn(amount, transform.position);

        PlayHitFeedback();

        if (hp <= 0)
        {
            Die();
        }
    }

    public void Place(Tile tile)
    {
        if (tile == null) return;

        if (CurrentTile != null && CurrentTile.Occupant == this)
            CurrentTile.Occupant = null;

        CurrentTile = tile;
        tile.Occupant = this;
        transform.position = tile.transform.position;
    }

    public void FaceTowards(Vector2Int targetCoord)
    {
        if (CurrentTile == null || anim == null || sr == null) return;

        Vector2Int delta = targetCoord - CurrentTile.Coord;
        if (delta == Vector2Int.zero) return;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            anim.Play("Side Idle");
            sr.flipX = (delta.x > 0);
        }
        else
        {
            sr.flipX = false;
            anim.Play(delta.y > 0 ? "Back Idle" : "Front Idle");
        }
    }

    // =========================================================
    // INTERNAL MOVEMENT LOGIC
    // =========================================================

    private IEnumerator MoveToTileInternal(Tile target)
    {
        if (target == null) yield break;

        if (CurrentTile == null)
        {
            Place(target);
            yield break;
        }

        if (grid == null)
        {
            grid = FindObjectOfType<GridManager>();
            if (grid == null)
            {
                Debug.LogError("Unit: No GridManager in scene.");
                yield break;
            }
        }

        List<Tile> path = BuildPathTo(target);
        if (path == null || path.Count == 0)
            yield break;

        Vector2Int lastDelta = Vector2Int.zero;

        foreach (var stepTile in path)
        {
            if (stepTile == null) continue;
            if (CurrentTile == null)
            {
                Place(stepTile);
                continue;
            }

            Vector2Int delta = stepTile.Coord - CurrentTile.Coord;
            lastDelta = delta;

            PlayWalkAnimation(delta);

            if (CurrentTile != null && CurrentTile.Occupant == this)
                CurrentTile.Occupant = null;

            CurrentTile = stepTile;
            stepTile.Occupant = this;

            Vector3 startPos = transform.position;
            Vector3 endPos = stepTile.transform.position;

            float t = 0f;
            float duration = Mathf.Max(0.01f, moveTimePerTile);

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            transform.position = endPos;
        }

        if (lastDelta != Vector2Int.zero)
        {
            PlayIdleAnimation(lastDelta);
        }
    }

    private List<Tile> BuildPathTo(Tile target)
    {
        if (CurrentTile == null || target == null) return null;
        if (target == CurrentTile) return new List<Tile>();

        var cameFrom = new Dictionary<Tile, Tile>();
        var dist = new Dictionary<Tile, int>();
        var queue = new Queue<Tile>();

        queue.Enqueue(CurrentTile);
        dist[CurrentTile] = 0;

        bool found = false;

        while (queue.Count > 0)
        {
            Tile t = queue.Dequeue();
            int d = dist[t];

            if (t == target)
            {
                found = true;
                break;
            }

            if (d == moveRange) continue;

            foreach (var n in grid.GetNeighbors(t))
            {
                if (n == null) continue;
                if (!n.Walkable) continue;
                if (n.Occupant != null && n != CurrentTile) continue;
                if (dist.ContainsKey(n)) continue;

                dist[n] = d + 1;
                cameFrom[n] = t;
                queue.Enqueue(n);
            }
        }

        if (!found)
            return null;

        var path = new List<Tile>();
        Tile cur = target;

        while (cur != CurrentTile)
        {
            path.Add(cur);
            if (!cameFrom.TryGetValue(cur, out cur))
            {
                return null;
            }
        }

        path.Reverse();
        return path;
    }

    // =========================================================
    // INTERNAL ATTACK LOGIC
    // =========================================================

    private IEnumerator AttackUnitInternal(Unit target, bool useSpecial)
    {
        if (target == null) yield break;
        if (!IsAlive) yield break;
        if (CurrentTile == null || target.CurrentTile == null) yield break;

        if (useSpecial && !CanUseSpecial)
            yield break;

        int usedRange = useSpecial ? specialAttackRange : attackRange;
        int usedDamage = useSpecial ? specialAttackDamage : attackDamage;

        int dist = MovementService.Manhattan(CurrentTile.Coord, target.CurrentTile.Coord);
        if (dist > usedRange)
            yield break;

        Vector2Int delta = target.CurrentTile.Coord - CurrentTile.Coord;

        FaceTowards(target.CurrentTile.Coord);

        if (target.IsAlive && target.CurrentTile != null)
        {
            target.FaceTowards(CurrentTile.Coord);
        }

        PlayAttackAnimation(delta);

        if (usedRange > 1 && arrowPrefab != null)
        {
            if (arrowReleaseDelay > 0f)
                yield return new WaitForSeconds(arrowReleaseDelay);

            var arrow = Instantiate(
                arrowPrefab,
                transform.position,
                Quaternion.identity
            );

            arrow.Launch(
                target.transform,
                arrowSprite,
                arrowTravelTime,
                arrowArcHeight
            );
        }

        yield return new WaitForSeconds(arrowTravelTime);

        target.ReceiveAttack(this, usedDamage);

        if (useSpecial)
        {
            PutSpecialOnCooldown();
            Debug.Log($"{name} used SPECIAL on {target.name}. Cooldown set to {currentSpecialCooldown}.");
        }

        if (BattleManager.Instance != null && BattleManager.Instance.BattleOver)
            yield break;

        yield return new WaitForSeconds(0.1f);

        if (IsAlive && CurrentTile != null)
        {
            PlayIdleAnimation(delta);
        }
    }

    // =========================================================
    // ANIMATION HELPERS
    // =========================================================

    private void PlayWalkAnimation(Vector2Int delta)
    {
        if (anim == null || sr == null) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            anim.Play("Side Walk");
            sr.flipX = (delta.x > 0);
        }
        else
        {
            sr.flipX = false;
            anim.Play(delta.y > 0 ? "Back Walk" : "Front Walk");
        }
    }

    private void PlayIdleAnimation(Vector2Int delta)
    {
        if (anim == null || sr == null) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            anim.Play("Side Idle");
            sr.flipX = (delta.x > 0);
        }
        else
        {
            sr.flipX = false;
            anim.Play(delta.y > 0 ? "Back Idle" : "Front Idle");
        }
    }

    private void PlayAttackAnimation(Vector2Int delta)
    {
        if (anim == null || sr == null) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            anim.Play("Side Attack");
            sr.flipX = (delta.x > 0);
        }
        else
        {
            sr.flipX = false;
            anim.Play(delta.y > 0 ? "Back Attack" : "Front Attack");
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        Debug.Log($"{name} died.");

        BattleManager.Instance?.NotifyUnitDied(this);

        if (CurrentTile != null && CurrentTile.Occupant == this)
        {
            CurrentTile.Occupant = null;
            CurrentTile = null;
        }

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        if (hitFeedbackRoutine != null)
        {
            StopCoroutine(hitFeedbackRoutine);
            hitFeedbackRoutine = null;
        }

        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (sr == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector3 originalScale = transform.localScale;
        Color originalColor = sr.color;

        float flashTime = deathDuration * 0.3f;
        float fadeTime = deathDuration * 0.7f;

        if (deathSkullPrefab != null)
        {
            Instantiate(deathSkullPrefab, transform.position, Quaternion.identity);
        }

        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / flashTime);

            sr.color = Color.Lerp(originalColor, deathFlashColor, lerp);

            yield return null;
        }

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeTime);

            transform.localScale = Vector3.Lerp(originalScale, originalScale * deathScaleShrink, lerp);

            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, lerp);
            sr.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }

    public void PlayHitFeedback()
    {
        if (hitFeedbackRoutine != null)
            StopCoroutine(hitFeedbackRoutine);

        hitFeedbackRoutine = StartCoroutine(HitFeedbackCoroutine());
    }

    private IEnumerator HitFeedbackCoroutine()
    {
        if (sr == null)
            yield break;

        Vector3 originalScale = transform.localScale;
        Color originalColor = sr.color;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / hitSquashDuration;
            float eased = 1f - (1f - t) * (1f - t);

            float scale = Mathf.Lerp(1f, hitScaleMultiplier, eased);
            transform.localScale = originalScale * scale;

            sr.color = Color.Lerp(originalColor, hitFlashColor, hitFlashIntensity * eased);

            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / hitRecoverDuration;
            float eased = t * t;

            transform.localScale = Vector3.Lerp(originalScale * hitScaleMultiplier, originalScale, eased);
            sr.color = Color.Lerp(hitFlashColor, originalColor, eased);

            yield return null;
        }

        transform.localScale = originalScale;
        sr.color = originalColor;
        hitFeedbackRoutine = null;
    }

    public void ResetGuard()
    {
        CurrentGuardStacks = maxGuardStacks;

        if (healthBar != null)
            healthBar.RefreshGuard(CurrentGuardStacks, maxGuardStacks);
    }

    private void ConsumeGuardStackIfMelee(Unit attacker)
    {
        if (attacker == null) return;

        bool isMelee = attacker.attackRange <= 1;

        if (!isMelee) return;
        if (CurrentGuardStacks <= 0) return;

        CurrentGuardStacks = Mathf.Max(0, CurrentGuardStacks - 1);
        if (healthBar != null)
            healthBar.RefreshGuard(CurrentGuardStacks, maxGuardStacks);

        Debug.Log($"{name} guard reduced: {CurrentGuardStacks}/{maxGuardStacks}");
    }

    public void ReceiveAttack(Unit attacker, int baseDamage)
    {
        if (!IsAlive) return;

        ConsumeGuardStackIfMelee(attacker);

        float multiplier = 1f;

        if (IsGuardBroken)
        {
            multiplier += brokenDamageBonus;
        }

        int finalDamage = Mathf.CeilToInt(baseDamage * multiplier);

        if (IsGuardBroken)
        {
            Debug.Log($"{name} is GUARD-BROKEN! Taking boosted damage: {finalDamage} (from {baseDamage})");
        }

        TakeDamage(finalDamage);
    }

    public void RemoveFromTile()
    {
        if (CurrentTile != null && CurrentTile.Occupant == this)
        {
            CurrentTile.Occupant = null;
        }

        CurrentTile = null;
    }
    public void ApplyVillagerData(VillagerData data, bool cavalry)
    {
        if (data == null) return;

        attackRange = data.range;

        Animator unitAnimator = GetComponent<Animator>();

        if (unitAnimator != null)
        {
            if (cavalry && data.battleHorseAnimatorController != null)
                unitAnimator.runtimeAnimatorController = data.battleHorseAnimatorController;
            else if (data.battleAnimatorController != null)
                unitAnimator.runtimeAnimatorController = data.battleAnimatorController;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && data.villagerIcon != null)
        {
            sr.sprite = cavalry && data.horseIcon != null ? data.horseIcon : data.villagerIcon;
        }
    }
}
