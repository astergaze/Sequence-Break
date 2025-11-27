using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework; // Necesario para MathHelper

namespace Sequence_Break
{
    public static class SettingsManager
    {
        private const string SettingsFileName = "settings.json";

        public static SettingsData Data { get; private set; }

        static SettingsManager()
        {
            LoadSettings();
        }

        // --- NUEVO CÓDIGO AÑADIDO ---
        // Esta es la propiedad que tu método PlaySfx está intentando leer.
        // Convierte el valor int (0 a 10) a float (0.0f a 1.0f)
        public static float SFXVolume
        {
            get
            {
                // MathHelper.Clamp evita errores si alguien edita el JSON y pone un valor como 500
                // MonoGame crashea si el volumen es mayor a 1.0f
                float clampedVolume = MathHelper.Clamp(Data.SfxVolume, 0, 10);
                return clampedVolume / 10f;
            }
        }

        // -----------------------------

        public static void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFileName);
                    Data = JsonSerializer.Deserialize<SettingsData>(json);
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine(
                        $"Error al cargar settings: {ex.Message}. Usando defaults."
                    );
                    Data = new SettingsData();
                }
            }
            else
            {
                Data = new SettingsData();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    Data,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(SettingsFileName, json);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al guardar settings: {ex.Message}");
            }
        }

        public static void ApplyMusicVolume()
        {
            // Usamos MathHelper aquí también por seguridad
            float clampedVolume = MathHelper.Clamp(Data.MusicVolume, 0, 10);
            Microsoft.Xna.Framework.Media.MediaPlayer.Volume = clampedVolume / 10f;
        }
    }
}
