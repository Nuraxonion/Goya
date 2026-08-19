#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utilities for the Art Shop.
///
/// Adding an upgrade means adding a panel that looks exactly like the ones
/// already there, so rather than rebuilding the button and six pips by hand this
/// clones an existing panel and wires the result into a new ArtShopManager slot.
/// Menu: Tools > Art Shop.
/// </summary>
public static class ArtShopPanelTool
{
    // How far to the right of the last panel a new one lands. Rough by design:
    // nudging it in the Scene view afterwards is expected.
    const float HorizontalOffset = 250f;

    // Pips are matched by name so the cloned array keeps the order the shop
    // fills them in.
    const string PipNamePrefix = "Pip";


    // =========================================================
    // ADD UPGRADE PANEL
    // =========================================================

    [MenuItem("Tools/Art Shop/Add Upgrade Panel")]
    static void AddUpgradePanel()
    {
        ArtShopManager manager = Object.FindAnyObjectByType<ArtShopManager>();

        if (manager == null)
        {
            Fail("No ArtShopManager in the open scene. Open Assets/Scenes/ArtShop.unity first.");
            return;
        }

        RectTransform source = FindTemplatePanel(manager);

        if (source == null)
        {
            Fail("Could not find a panel to clone. The first upgrade slot needs a " +
                 "button whose parent is the panel holding it and its pips.");
            return;
        }

        // -----------------------------------------------------------------
        // CLONE
        // -----------------------------------------------------------------

        int slotCount = manager.upgradeSlots != null ? manager.upgradeSlots.Length : 0;

        RectTransform clone = Object.Instantiate(source, source.parent);
        clone.name = "UpgradePanel" + (slotCount + 1);

        Undo.RegisterCreatedObjectUndo(clone.gameObject, "Add Upgrade Panel");

        // Instantiate does not carry the RectTransform layout across reliably when
        // the anchors are non standard, and this panel's are - copy them explicitly.
        clone.anchorMin = source.anchorMin;
        clone.anchorMax = source.anchorMax;
        clone.pivot = source.pivot;
        clone.sizeDelta = source.sizeDelta;
        clone.localScale = source.localScale;
        clone.anchoredPosition =
            source.anchoredPosition + new Vector2(HorizontalOffset * slotCount, 0f);

        clone.SetAsLastSibling();

        // -----------------------------------------------------------------
        // FIND THE PIECES IN THE CLONE
        // -----------------------------------------------------------------

        Button cloneButton = clone.GetComponentInChildren<Button>(true);

        if (cloneButton != null)
        {
            cloneButton.name = "UpgradeButton" + (slotCount + 1);
        }

        // Any onClick entries copied from the template are harmless - the manager
        // calls RemoveAllListeners on every slot button in Start.

        Image[] clonePips = CollectPips(clone);

        // -----------------------------------------------------------------
        // APPEND THE SLOT
        // -----------------------------------------------------------------

        SerializedObject so = new SerializedObject(manager);
        SerializedProperty slots = so.FindProperty("upgradeSlots");

        slots.arraySize = slotCount + 1;

        SerializedProperty slot = slots.GetArrayElementAtIndex(slotCount);

        slot.FindPropertyRelative("upgradeId").stringValue = "";
        slot.FindPropertyRelative("button").objectReferenceValue = cloneButton;
        slot.FindPropertyRelative("background").objectReferenceValue = null;

        SerializedProperty pips = slot.FindPropertyRelative("pips");
        pips.arraySize = clonePips.Length;

        for (int i = 0; i < clonePips.Length; i++)
        {
            pips.GetArrayElementAtIndex(i).objectReferenceValue = clonePips[i];
        }

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeGameObject = clone.gameObject;

        Debug.Log($"Added upgrade slot {slotCount + 1}. Button wired: " +
                  $"{cloneButton != null}. Pips wired: {clonePips.Length}. " +
                  "Set its Upgrade Id to match an entry in MetaUpgradeSet.asset.");
    }


    // =========================================================
    // REWIRE PIPS
    // =========================================================

