using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media; // NECESARIO PARA LA MUSICA
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using TiledSharp;

namespace Sequence_Break
{
    public class CaseScreen : Screen
    {
        // Estructura de Interaccion
        public struct InteractableObject
        {
            public string Name;
            public Rectangle TriggerZone;
            public string TargetMap;
            public string TargetSpawn;
            public string Message;
        }

        // Variables de Specter
        private AnimatedSprite _specterWalkFront;
        private AnimatedSprite _specterWalkLeft;
        private AnimatedSprite _specterWalkRight;
        private AnimatedSprite _specterWalkBack;
        private AnimatedSprite _specterCurrent;
        private Vector2 _specterPosition;
        private bool _isMoving;

        // --- VARIABLES DEL JEFE ---
        private AnimatedSprite _bossIdle;
        private Vector2 _bossPosition;
        private bool _bossActive;

        // --- VARIABLES DE ENEMIGOS COMUNES ---
        private List<Enemy> _enemies;

        // Los 3 atlas necesarios para la animacion direccional
        private TextureAtlas _enemyAtlasBack;
        private TextureAtlas _enemyAtlasFront;
        private TextureAtlas _enemyAtlasSide;

        // -------------------------------------

        // --- MUSICA (NUEVO) ---
        private Song _backgroundMusic;

        // Variables de Control
        private const float MOVEMENT_SPEED = 5.0f;
        private KeyboardState _previousKeyboardState;
        private const float PLAYER_SCALE = 3.0f;
        private const int PLAYER_BASE_WIDTH = 22;
        private const int PLAYER_BASE_HEIGHT = 40;
        private const float PLAYER_REFERENCE_WIDTH = PLAYER_BASE_WIDTH * PLAYER_SCALE;
        private const float PLAYER_REFERENCE_HEIGHT = PLAYER_BASE_HEIGHT * PLAYER_SCALE;

        // Variables del Nivel (Tiled)
        private TiledMapRenderer _mapRenderer;
        private List<Rectangle> _collisionBarriers;
        private List<InteractableObject> _interactableObjects;

        // Variables de estado
        private string _currentMapName;
        private string _initialMap;
        private Vector2 _initialSpawnPoint;

        // Textura para Debug
        private Texture2D _pixelTexture;

        // UI
        private InteractionPanel _interactionPanel;
        private SpriteFont _uiFont;
        private TextureAtlas _uiAtlas;
        private string _currentInteractionName = string.Empty;

        // Camara
        private Matrix _cameraTransform;

        // Variable del menu
        private PauseMenu _pauseMenu;

        // Variable del inventario
        private InventoryMenu _inventoryMenu;

        public CaseScreen(Game1 game)
            : base(game)
        {
            _initialMap = "Lobby";
            _initialSpawnPoint = new Vector2(600, 750);
        }

        public CaseScreen(Game1 game, string mapToLoad, Vector2 positionToSpawn)
            : base(game)
        {
            _initialMap = mapToLoad;
            _initialSpawnPoint = positionToSpawn;
        }

