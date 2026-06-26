using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketOrderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI rewardAmountText;

    [SerializeField] private Image itemOneIcon;
    [SerializeField] private TextMeshProUGUI itemOneAmount;

    [SerializeField] private Image itemTwoIcon;
    [SerializeField] private TextMeshProUGUI itemTwoAmount;

    [SerializeField] private Image itemThreeIcon;
    [SerializeField] private TextMeshProUGUI itemThreeAmount;

    [SerializeField] private GameObject completeButton;
    [SerializeField] private GameObject notCompleteButton;
    [SerializeField] private Button refreshButton;

    private Market market;
    private MarketOrder order;

    public void Initialize(Market market, MarketOrder order, ResourceSO resources)
    {
        this.market = market;
        this.order = order;

        titleText.text = "Order #" + order.orderNumber;
        rewardAmountText.text = order.gemReward > 0 ? order.gemReward.ToString() : order.coinReward.ToString() + "x";

        SetupItem(0, itemOneIcon, itemOneAmount, resources);
        SetupItem(1, itemTwoIcon, itemTwoAmount, resources);
        SetupItem(2, itemThreeIcon, itemThreeAmount, resources);

        completeButton.GetComponent<Button>().onClick.RemoveAllListeners();
        completeButton.GetComponent<Button>().onClick.AddListener(() => market.CompleteOrder(order, gameObject));

        refreshButton.onClick.RemoveAllListeners();
        refreshButton.onClick.AddListener(() => market.RefreshOrder(order, gameObject));

        RefreshCompleteState();
    }

    private void SetupItem(int index, Image icon, TextMeshProUGUI amountText, ResourceSO resources)
    {
        if (index >= order.resourceIDs.Length)
        {
            icon.transform.parent.gameObject.SetActive(false);
            return;
        }

        icon.transform.parent.gameObject.SetActive(true);

        int resourceID = order.resourceIDs[index];
        icon.sprite = resources.resourcesData[resourceID].Icon;
        icon.preserveAspect = true;

        amountText.text = order.amounts[index] + "x";
    }

    public void RefreshCompleteState()
    {
        bool canComplete = market.CanCompleteOrder(order);

        completeButton.SetActive(canComplete);
        notCompleteButton.SetActive(!canComplete);
    }
}