    /// <summary>
    /// Re-finds each slot's pips from the panel its button lives in. Use after
    /// renaming or reordering pip objects in the scene.
    /// </summary>
    [MenuItem("Tools/Art Shop/Rewire Slot Pips")]
    static void RewireSlotPips()
    {
        ArtShopManager manager = Object.FindAnyObjectByType<ArtShopManager>();

        if (manager == null)
        {
            Fail("No ArtShopManager in the open scene.");
            return;
        }

        SerializedObject so = new SerializedObject(manager);
        SerializedProperty slots = so.FindProperty("upgradeSlots");

        int rewired = 0;

        for (int i = 0; i < slots.arraySize; i++)
        {
            SerializedProperty slot = slots.GetArrayElementAtIndex(i);

            Button button =
                slot.FindPropertyRelative("button").objectReferenceValue as Button;

            if (button == null || button.transform.parent == null)
            {
                continue;
            }

            Image[] found = CollectPips(button.transform.parent);

            SerializedProperty pips = slot.FindPropertyRelative("pips");
            pips.arraySize = found.Length;

            for (int p = 0; p < found.Length; p++)
            {
                pips.GetArrayElementAtIndex(p).objectReferenceValue = found[p];
            }

            rewired++;
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        Debug.Log($"Rewired pips on {rewired} upgrade slot(s).");
    }


    // =========================================================
    // DEBUG: SET LEVELS
    // =========================================================

    /// <summary>
    /// Sets every upgrade to its maximum level, so an effect can be felt in game
    /// before its button exists. Levels are plain PlayerPrefs, so this is the same
    /// thing buying them would do.
    /// </summary>
    [MenuItem("Tools/Art Shop/Debug/Max All Upgrades")]
    static void MaxAllUpgrades()
    {
        SetAllUpgrades(true);
    }

    [MenuItem("Tools/Art Shop/Debug/Reset All Upgrades")]
    static void ResetAllUpgrades()
    {
        SetAllUpgrades(false);
    }

    static void SetAllUpgrades(bool maxed)
    {
        MetaUpgradeSet set = MetaUpgrades.Set;

        if (set == null || set.upgrades == null)
        {
            Fail("No MetaUpgradeSet found at Assets/Resources/MetaUpgradeSet.asset.");
            return;
        }

        foreach (MetaUpgrade upgrade in set.upgrades)
        {
            if (upgrade == null)
            {
                continue;
            }

            MetaUpgrades.SetLevel(upgrade, maxed ? upgrade.MaxLevel : 0);
        }

        Debug.Log(maxed
            ? "All meta upgrades set to maximum level."
            : "All meta upgrades reset to level 0.");
    }


    // =========================================================
    // HELPERS
    // =========================================================

    /// <summary>The panel of the first slot that has a usable button.</summary>
    static RectTransform FindTemplatePanel(ArtShopManager manager)
    {
        if (manager.upgradeSlots == null)
        {
            return null;
        }

        foreach (ArtShopManager.UpgradeSlot slot in manager.upgradeSlots)
        {
            if (slot == null || slot.button == null)
            {
                continue;
            }

            Transform panel = slot.button.transform.parent;

            if (panel != null)
            {
                return panel as RectTransform;
            }
        }

        return null;
    }

    /// <summary>
    /// Every Image named Pip1, Pip2, ... under the panel, in numeric order. The
    /// PipBackground objects are skipped: only the inner pips get coloured.
    /// </summary>
    static Image[] CollectPips(Transform panel)
    {
        List<Image> found = new List<Image>();

        // Numbered rather than hierarchy ordered, because the pips sit inside
        // their own PipBackground parents and the sibling order of those has
        // drifted from the display order before.
        for (int number = 1; ; number++)
        {
            string wanted = PipNamePrefix + number;
            Image match = null;

            foreach (Image candidate in panel.GetComponentsInChildren<Image>(true))
            {
                if (candidate.name == wanted)
                {
                    match = candidate;
                    break;
                }
            }

            if (match == null)
            {
                break;
            }

            found.Add(match);
        }

        return found.ToArray();
    }

    static void Fail(string message)
    {
        EditorUtility.DisplayDialog("Art Shop", message, "OK");
    }
}
#endif
