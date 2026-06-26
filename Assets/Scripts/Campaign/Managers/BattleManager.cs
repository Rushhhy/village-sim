using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CombatInput combatInput;

    [Header("Battle Rules")]
    [SerializeField] private int roundLimit = 10;
    [SerializeField] private int starRoundTarget = 6;

    [Header("End Battle Timing")]
    [SerializeField] private float resultScreenDelay = 0.6f;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text roundsText;
    [SerializeField] private TMP_Text causeText;

    [Header("Live Round UI")]
    [SerializeField] private GameObject roundCounterContainer;
    [SerializeField] private TMP_Text roundCounterText;

    [Header("Condition Rows")]
    [SerializeField] private Image conditionStar1;
    [SerializeField] private Image conditionStar2;
    [SerializeField] private Image conditionStar3;

    [SerializeField] private TMP_Text conditionText1;
    [SerializeField] private TMP_Text conditionText2;
    [SerializeField] private TMP_Text conditionText3;

    [Header("Final Stars")]
    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;

    [Header("Star Sprites")]
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;

    public int CurrentRound { get; private set; } = 1;
    public bool BattleOver { get; private set; }

    private int playerDeaths;
    private Coroutine endBattleRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (roundCounterContainer != null)
            roundCounterContainer.SetActive(false);
    }

    private void Start()
    {
        InitializeBattle();
    }

    public void InitializeBattle()
    {
        Time.timeScale = 1f;

        BattleOver = false;
        CurrentRound = 1;
        playerDeaths = 0;

        if (endBattleRoutine != null)
        {
            StopCoroutine(endBattleRoutine);
            endBattleRoutine = null;
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (roundCounterContainer != null)
            roundCounterContainer.SetActive(false);

        UpdateRoundCounterUI();
    }

    public void StartBattle()
    {
        BattleOver = false;
        CurrentRound = 1;
        playerDeaths = 0;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (roundCounterContainer != null)
            roundCounterContainer.SetActive(true);

        UpdateRoundCounterUI();
    }

    public void NotifyUnitDied(Unit deadUnit)
    {
        if (BattleOver || deadUnit == null)
            return;

        if (deadUnit.team == Unit.Team.Player)
            playerDeaths++;

        CheckImmediateEndConditions();
    }

    public void NotifyRoundAdvanced()
    {
        if (BattleOver)
            return;

        CurrentRound++;
        UpdateRoundCounterUI();

        if (CurrentRound > roundLimit)
        {
            EndBattle(false, "Round limit exceeded");
        }
    }

    public void CheckImmediateEndConditions()
    {
        if (BattleOver)
            return;

        int livingPlayers = CountLivingUnits(Unit.Team.Player);
        int livingEnemies = CountLivingUnits(Unit.Team.Enemy);

        if (livingEnemies <= 0)
        {
            EndBattle(true, "");
            return;
        }

        if (livingPlayers <= 0)
        {
            EndBattle(false, "All units defeated");
            return;
        }
    }

    private int CountLivingUnits(Unit.Team team)
    {
#if UNITY_2023_1_OR_NEWER
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        Unit[] allUnits = FindObjectsOfType<Unit>();
#endif

        int count = 0;

        foreach (var u in allUnits)
        {
            if (u == null) continue;
            if (u.team != team) continue;
            if (!u.IsAlive) continue;
            if (u.CurrentTile == null) continue;

            count++;
        }

        return count;
    }

    private void EndBattle(bool victory, string defeatCause)
    {
        if (BattleOver)
            return;

        BattleOver = true;

        if (combatInput != null)
            combatInput.ForceDeselect();

        if (endBattleRoutine != null)
            StopCoroutine(endBattleRoutine);

        endBattleRoutine = StartCoroutine(EndBattleRoutine(victory, defeatCause));
    }

    private IEnumerator EndBattleRoutine(bool victory, string defeatCause)
    {
        if (resultScreenDelay > 0f)
            yield return new WaitForSeconds(resultScreenDelay);

        ShowResultScreen(victory, defeatCause);

        endBattleRoutine = null;
    }

    private void ShowResultScreen(bool victory, string defeatCause)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        bool conditionWin = victory;
        bool conditionSpeed = victory && CurrentRound <= starRoundTarget;
        bool conditionNoDeaths = victory && playerDeaths == 0;

        int starsEarned = 0;
        if (conditionWin) starsEarned++;
        if (conditionSpeed) starsEarned++;
        if (conditionNoDeaths) starsEarned++;

        if (resultTitleText != null)
            resultTitleText.text = victory ? "Victory" : "Game Over";

        if (roundsText != null)
            roundsText.text = $"Round: {CurrentRound}";

        if (causeText != null)
            causeText.text = victory ? "" : defeatCause;

        if (conditionText1 != null)
            conditionText1.text = "Win the battle";

        if (conditionText2 != null)
            conditionText2.text = $"Finish within {starRoundTarget} rounds";

        if (conditionText3 != null)
            conditionText3.text = "No units lost";

        SetStar(conditionStar1, conditionWin);
        SetStar(conditionStar2, conditionSpeed);
        SetStar(conditionStar3, conditionNoDeaths);

        SetStar(star1, starsEarned >= 1);
        SetStar(star2, starsEarned >= 2);
        SetStar(star3, starsEarned >= 3);

        Time.timeScale = 0f;
    }

    private void UpdateRoundCounterUI()
    {
        if (roundCounterText != null)
            roundCounterText.text = $"Round {CurrentRound} / {roundLimit}";
    }

    private void SetStar(Image img, bool filled)
    {
        if (img == null) return;
        if (filledStarSprite == null || emptyStarSprite == null) return;

        img.sprite = filled ? filledStarSprite : emptyStarSprite;
    }

    public void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    public void SetStarRoundTarget(int target)
    {
        starRoundTarget = target;
    }
}