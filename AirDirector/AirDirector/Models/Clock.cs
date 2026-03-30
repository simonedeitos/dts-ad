using System;
using System.Collections.Generic;
using System.Linq;

namespace AirDirector.Models
{
    /// <summary>
    /// Clock: sequenza di categorie/generi per generazione playlist
    /// </summary>
    public class Clock
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public List<ClockItem> Items { get; set; }

        public Clock()
        {
            ID = 0;
            Name = string.Empty;
            IsDefault = false;
            Items = new List<ClockItem>();
        }

        public Clock(int id, string name, bool isDefault = false)
        {
            ID = id;
            Name = name;
            IsDefault = isDefault;
            Items = new List<ClockItem>();
        }

        /// <summary>
        /// Aggiunge un item al clock (metodo legacy)
        /// </summary>
        public void AddItem(string categoryName, int count = 1)
        {
            Items.Add(new ClockItem(categoryName, count));
        }

        /// <summary>
        /// Aggiunge un item completo al clock
        /// </summary>
        public void AddItem(ClockItem item)
        {
            if (item != null)
            {
                Items.Add(item);
            }
        }

        /// <summary>
        /// Rimuove un item dal clock
        /// </summary>
        public void RemoveItem(ClockItem item)
        {
            Items.Remove(item);
        }

        /// <summary>
        /// Rimuove un item per indice
        /// </summary>
        public void RemoveItemAt(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items.RemoveAt(index);
            }
        }

        /// <summary>
        /// Sposta un item verso l'alto
        /// </summary>
        public void MoveItemUp(ClockItem item)
        {
            int index = Items.IndexOf(item);
            if (index > 0)
            {
                var temp = Items[index - 1];
                Items[index - 1] = Items[index];
                Items[index] = temp;
            }
        }

        /// <summary>
        /// Sposta un item verso l'alto per indice
        /// </summary>
        public void MoveItemUp(int index)
        {
            if (index > 0 && index < Items.Count)
            {
                var temp = Items[index - 1];
                Items[index - 1] = Items[index];
                Items[index] = temp;
            }
        }

        /// <summary>
        /// Sposta un item verso il basso
        /// </summary>
        public void MoveItemDown(ClockItem item)
        {
            int index = Items.IndexOf(item);
            if (index >= 0 && index < Items.Count - 1)
            {
                var temp = Items[index + 1];
                Items[index + 1] = Items[index];
                Items[index] = temp;
            }
        }

        /// <summary>
        /// Sposta un item verso il basso per indice
        /// </summary>
        public void MoveItemDown(int index)
        {
            if (index >= 0 && index < Items.Count - 1)
            {
                var temp = Items[index + 1];
                Items[index + 1] = Items[index];
                Items[index] = temp;
            }
        }

        /// <summary>
        /// Ottiene il numero totale di elementi che il clock genera
        /// </summary>
        public int GetTotalItemCount()
        {
            return Items.Sum(i => i.Count);
        }

        /// <summary>
        /// Ottiene descrizione del clock
        /// </summary>
        public string GetDescription()
        {
            if (Items.Count == 0)
                return "Clock vuoto";

            int totalItems = GetTotalItemCount();
            return $"{Items.Count} elementi, {totalItems} brani totali";
        }

        /// <summary>
        /// Ottiene descrizione dettagliata degli items
        /// </summary>
        public string GetDetailedDescription()
        {
            if (Items.Count == 0)
                return "Nessun elemento";

            var parts = new List<string>();
            foreach (var item in Items)
            {
                string icon = item.Type == "Category" ? "📁" : "🎵";
                string filter = item.YearFilterEnabled ? $" [{item.YearFrom}-{item.YearTo}]" : "";
                parts.Add($"{icon} {item.Value}{filter}");
            }

            return string.Join(" → ", parts);
        }

        /// <summary>
        /// Verifica se il clock contiene elementi
        /// </summary>
        public bool IsEmpty()
        {
            return Items == null || Items.Count == 0;
        }

        /// <summary>
        /// Clona il clock
        /// </summary>
        public Clock Clone()
        {
            var clone = new Clock(this.ID, this.Name, this.IsDefault);

            foreach (var item in this.Items)
            {
                clone.Items.Add(new ClockItem
                {
                    Type = item.Type,
                    CategoryName = item.CategoryName,
                    Count = item.Count,
                    YearFilterEnabled = item.YearFilterEnabled,
                    YearFrom = item.YearFrom,
                    YearTo = item.YearTo
                });
            }

            return clone;
        }

        public override string ToString()
        {
            string defaultMarker = IsDefault ? " ⭐" : "";
            return $"{Name}{defaultMarker}";
        }

        /// <summary>
        /// Rappresentazione completa per debug
        /// </summary>
        public string ToDebugString()
        {
            return $"Clock #{ID}: {Name} (Default: {IsDefault}, Items: {Items.Count})";
        }
    }
}