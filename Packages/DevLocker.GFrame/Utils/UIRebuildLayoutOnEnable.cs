using DevLocker.GFrame.Utils;
using UnityEngine;

public class UIRebuildLayoutOnEnable : MonoBehaviour
{
    private bool m_RebuildDone = false;
    
    void OnEnable()
    {
        m_RebuildDone = false;
    }

    private void Update()
    {
        if (!m_RebuildDone && UIUtils.IsLayoutRebuildPending()) {
            m_RebuildDone = true;
            UIUtils.ForceRecalclulateLayouts((RectTransform)transform);
        }
    }
}
