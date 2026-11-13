using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>TEST YER TUTUCUSU — HUD görev hedefi (sağ üst).</summary>
    public sealed class PlaceholderObjectiveView : RegisteredView<IObjectiveView>, IObjectiveView
    {
        [SerializeField] private TMP_Text label;

        public void SetObjective(string text)
        {
            if (label == null) return;
            label.text = text;
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}
