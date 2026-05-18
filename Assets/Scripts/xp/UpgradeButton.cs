using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButton : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    UpgradeData currentUpgrade;
    UpgradeManager manager;

    public void Setup(
        UpgradeData data,
        UpgradeManager upgManager)
    {
        currentUpgrade = data;
        manager = upgManager;

        titleText.text = data.upgradeName;
        descText.text = data.description;
    }

    public void OnClick()
    {
        manager.SelectUpgrade(currentUpgrade);
    }
}