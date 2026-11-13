using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölye ekonomisi: başlangıç kesesi, Usta'nın borcu ve kayıt için madde kataloğu.
    /// Fiyatlar maddenin kendisinde (SpeciesData.price), ödeme siparişte (OrderTask.reward).
    /// Pazarda ne satıldığı burada değil, her tezgâhın kendi MarketCatalog'unda.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Ekonomi", fileName = "Economy")]
    public sealed class EconomyData : ScriptableObject
    {
        [Min(0)] [SerializeField] private int startingMoney = 30;
        [Tooltip("Para, pazarda satılan en ucuz kavanoza yetmezse Usta keseye bu kadar koyar (oyun kilitlenmez).")]
        [Min(1)] [SerializeField] private int loanAmount = 10;
        [Tooltip("Kayıttan geri yüklemek için bütün maddeler (formül = kimlik).")]
        [SerializeField] private List<SpeciesData> catalog = new List<SpeciesData>();

        public int StartingMoney => startingMoney;
        public int LoanAmount => loanAmount;

        /// <summary>Kayıttaki formülden maddeyi bulur; yoksa null.</summary>
        public SpeciesData FindSpecies(string formula)
        {
            if (string.IsNullOrEmpty(formula)) return null;
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i] != null && catalog[i].Formula == formula) return catalog[i];
            return null;
        }
    }
}
