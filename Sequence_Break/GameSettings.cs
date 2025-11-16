namespace Sequence_Break
{
    // guarda la configuracion global del juego para que sea accesible desde cualquier lugar
    public static class GameSettings
    {
        // Volumen de la musica (0-10).
        public static int MusicVolume { get; set; } = 8;

        // Volumen de los efectos de sonido (0-10).
        public static int SfxVolume { get; set; } = 7;

        // Estado de la pantalla (true = Completa, false = Ventana).
        public static bool IsFullscreen { get; set; } = true;
    }
}
