using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Karatahta: biten görevlerin denklemleri, her satır bir denklem (tebeşir rengi).</summary>
    public sealed class WorkshopChalkboardView : RegisteredView<IChalkboardView>, IChalkboardView
    {
        [SerializeField] private TMP_Text text;
        [Tooltip("İlk satır (ör. ustanın kuralı). Boş bırakılabilir.")]
        [SerializeField] private string header = "";

        private readonly StringBuilder sb = new StringBuilder(256);

        public void SetEquations(IReadOnlyList<string> richEquations)
        {
            if (text == null) return;
            sb.Clear();
            if (!string.IsNullOrEmpty(header)) sb.Append(header).Append('\n');
            for (int i = 0; i < richEquations.Count; i++) sb.Append(richEquations[i]).Append('\n');
            text.text = sb.ToString();
        }
    }
}
