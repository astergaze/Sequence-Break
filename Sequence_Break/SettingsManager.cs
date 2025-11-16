using System.IO;
using System.Text.Json;

namespace Sequence_Break
{
    public static class SettingsManager
    {
        private const string SettingsFileName = "settings.json";

        public static SettingsData Data { get; private set; }

        // El constructor estatico se llama solo la primera vez que se usa la clase
        static SettingsManager()
        {
            LoadSettings();
        }

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
                    Data = new SettingsData(); // Carga fallida
                }
            }
            else
            {
                Data = new SettingsData(); // No existe, usa defaults
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

        // Metodo para aplicar el volumen
        public static void ApplyMusicVolume()
        {
            Microsoft.Xna.Framework.Media.MediaPlayer.Volume = Data.MusicVolume / 10f;
        }
    }
}