        public override void LoadContent()
        {
            // --- CARGA DE SPECTER ---
            TextureAtlas atlasFront = TextureAtlas.FromFile(
                Content,
                "textures/Specter-front-atlas-definition.xml"
            );
            _specterWalkFront = atlasFront.CreateAnimatedSprite("luka-walk-front");
            _specterWalkFront.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            TextureAtlas atlasBack = TextureAtlas.FromFile(
                Content,
                "textures/Specter-back-atlas-definition.xml"
            );
            _specterWalkBack = atlasBack.CreateAnimatedSprite("luka-walk-back");
            _specterWalkBack.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            TextureAtlas atlasLeft = TextureAtlas.FromFile(
                Content,
                "textures/Specter-left-atlas-definition.xml"
            );
            _specterWalkLeft = atlasLeft.CreateAnimatedSprite("luka-walk-left");
            _specterWalkLeft.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            TextureAtlas atlasRight = TextureAtlas.FromFile(
                Content,
                "textures/Specter-right-atlas-definition.xml"
            );
            _specterWalkRight = atlasRight.CreateAnimatedSprite("luka-walk-right");
            _specterWalkRight.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            _specterCurrent = _specterWalkFront;

            // --- CARGA DE JEFE Y ENEMIGOS ---
            try
            {
                // 1. Carga del Jefe
                TextureAtlas bossAtlas = TextureAtlas.FromFile(
                    Content,
                    "textures/enemies/demo/enemy-2-atlas-definition.xml"
                );
                _bossIdle = bossAtlas.CreateAnimatedSprite("enemy-idle");
                _bossIdle.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

                // 2. Carga de Enemigos Comunes (3 Atlas distintos)
                _enemyAtlasBack = TextureAtlas.FromFile(
                    Content,
                    "textures/enemies/demo/enemy-1-atlas-w-b.xml"
                );
                _enemyAtlasFront = TextureAtlas.FromFile(
                    Content,
                    "textures/enemies/demo/enemy-1-atlas-w-f.xml"
                );
                _enemyAtlasSide = TextureAtlas.FromFile(
                    Content,
                    "textures/enemies/demo/enemy-1-atlas-w-side.xml"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando assets de enemigos: {ex.Message}");
                _bossIdle = null;
                // Si falla, dejamos los atlas en null
            }

            _collisionBarriers = new List<Rectangle>();
            _interactableObjects = new List<InteractableObject>();
            _enemies = new List<Enemy>();

            LoadMap(_initialMap);

            _specterPosition = _initialSpawnPoint;

            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // --- CARGAR MUSICA ---
            try
            {
                // CAMBIA "audio/CaseTheme" POR EL NOMBRE DE TU ARCHIVO DE MUSICA
                _backgroundMusic = Content.Load<Song>("audio/mapSong");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la musica: {ex.Message}");
                _backgroundMusic = null;
            }
            // ---------------------

            try
            {
                _uiFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");
                _uiAtlas = TextureAtlas.FromFile(
                    Content,
                    "Interface/Combat/interface-combat-atlas-definition.xml"
                );
            }
            catch
            {
                throw;
            }

            _interactionPanel = new InteractionPanel(_uiFont, _uiAtlas, GraphicsDevice);
            _interactionPanel.OnOptionSelected += HandleInteractionChoice;

            _previousKeyboardState = Keyboard.GetState();
            _pauseMenu = new PauseMenu(_game, this, _uiFont, GraphicsDevice);
            _inventoryMenu = new InventoryMenu(_game, _uiFont, GraphicsDevice);
        }

        private void LoadMap(string mapName)
        {
            _currentMapName = mapName;
            string mapFileSystemPath = Path.Combine(
                AppContext.BaseDirectory,
                $"Content/maps/demo/{mapName}.tmx"
            );
            string tilesetContentFolder = "maps/demo/textures";

            _mapRenderer = new TiledMapRenderer(Content, mapFileSystemPath, tilesetContentFolder);

            _collisionBarriers = _mapRenderer.GetCollisionRectangles();
            _interactableObjects = _mapRenderer.GetInteractableObjects();

            // --- LOGICA SPAWN DEL JEFE ---
            _bossActive = false;
            foreach (var obj in _interactableObjects)
            {
                if (obj.Name == "BossSpawn")
                {
                    _bossPosition = new Vector2(obj.TriggerZone.X, obj.TriggerZone.Y);
                    _bossActive = true;
                    if (_bossIdle != null)
                    {
                        float spriteHeight = 42 * PLAYER_SCALE;
                        _bossPosition.Y -= spriteHeight;
                    }
                }
            }

            // --- LOGICA SPAWN ENEMIGOS COMUNES ---
            _enemies.Clear();

            // Obtenemos la configuracion de la capa "Enemies"
            var enemiesData = _mapRenderer.GetEnemiesConfiguration("Enemies");

            // Verificamos que los 3 atlas se hayan cargado correctamente
            if (_enemyAtlasBack != null && _enemyAtlasFront != null && _enemyAtlasSide != null)
            {
                foreach (var data in enemiesData)
                {
                    // *** PERSISTENCIA ***
                    // Si el enemigo con este ID ya esta en la lista de muertos, NO lo creamos
                    if (PlayerStatus.IsEnemyDefeated(_currentMapName, data.Id))
                    {
                        continue;
                    }

                    // Creamos los 3 sprites necesarios para CADA enemigo
                    AnimatedSprite sBack = _enemyAtlasBack.CreateAnimatedSprite("enemy-walk-bw");
                    sBack.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

                    AnimatedSprite sFront = _enemyAtlasFront.CreateAnimatedSprite("enemy-walk-fw");
                    sFront.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

                    AnimatedSprite sSide = _enemyAtlasSide.CreateAnimatedSprite("enemy-walk-side");
                    sSide.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

                    // Instanciamos el enemigo pasandole el ID y los 3 sprites
                    Enemy newEnemy = new Enemy(
                        data.Id, // ID Unico de Tiled
                        sBack,
                        sFront,
                        sSide,
                        data.Path,
                        data.Speed,
                        data.VisionRange
                    );
                    _enemies.Add(newEnemy);
                }
            }
            Console.WriteLine($"DEBUG: Se cargaron {_enemies.Count} enemigos vivos en patrulla.");
        }

        private Rectangle GetPlayerBox(Vector2 position)
        {
            float spriteWidth = PLAYER_REFERENCE_WIDTH;
            float spriteHeight = PLAYER_REFERENCE_HEIGHT;
            int boxWidth = (int)(spriteWidth * 0.7f);
            int boxHeight = (int)(spriteHeight * 0.9f);
            int boxX = (int)(position.X + (spriteWidth * 0.15f));
            int boxY = (int)(position.Y + (spriteHeight * 0.1f));
            return new Rectangle(boxX, boxY, boxWidth, boxHeight);
        }
private Rectangle GetBossBodyBox(Vector2 position)
{
    // Las dimensiones del Boss (enemy-2) según el LoadMap
    // Ancho: 36 * PLAYER_SCALE (108) | Alto: 42 * PLAYER_SCALE (126)
    
    float spriteWidth = 36 * PLAYER_SCALE;
    float spriteHeight = 42 * PLAYER_SCALE;
    
    // Hitbox precisa para que el jugador no pase
    int boxWidth = (int)(spriteWidth * 0.8f);
    int boxHeight = (int)(spriteHeight * 0.8f);
    
    // Posiciona la caja en el centro inferior del sprite
    int boxX = (int)(position.X + (spriteWidth * 0.1f));
    int boxY = (int)(position.Y + (spriteHeight * 0.2f));
    
    return new Rectangle(boxX, boxY, boxWidth, boxHeight);
}

// En la clase CaseScreen.cs

private bool HasCollision(Rectangle playerBox)
{
    // --- NUEVA LÓGICA DE COLISIÓN DEL JEFE ---
    if (_bossActive)
    {
        // Usamos la posición ajustada en LoadMap: _bossPosition.Y ya fue ajustada.
        if (playerBox.Intersects(GetBossBodyBox(_bossPosition)))
            return true;
    }
    // -----------------------------------------

    foreach (Rectangle barrier in _collisionBarriers)
    {
        if (playerBox.Intersects(barrier))
            return true;
    }
    return false;
}
        // En la clase CaseScreen.cs (Métodos auxiliares)



        private void CheckForInteraction()
        {
            Rectangle playerBox = GetPlayerBox(_specterPosition);

            if (_bossActive)
            {
                int bossW = (int)(36 * PLAYER_SCALE);
                int bossH = (int)(42 * PLAYER_SCALE);
                Rectangle bossRect = new Rectangle(
                    (int)_bossPosition.X - 20,
                    (int)_bossPosition.Y - 20,
                    bossW + 40,
                    bossH + 40
                );

                if (playerBox.Intersects(bossRect))
                {
                    _game.IsMouseVisible = false;
                    // El combate de jefe puede usar un ID especial como -1 o 9999
                    _game.ChangeScreen(
                        new CombatScreen(
                            _game,
                            _currentMapName,
                            _specterPosition,
                            enemyId: -1,
                            enemyType: "Boss"
                        )
                    );
                    return;
                }
            }

            for (int i = 0; i < _interactableObjects.Count; i++)
            {
                if (_interactableObjects[i].Name == "BossSpawn")
                    continue;
                if (playerBox.Intersects(_interactableObjects[i].TriggerZone))
                {
                    PerformInteraction(i);
                    break;
                }
            }
        }

        private void PerformInteraction(int index)
        {
            InteractableObject interactable = _interactableObjects[index];
            _currentInteractionName = interactable.Name;

            if (interactable.TargetMap != null && interactable.TargetSpawn != null)
            {
                if (interactable.Name == "DoorBossRoom")
                {
                    bool hasKey = PlayerStatus.KeyItems.Exists(item =>
                        item.Name == "Llave de la sala de experimentacion"
                    );
                    if (!hasKey)
                    {
                        _interactionPanel.Show(
                            "La puerta esta cerrada. Necesitas la llave de experimentacion.",
                            null,
                            "Puerta"
                        );
                        return;
                    }
                }

                LoadMap(interactable.TargetMap);
                _specterPosition = _mapRenderer.GetSpawnPoint(interactable.TargetSpawn);
                _cameraTransform = Matrix.CreateTranslation(
                    -_specterPosition.X + (GraphicsDevice.Viewport.Width / 2),
                    -_specterPosition.Y + (GraphicsDevice.Viewport.Height / 2),
                    0
                );
                return;
            }

            bool alreadyInteracted = PlayerStatus.HasInteracted(interactable.Name);

            // --- OBJETOS HARDCODEADOS ---
            switch (interactable.Name)
            {
                case "FileCabinet":
                    _interactionPanel.Show(
                        "Un monton de archivos, ninguno que me interese.",
                        null,
                        "Archivador"
                    );
                    break;
                case "DeskDrawer":
                    _interactionPanel.Show("Esta cerrado con llave.", null, "Cajon");
                    break;
                case "WaitingChairs":
                    _interactionPanel.Show(
                        "Sillas para una sala de espera...",
                        null,
                        "Sala de Espera"
                    );
                    break;
                case "DeskWithPapers":
                    _interactionPanel.Show("Un escritorio lleno de papeles...", null, null);
                    break;
                case "DeskPapers":
                    _interactionPanel.Show("Mas papeles.", null, null);
                    break;
                case "Desk":
                    _interactionPanel.Show("Un escritorio.", null, null);
                    break;
                case "PapersOnDesk":
                    _interactionPanel.Show("Mas papeles en el escritorio...", null, "Notas");
                    break;
                case "NotImportantComputer":
                    _interactionPanel.Show("No enciende.", null, "PC");
                    break;
                case "ExpBed":
                    _interactionPanel.Show("Correas y manchas secas.", null, "Camilla");
                    break;
                case "VitalsMonitor":
                    _interactionPanel.Show("Linea plana continua...", null, "Monitor");
                    break;
                case "ContainedExperiment":
                    _interactionPanel.Show("Algo flota en el liquido verde.", null, "Contenedor");
                    break;

                case "ImportantComputer":
                    if (!alreadyInteracted)
                    {
                        PlayerStatus.AddKeyItem(
                            "Llave de la sala de experimentacion",
                            "Permite entrar a la sala de experimentacion"
                        );
                        _interactionPanel.Show(
                            "Encontraste la Llave de la sala de experimentacion!",
                            null,
                            "Sistema"
                        );
                        PlayerStatus.RegisterInteraction(interactable.Name);
                    }
                    else
                        _interactionPanel.Show("La computadora sigue encendida...", null, "PC");
                    break;

                case "Chest_Curas":
                    if (!alreadyInteracted)
                    {
                        PlayerStatus.AddItem("Paquete de curitas", "Cura 30 HP.", 1, 30, "Heal");
                        _interactionPanel.Show("Encontraste suministros medicos!", null, "Sistema");
                        PlayerStatus.RegisterInteraction(interactable.Name);
                    }
                    else
                        _interactionPanel.Show("El botiquin esta vacio.", null, null);
                    break;

                case "Chest_Cordura":
                    if (!alreadyInteracted)
                    {
                        PlayerStatus.AddItem(
                            "Pastillas de cordura",
                            "Calma la mente.",
                            2,
                            20,
                            "Sanity"
                        );
                        _interactionPanel.Show("Encontraste medicacion.", null, "Sistema");
                        PlayerStatus.RegisterInteraction(interactable.Name);
                    }
                    else
                        _interactionPanel.Show("Solo queda polvo.", null, null);
                    break;

                case "ExperimentationTools":
                    if (!alreadyInteracted)
                    {
                        PlayerStatus.AddItem(
                            "Elixir Experimental",
                            "Aumenta percepcion.",
                            1,
                            0,
                            "PowerUp"
                        );
                        _interactionPanel.Show("Recogiste herramientas extranas.", null, "Sistema");
                        PlayerStatus.RegisterInteraction(interactable.Name);
                    }
                    else
                        _interactionPanel.Show("Ya tomaste las muestras.", null, null);
                    break;

                default:
                    if (!string.IsNullOrEmpty(interactable.Message))
                        _interactionPanel.Show(interactable.Message, null, null);
                    break;
            }
        }

        private void HandleInteractionChoice(int optionIndex)
        {
            _currentInteractionName = string.Empty;
        }

        public override void Update(GameTime gameTime)
        {
            // --- LOGICA DE MUSICA (NUEVO) ---
            if (_backgroundMusic != null && MediaPlayer.Queue.ActiveSong != _backgroundMusic)
            {
                MediaPlayer.Play(_backgroundMusic);
                MediaPlayer.IsRepeating = true;
                SettingsManager.ApplyMusicVolume();
            }
            // -------------------------------

            _pauseMenu.Update(gameTime);
            if (_pauseMenu.IsActive)
                return;

            if (_bossActive && _bossIdle != null)
                _bossIdle.Update(gameTime);

            foreach (var enemy in _enemies)
            {
                enemy.Update(gameTime, _specterPosition);

                // Check Colision Combate (Automatico)
                if (GetPlayerBox(_specterPosition).Intersects(enemy.BoundingBox))
                {
                    Console.WriteLine($"DEBUG: Combate iniciado con enemigo ID {enemy.Id}");
                    _game.IsMouseVisible = false;

                    // CAMBIO: Pasamos el tipo "Common"
                    _game.ChangeScreen(
                        new CombatScreen(
                            _game,
                            _currentMapName,
                            _specterPosition,
                            enemy.Id,
                            enemyType: "Common"
                        )
                    );
                    return;
                }
            }

            KeyboardState currentKeyboardState = Keyboard.GetState();
            if (
                (
                    currentKeyboardState.IsKeyDown(Keys.Tab)
                    && !_previousKeyboardState.IsKeyDown(Keys.Tab)
                )
                || (
                    currentKeyboardState.IsKeyDown(Keys.I)
                    && !_previousKeyboardState.IsKeyDown(Keys.I)
                )
            )
            {
                _inventoryMenu.Toggle();
            }

            if (_inventoryMenu.IsActive)
            {
                _inventoryMenu.Update(gameTime);
                _previousKeyboardState = currentKeyboardState;
                return;
            }

            if (
                currentKeyboardState.IsKeyDown(Keys.Escape)
                && !_previousKeyboardState.IsKeyDown(Keys.Escape)
            )
                _pauseMenu.Show();
            if (
                currentKeyboardState.IsKeyDown(Keys.F11)
                && !_previousKeyboardState.IsKeyDown(Keys.F11)
            )
            {
                Core.Graphics.ToggleFullScreen();
                Core.Graphics.ApplyChanges();
            }

            if (_interactionPanel.IsActive)
            {
                _interactionPanel.Update(gameTime);
            }
            else
            {
                _isMoving = false;
                Vector2 movement = Vector2.Zero;
                if (
                    currentKeyboardState.IsKeyDown(Keys.W)
                    || currentKeyboardState.IsKeyDown(Keys.Up)
                )
                    movement.Y = -1;
                if (
                    currentKeyboardState.IsKeyDown(Keys.S)
                    || currentKeyboardState.IsKeyDown(Keys.Down)
                )
                    movement.Y = 1;
                if (
                    currentKeyboardState.IsKeyDown(Keys.A)
                    || currentKeyboardState.IsKeyDown(Keys.Left)
                )
                    movement.X = -1;
                if (
                    currentKeyboardState.IsKeyDown(Keys.D)
                    || currentKeyboardState.IsKeyDown(Keys.Right)
                )
                    movement.X = 1;

                if (movement != Vector2.Zero)
                {
                    _isMoving = true;
                    if (movement.X < 0)
                        _specterCurrent = _specterWalkLeft;
                    else if (movement.X > 0)
                        _specterCurrent = _specterWalkRight;
                    else if (movement.Y < 0)
                        _specterCurrent = _specterWalkBack;
                    else if (movement.Y > 0)
                        _specterCurrent = _specterWalkFront;

                    movement.Normalize();
                    movement *= MOVEMENT_SPEED;
                }

                Vector2 newPosition = _specterPosition;
                newPosition.X += movement.X;
                if (HasCollision(GetPlayerBox(newPosition)))
                    newPosition.X = _specterPosition.X;
                newPosition.Y += movement.Y;
                if (HasCollision(GetPlayerBox(newPosition)))
                    newPosition.Y = _specterPosition.Y;
                _specterPosition = newPosition;

                if (
                    currentKeyboardState.IsKeyDown(Keys.E)
                    && !_previousKeyboardState.IsKeyDown(Keys.E)
                )
                    CheckForInteraction();

                if (_isMoving)
                    _specterCurrent.Update(gameTime);
                else
                    _specterCurrent.CurrentFrame = 0;

                _cameraTransform = Matrix.CreateTranslation(
                    -_specterPosition.X + (GraphicsDevice.Viewport.Width / 2),
                    -_specterPosition.Y + (GraphicsDevice.Viewport.Height / 2),
                    0
                );
            }
            _previousKeyboardState = currentKeyboardState;
        }

        public override void Draw(GameTime gameTime)
        {
            _mapRenderer.Draw(SpriteBatch, _cameraTransform);

            SpriteBatch.Begin(
                transformMatrix: _cameraTransform,
                samplerState: SamplerState.PointClamp
            );

            if (_bossActive)
            {
                if (_bossIdle != null)
                    _bossIdle.Draw(SpriteBatch, _bossPosition);
                else
                    SpriteBatch.Draw(
                        _pixelTexture,
                        new Rectangle((int)_bossPosition.X, (int)_bossPosition.Y, 50, 50),
                        Color.Red
                    );
            }

            foreach (var enemy in _enemies)
            {
                enemy.Draw(SpriteBatch);
            }

            Vector2 drawPos = new Vector2((int)_specterPosition.X, (int)_specterPosition.Y);
            _specterCurrent.Draw(SpriteBatch, drawPos);

            SpriteBatch.End();

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _interactionPanel.Draw(gameTime, SpriteBatch);
            _inventoryMenu.Draw(SpriteBatch);
            _pauseMenu.Draw(SpriteBatch);
            SpriteBatch.End();
        }
    }
}
