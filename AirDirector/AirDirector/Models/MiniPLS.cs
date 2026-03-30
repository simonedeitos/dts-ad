using System.Collections.Generic;
using System.Linq;

namespace AirDirector.Models
{
    /// <summary>
    /// Sequenza predefinita di brani (MiniPLS)
    /// </summary>
    public class MiniPLS
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<MiniPLSItem> Items { get; set; }

        public MiniPLS()
        {
            ID = 0;
            Name = string.Empty;
            Description = string.Empty;
            Items = new List<MiniPLSItem>();
        }

        public MiniPLS(int id, string name, string description)
        {
            ID = id;
            Name = name;
            Description = description;
            Items = new List<MiniPLSItem>();
        }

        /// <summary>
        /// Aggiunge un item alla sequenza
        /// </summary>
        public void AddItem(string filePath)
        {
            int nextOrder = Items.Count > 0 ? Items.Max(i => i.Order) + 1 : 1;
            Items.Add(new MiniPLSItem(filePath, nextOrder));
        }

        /// <summary>
        /// Rimuove un item dalla sequenza
        /// </summary>
        public void RemoveItem(MiniPLSItem item)
        {
            Items.Remove(item);
            ReorderItems();
        }

        /// <summary>
        /// Sposta un item verso l'alto
        /// </summary>
        public void MoveItemUp(MiniPLSItem item)
        {
            int index = Items.IndexOf(item);
            if (index > 0)
            {
                Items[index] = Items[index - 1];
                Items[index - 1] = item;
                ReorderItems();
            }
        }

        /// <summary>
        /// Sposta un item verso il basso
        /// </summary>
        public void MoveItemDown(MiniPLSItem item)
        {
            int index = Items.IndexOf(item);
            if (index < Items.Count - 1)
            {
                Items[index] = Items[index + 1];
                Items[index + 1] = item;
                ReorderItems();
            }
        }

        /// <summary>
        /// Riordina gli items dopo modifiche
        /// </summary>
        private void ReorderItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].Order = i + 1;
            }
        }

        /// <summary>
        /// Ottiene il numero di items
        /// </summary>
        public int GetItemCount()
        {
            return Items.Count;
        }

        public override string ToString()
        {
            return $"{Name} ({Items.Count} items)";
        }
    }
}