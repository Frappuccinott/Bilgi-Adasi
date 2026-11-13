using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BilgiAdasi.Order
{
    /// <summary>Envanterde bir slot: tek maddenin yığını (O₂ × 5). Boşsa Species null.</summary>
    public readonly struct InventorySlot
    {
        public readonly SpeciesData Species;
        public readonly int Count;

        public InventorySlot(SpeciesData species, int count)
        {
            Species = species;
            Count = count;
        }

        public bool IsEmpty => Species == null || Count <= 0;
    }

    /// <summary>
    /// Oyuncunun envanteri (ekranın altında 6 slot) ve kesesi. Oyuncu objesinde durur; etkileşimler
    /// PlayerInteractor.Inventory ile ulaşır. Seçili slot "eldeki" maddedir.
    /// Yeni madde aynı maddenin yığınına, yoksa soldan ilk boş slota girer.
    /// Her değişiklik kayıt verisine yazılır (diske GameManager teslimde ve çıkışta kaydeder).
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int SlotCount = 6;

        [SerializeField] private EconomyData economy;

        private readonly SpeciesData[] species = new SpeciesData[SlotCount];
        private readonly int[] counts = new int[SlotCount];
        private readonly List<InventorySlot> slotBuffer = new List<InventorySlot>(SlotCount);
        private InputAction nextSlot, prevSlot, selectSlot;
        private IInventoryView view;

        public int Selected { get; private set; }
        public int Money { get; private set; }
        public EconomyData Economy => economy;
        public SpeciesData SelectedSpecies => counts[Selected] > 0 ? species[Selected] : null;
        public int SelectedCount => counts[Selected];

        /// <summary>İçerik, seçim ya da para değişti.</summary>
        public event Action Changed;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Awake() => Load();

        private void OnEnable()
        {
            nextSlot = GameInput.FindAction("Player/NextSlot");
            prevSlot = GameInput.FindAction("Player/PrevSlot");
            selectSlot = GameInput.FindAction("Player/SelectSlot");
            nextSlot.performed += OnNext;
            prevSlot.performed += OnPrev;
            selectSlot.performed += OnSelectSlot;
            ViewLocator.Changed += OnViewsChanged;
            view = ViewLocator.Get<IInventoryView>();
            Refresh();
        }

        private void OnDisable()
        {
            if (GameInput.TryGetControls(out _))
            {
                nextSlot.performed -= OnNext;
                prevSlot.performed -= OnPrev;
                selectSlot.performed -= OnSelectSlot;
            }
            ViewLocator.Changed -= OnViewsChanged;
        }

        // ---------- Sorgular ----------

        public InventorySlot Get(int index) => new InventorySlot(species[index], counts[index]);

        public int CountOf(SpeciesData s)
        {
            int n = 0;
            for (int i = 0; i < SlotCount; i++)
                if (species[i] == s) n += counts[i];
            return n;
        }

        /// <summary>Bu maddeden n tane sığar mı (aynı yığın ya da boş slot)?</summary>
        public bool CanAdd(SpeciesData s) => FindStack(s) >= 0 || FindEmpty() >= 0;

        // ---------- Değişiklikler ----------

        /// <summary>Maddeyi aynı yığına ya da soldan ilk boş slota ekler. Yer yoksa false.</summary>
        public bool Add(SpeciesData s, int n)
        {
            if (s == null || n <= 0) return false;
            int i = FindStack(s);
            if (i < 0) i = FindEmpty();
            if (i < 0) return false;
            species[i] = s;
            counts[i] += n;
            Refresh();
            return true;
        }

        /// <summary>Seçili slottan n tane alır. Yetmezse false.</summary>
        public bool RemoveSelected(int n) => RemoveAt(Selected, n);

        public bool RemoveAt(int index, int n)
        {
            if (index < 0 || index >= SlotCount || counts[index] < n || n <= 0) return false;
            counts[index] -= n;
            if (counts[index] == 0) species[index] = null;
            Refresh();
            return true;
        }

        /// <summary>Maddeden toplam n tane alır (birden fazla slottan olabilir). Yetmezse hiçbir şey almaz.</summary>
        public bool Remove(SpeciesData s, int n)
        {
            if (CountOf(s) < n) return false;
            for (int i = 0; i < SlotCount && n > 0; i++)
            {
                if (species[i] != s) continue;
                int take = Mathf.Min(n, counts[i]);
                counts[i] -= take;
                n -= take;
                if (counts[i] == 0) species[i] = null;
            }
            Refresh();
            return true;
        }

        /// <summary>Seçili slottaki yığının tamamını verir ve slotu boşaltır.</summary>
        public InventorySlot TakeSelected()
        {
            var slot = Get(Selected);
            species[Selected] = null;
            counts[Selected] = 0;
            Refresh();
            return slot;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Money < amount) return false;
            Money -= amount;
            Refresh();
            return true;
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            Money += amount;
            Refresh();
        }

        public void Select(int index)
        {
            if (index < 0 || index >= SlotCount || index == Selected) return;
            Selected = index;
            Refresh();
        }

        /// <summary>Kaydı sıfırlayınca (geliştirici) ya da ilk açılışta.</summary>
        public void ResetToStart()
        {
            Array.Clear(species, 0, SlotCount);
            Array.Clear(counts, 0, SlotCount);
            Selected = 0;
            Money = economy != null ? economy.StartingMoney : 0;
            Refresh();
        }

        // ---------- Girdi ----------

        private void OnNext(InputAction.CallbackContext _)
        {
            if (!GameInput.InventoryLocked) Select((Selected + 1) % SlotCount);
        }

        private void OnPrev(InputAction.CallbackContext _)
        {
            if (!GameInput.InventoryLocked) Select((Selected + SlotCount - 1) % SlotCount);
        }

        private void OnSelectSlot(InputAction.CallbackContext ctx)
        {
            if (GameInput.InventoryLocked) return;
            if (ctx.control is KeyControl key)
            {
                int index = key.keyCode - Key.Digit1;
                if (index >= 0 && index < SlotCount) Select(index);
            }
        }

        // ---------- İç ----------

        private int FindStack(SpeciesData s)
        {
            for (int i = 0; i < SlotCount; i++)
                if (species[i] == s && counts[i] > 0) return i;
            return -1;
        }

        private int FindEmpty()
        {
            for (int i = 0; i < SlotCount; i++)
                if (counts[i] <= 0) return i;
            return -1;
        }

        private void Refresh()
        {
            WriteSave();
            if (view != null)
            {
                slotBuffer.Clear();
                for (int i = 0; i < SlotCount; i++) slotBuffer.Add(Get(i));
                view.ShowInventory(slotBuffer, Selected, Money);
            }
            GameEvents.RaiseHandItemChanged(SelectedSpecies != null);
            Changed?.Invoke();
        }

        private void Load()
        {
            var data = Data;
            if (data == null || !data.EconomyStarted)
            {
                Money = economy != null ? economy.StartingMoney : 0;
                if (data != null) data.EconomyStarted = true;
                WriteSave();
                return;
            }

            Money = data.Money;
            var list = data.Inventory;
            for (int i = 0; i < list.Count; i++)
            {
                if (!int.TryParse(list[i].key, out int index) || index < 0 || index >= SlotCount) continue;
                var s = economy != null ? economy.FindSpecies(list[i].species) : null;
                if (s == null || list[i].count <= 0) continue;
                species[index] = s;
                counts[index] = list[i].count;
            }
        }

        private void WriteSave()
        {
            var data = Data;
            if (data == null) return;
            data.Money = Money;
            var list = data.Inventory;
            list.Clear();
            for (int i = 0; i < SlotCount; i++)
                if (counts[i] > 0 && species[i] != null)
                    list.Add(new SaveData.StackEntry { key = SlotKeys[i], species = species[i].Formula, count = counts[i] });
        }

        private static readonly string[] SlotKeys = { "0", "1", "2", "3", "4", "5" };

        private void OnViewsChanged(Type type)
        {
            if (type != typeof(IInventoryView)) return;
            view = ViewLocator.Get<IInventoryView>();
            Refresh();
        }
    }
}
