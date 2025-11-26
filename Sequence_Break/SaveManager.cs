using System;
using System.IO;
using System.Text.Json; // Necesario para serializar
using Microsoft.Xna.Framework; // Para Vector2 si lo usas

namespace Sequence_Break
{
    public static class SaveManager
    {
        // Definimos donde se guardara el archivo
        // Se guardará en: C:\Users\TuUsuario\AppData\Roaming\SequenceBreak\savegame.json
        private static string _saveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SequenceBreak"
        );

        private static string _saveFilePath = Path.Combine(_saveFolder, "savegame.json");

        public static void SaveGame()
        {
            // Asegurarnos de que la carpeta exista
            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            // Crear el objeto de datos y llenarlo con la info ACTUAL del juego
            SaveData data = new SaveData
            {
                HP = PlayerStatus.CurrentHP,
                MaxHP = PlayerStatus.MaxHP,
                Sanity = PlayerStatus.CurrentSanity,
                MaxSanity = PlayerStatus.MaxSanity,
                CurrentWeapon = PlayerStatus.CurrentWeapon,
                Inventory = PlayerStatus.Inventory,
                KeyItems = PlayerStatus.KeyItems,
            };

            // Convertir a Texto JSON
            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            string jsonString = JsonSerializer.Serialize(data, options);

            // Escribir en el disco
            File.WriteAllText(_saveFilePath, jsonString);

            Console.WriteLine($"Partida guardada en: {_saveFilePath}");
        }

        public static bool LoadGame()
        {
            // Verificar si existe el archivo
            if (!File.Exists(_saveFilePath))
            {
                Console.WriteLine("No existe archivo de guardado.");
                return false;
            }

            try
            {
                // Leer el texto del archivo
                string jsonString = File.ReadAllText(_saveFilePath);

                // Convertir Texto JSON a Objeto C#
                var options = new JsonSerializerOptions { IncludeFields = true };
                SaveData data = JsonSerializer.Deserialize<SaveData>(jsonString, options);

                // Volcar los datos cargados a la clase estática del juego (PlayerStatus)
                PlayerStatus.CurrentHP = data.HP;
                PlayerStatus.MaxHP = data.MaxHP;
                PlayerStatus.CurrentSanity = data.Sanity;
                PlayerStatus.MaxSanity = data.MaxSanity;
                PlayerStatus.CurrentWeapon = data.CurrentWeapon;
                PlayerStatus.Inventory = data.Inventory;
                PlayerStatus.KeyItems = data.KeyItems;

                Console.WriteLine("Partida cargada exitosamente.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar partida: {ex.Message}");
                // TO DO: Si el archivo esta corrupto, iniciar una partida nueva
                return false;
            }
        }

        public static bool SaveFileExists()
        {
            return File.Exists(_saveFilePath);
        }
    }
}
