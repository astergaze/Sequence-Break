using System.Collections.Generic;

namespace Sequence_Break
{
    // Esta clase es solo un contenedor de datos para guardar en el archivo
    public class SaveData
    {
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int Sanity { get; set; }
        public int MaxSanity { get; set; }

        // Guardamos las listas de items
        public ItemData CurrentWeapon { get; set; }
        public List<ItemData> Inventory { get; set; } = new List<ItemData>();
        public List<ItemData> KeyItems { get; set; } = new List<ItemData>();

        // TO DO: Agregar cosas como las siguientes:
        // public string CurrentLevelName { get; set; }
        // public Vector2 PlayerPosition { get; set; }
    }
}
