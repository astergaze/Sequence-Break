using System;
using System.Collections.Generic;

namespace Sequence_Break
{
    // Struct global para los datos del Item
    public struct ItemData
    {
        public string Name;
        public string Description;
        public int Damage; // Usado como "Valor del Efecto" para consumibles (ej: 20 curacion)
        public int CurrentAmmo; // Usado como "Cantidad" en el inventario
        public int MaxAmmo; // Limite de stack
        public bool IsKeyItem;
    }

    public static class PlayerStatus
    {
        // --- ESTADISTICAS ---
        public static int CurrentHP = 100;
        public static int MaxHP = 100;

        public static int CurrentSanity = 100;
        public static int MaxSanity = 100;
        public static int Perception = 5;

        // --- INVENTARIO ---
        public static ItemData CurrentWeapon;
        public static List<ItemData> Inventory = new List<ItemData>();
        public static List<ItemData> KeyItems = new List<ItemData>();

        // --- SISTEMA DE REGISTRO DE INTERACCIONES (OBJETOS) ---
        private static HashSet<string> _interactedObjectNames = new HashSet<string>();

        public static bool HasInteracted(string objectName)
        {
            return _interactedObjectNames.Contains(objectName);
        }

        public static void RegisterInteraction(string objectName)
        {
            if (!_interactedObjectNames.Contains(objectName))
            {
                _interactedObjectNames.Add(objectName);
            }
        }

        // --- SISTEMA DE MEMORIA DE ENEMIGOS (NUEVO) ---
        // Guarda IDs unicos en formato "Mapa_ID" para que no respawneen
        private static HashSet<string> _defeatedEnemies = new HashSet<string>();

        public static void MarkEnemyAsDefeated(string mapName, int enemyId)
        {
            string uniqueKey = $"{mapName}_{enemyId}";
            if (!_defeatedEnemies.Contains(uniqueKey))
            {
                _defeatedEnemies.Add(uniqueKey);
            }
        }

        public static bool IsEnemyDefeated(string mapName, int enemyId)
        {
            string uniqueKey = $"{mapName}_{enemyId}";
            return _defeatedEnemies.Contains(uniqueKey);
        }

        // ----------------------------------------------

        // --- INICIALIZACION ---
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
            AddItem("Pastillas de cordura", "Sabor menta.", 3, 20, "Sanity");
            AddItem("Paquete de curitas", "Cura heridas.", 5, 30, "Heal");

            KeyItems.Clear();

            // Limpiar registros al iniciar juego nuevo
            _interactedObjectNames.Clear();
            _defeatedEnemies.Clear(); // <--- Limpiamos los enemigos muertos
        }

        // --- MODIFICADORES DE ESTADISTICAS ---
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

        // --- SISTEMA DE INVENTARIO: AGREGAR ---

        public static void AddItem(ItemData item)
        {
            AddItem(item.Name, item.Description, item.CurrentAmmo, item.Damage, "");
        }

        public static void AddKeyItem(string name, string description)
        {
            // Verificamos si ya lo tiene para no duplicar
            if (KeyItems.Exists(k => k.Name == name))
                return;

            ItemData keyItem = new ItemData
            {
                Name = name,
                Description = description,
                CurrentAmmo = 1,
                MaxAmmo = 1,
                Damage = 0, // No hace daño ni cura
                IsKeyItem = true,
            };

            KeyItems.Add(keyItem);
        }

        public static void AddItem(
            string name,
            string description,
            int quantity,
            int effectValue,
            string type
        )
        {
            int index = Inventory.FindIndex(i => i.Name == name);

            if (index != -1)
            {
                ItemData existing = Inventory[index];
                existing.CurrentAmmo += quantity;
                if (existing.CurrentAmmo > existing.MaxAmmo)
                    existing.CurrentAmmo = existing.MaxAmmo;
                Inventory[index] = existing;
            }
            else
            {
                ItemData newItem = new ItemData
                {
                    Name = name,
                    Description = description,
                    CurrentAmmo = quantity,
                    MaxAmmo = 99,
                    Damage = effectValue,
                    IsKeyItem = false,
                };
                Inventory.Add(newItem);
            }
        }

        // --- SISTEMA DE INVENTARIO: USAR ---
        public static string UseItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= Inventory.Count)
                return "Error de seleccion.";

            ItemData item = Inventory[inventoryIndex];
            bool used = false;
            string message = "";

            if (IsHealingItem(item.Name))
            {
                if (CurrentHP < MaxHP)
                {
                    int healAmount = item.Damage;
                    if (healAmount == 0)
                        healAmount = 10;

                    ModifyHP(healAmount);
                    message = $"Usaste {item.Name}. Recuperas {healAmount} HP.";
                    used = true;
                }
                else
                    return "Tu salud ya esta al maximo.";
            }
            else if (IsSanityItem(item.Name))
            {
                if (CurrentSanity < MaxSanity)
                {
                    int sanityAmount = item.Damage;
                    if (sanityAmount == 0)
                        sanityAmount = 20;

                    ModifySanity(sanityAmount);
                    message = $"Usaste {item.Name}. Recuperas {sanityAmount} Cordura.";
                    used = true;
                }
                else
                    return "Tu mente esta clara.";
            }
            else
                return $"No puedes usar {item.Name} en este momento.";

            if (used)
                RemoveItemQuantity(inventoryIndex, 1);

            return message;
        }

        // --- SISTEMA DE INVENTARIO: TIRAR ---
        public static string DropItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= Inventory.Count)
                return "";

            ItemData item = Inventory[inventoryIndex];
            RemoveItemQuantity(inventoryIndex, 1);
            return $"Tiraste 1x {item.Name}.";
        }

        // --- UTILIDADES INTERNAS ---

        private static void RemoveItemQuantity(int index, int amount)
        {
            ItemData item = Inventory[index];
            item.CurrentAmmo -= amount;

            if (item.CurrentAmmo <= 0)
                Inventory.RemoveAt(index);
            else
                Inventory[index] = item;
        }

        private static bool IsHealingItem(string name)
        {
            return name == "Paquete de curitas"
                || name == "Manzanas"
                || name == "Venda"
                || name == "Botiquin";
        }

        private static bool IsSanityItem(string name)
        {
            return name == "Pastillas de cordura"
                || name == "Sedante"
                || name == "Chocolate"
                || name == "Cafe";
        }
    }
}
