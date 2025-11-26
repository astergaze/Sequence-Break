using System.Collections.Generic;

namespace Sequence_Break
{
    // Struct global
    public struct ItemData
    {
        public string Name;
        public string Description;
        public int Damage; // 0 si es consumible/clave
        public int CurrentAmmo; // Cantidad actual
        public int MaxAmmo; // Capacidad maxima (stack)
        public bool IsKeyItem; // Para diferenciar internamente si hace falta
    }

    public static class PlayerStatus
    {
        // --- ESTADISTICAS ---
        public static int CurrentHP = 100;
        public static int MaxHP = 100;

        public static int CurrentSanity = 100;
        public static int MaxSanity = 100;

        // --- INVENTARIO ---
        public static ItemData CurrentWeapon;
        public static List<ItemData> Inventory = new List<ItemData>();
        public static List<ItemData> KeyItems = new List<ItemData>();

        // --- INICIALIZACIÓN (Llamar al inicio del juego) ---
        public static void Initialize()
        {
            CurrentHP = 100;
            MaxHP = 100;
            CurrentSanity = 100;
            MaxSanity = 100;

            // Arma Inicial
            CurrentWeapon = new ItemData
            {
                Name = "Pistola de Paintball",
                Description = "Arma no letal modificada...",
                Damage = 15,
                CurrentAmmo = 12,
                MaxAmmo = 12,
            };

            // Items Iniciales
            Inventory.Clear();
            Inventory.Add(
                new ItemData
                {
                    Name = "Pastillas de cordura",
                    Description = "Sabor menta.",
                    CurrentAmmo = 3,
                    MaxAmmo = 10,
                }
            );
            Inventory.Add(
                new ItemData
                {
                    Name = "Paquete de curitas",
                    Description = "Cura heridas.",
                    CurrentAmmo = 5,
                    MaxAmmo = 10,
                }
            );

            // Objetos Clave Iniciales
            KeyItems.Clear();
            // KeyItems.Add(
            //     new ItemData
            //     {
            //         Name = "Llave de libreria",
            //         Description = "Llave antigua.",
            //         CurrentAmmo = 1,
            //         MaxAmmo = 1,
            //     }
            // );
        }

        // --- MeTODOS DE UTILIDAD ---
        public static void AddItem(ItemData item)
        {
            // TO DO: Logica para stackear items
            Inventory.Add(item);
        }

        public static void ModifyHP(int amount)
        {
            CurrentHP += amount;
            if (CurrentHP > MaxHP)
                CurrentHP = MaxHP;
            if (CurrentHP < 0)
                CurrentHP = 0;
        }

        public static void ModifySanity(int amount)
        {
            CurrentSanity += amount;
            if (CurrentSanity > MaxSanity)
                CurrentSanity = MaxSanity;
            if (CurrentSanity < 0)
                CurrentSanity = 0;
        }
    }
}
