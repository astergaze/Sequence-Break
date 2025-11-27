using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media; // NECESARIO PARA LA MUSICA
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Sequence_Break
{
    public class CombatScreen : Screen
    {
        // --- CONSTANTES ---
        private const int COST_PRECOGNITION = 10;
        private const int COST_STASIS = 15;
        private const int COST_RELOAD = 15;
        private const int COST_PHASE = 20;

        private const int RECOVERY_DEFENSE_CORDURA = 5;
        private const int ENEMY_BASE_DAMAGE = 10;
        private const int SHOCK_DAMAGE = 5;

        private const int HEAL_HP_SMALL = 20;
        private const int HEAL_SANITY_SMALL = 20;

        // --- CLASES INTERNAS ---
        public class Combatant
        {
            public string Name { get; set; }
            public int CurrentHP { get; set; }
            public int MaxHP { get; set; }
            public Texture2D Sprite { get; set; }
            public Vector2 Position { get; set; }
        }

        public class Player : Combatant
        {
            public int CurrentCordura { get; set; }
            public int MaxCordura { get; set; }
            public int Balas { get; set; }
            public int MaxBalas { get; set; }
            public int Perception { get; set; }
        }

        public class Enemy : Combatant
        {
            public AnimatedSprite AnimatedSprite { get; set; }
        }

        // --- VARIABLES RECURSOS ---
        private SpriteFont _uiFont;
        private Texture2D _pixel;
        private Texture2D _backgroundTexture;

        private TextureAtlas _enemyAtlas;
        private const float ENEMY_SCALE = 3.0f;

        private TextureAtlas _specterAttackAtlas;
        private AnimatedSprite _specterAttackSprite;
        private AnimatedSprite _specterAttackIdleSprite;
        private AnimatedSprite _currentSpecterSprite;
        private Vector2 _specterPosition;
        private const float PLAYER_SCALE = 3.0f;

        private TextureAtlas _hitEffectAtlas;
        private AnimatedSprite _hitSprite;
        private bool _isHitEffectActive;
        private Vector2 _hitTargetPosition;
        private const float HIT_SCALE = 3.0f;
        private const int HIT_BASE_SIZE = 64;

        // --- MUSICA (NUEVO) ---
        private Song _combatMusic;

        // --- COMBATE ---
        private bool _isPlayerAttacking = false;
        private float _attackTimer = 0f;
        private const float ATTACK_DURATION = 0.5f;

        private int _precognitionTurns = 0;
        private Random _diceRandom = new Random();

        private enum EnemyIntent
        {
            Attack,
            Defend,
            Special,
        }

        private EnemyIntent _enemyNextMove;
        private string _enemyIntentText = "";

        private int _stasisTurns = 0;
        private bool _stasisSkipTurn = false;

        private bool _isPlayerPhased = false;
        private bool _isPlayerDefending = false;

        private Player _player;
        private Enemy _enemy;

        private enum CombatState
        {
            Start,
            PlayerSelectAction,
            SkillMenu,
            Inventory,
            PlayerAction,
            EnemyTurn,
            EnemyAction,
            Won,
            Lost,
            ShowMessage,
            Won_End,
            Lost_End,
        }

        private CombatState _currentState;
        private CombatState _nextState;

        private string[] _menuOptions = { "ATAQUE", "GLITCH", "DEFENSA", "OBJETOS", "ESCAPAR" };
        private int _selectedOption = 0;

        private string[] _skillOptions = { "PREVER", "ESTASIS", "RECARGAR", "DESFASE", "ATRAS" };
        private int _selectedSkillOption = 0;

        // Inventario
        private int _selectedItemIndex = 0;
        private List<ItemData> _combatInventory;

        // UI Scroll
        private float _uiTimer;
        private float _scrollX;
        private float _scrollY;
        private const float SCROLL_SPEED_X = 60f;
        private const float SCROLL_SPEED_Y = 30f;
        private const float SCROLL_WAIT_TIME = 0.5f;

        // UI Boxes (Rectángulos)
        private Rectangle _uiBoxMainContainer; // Todo el ancho abajo
        private Rectangle _uiBoxLeft; // 25% Izquierda (Menu)
        private Rectangle _uiBoxRight; // 75% Derecha (Stats/Inventario)

        private TextureAtlas _uiAtlas;
        private Sprite _uiTopLeft,
            _uiTopCenter,
            _uiTopRight,
            _uiMiddleLeft,
            _uiMiddleCenter,
            _uiMiddleRight,
            _uiBottomLeft,
            _uiBottomCenter,
            _uiBottomRight;

        private Vector2 _menuStartPosition;

        private Color _menuNormalColor = Color.White;
        private Color _menuSelectedColor = new Color(112, 56, 168);
        private Color _hpColor = new Color(111, 19, 175);
        private Color _corduraColor = new Color(124, 176, 255);
        private Color _barBackgroundColor = new Color(40, 40, 40);

        private InteractionPanel _interactionPanel;
        private KeyboardState _previousKeyboardState;

        // --- VARIABLES DE NAVEGACION Y PERSISTENCIA ---
        private string _returnMapName;
        private Vector2 _returnPosition;
        private int _enemyId;
        private string _enemyType;

        private RasterizerState _scissorRasterizerState = new RasterizerState()
        {
            ScissorTestEnable = true,
        };

        // --- CONSTRUCTOR ---
        public CombatScreen(
            Game1 game,
            string returnMap,
            Vector2 returnPos,
            int enemyId = -1,
            string enemyType = "Common"
        )
            : base(game)
        {
            _returnMapName = returnMap;
            _returnPosition = returnPos;
            _enemyId = enemyId;
            _enemyType = enemyType;
        }

        public override void LoadContent()
        {
            _uiFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");
            try
            {
                _backgroundTexture = Content.Load<Texture2D>("Interface/Combat/battle_background");
            }
            catch
            {
                throw;
            }

            // --- CONFIGURACION DINAMICA DEL ENEMIGO Y MUSICA ---
            string atlasPath;
            string enemyName;
            int hp;
            string musicPath; // Variable para la musica

            if (_enemyType == "Boss")
            {
                atlasPath = "textures/enemies/demo/enemy-2-atlas-definition.xml";
                enemyName = "Sujeto de Prueba 02";
                hp = 200;
                musicPath = "audio/battle"; // CAMBIA ESTO POR TU ARCHIVO DE MUSICA DE JEFE
            }
            else
            {
                // Enemigo comun
                atlasPath = "textures/enemies/demo/enemy-1-texture-atlas.xml";
                enemyName = "Disonancia";
                hp = 80;
                musicPath = "audio/battle"; // CAMBIA ESTO POR TU ARCHIVO DE MUSICA DE PELEA
            }

            // --- CARGAR MUSICA ---
            try
            {
                _combatMusic = Content.Load<Song>(musicPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando musica de combate: {ex.Message}");
                _combatMusic = null;
            }
            // ---------------------

            _enemyAtlas = TextureAtlas.FromFile(Content, atlasPath);
            AnimatedSprite enemyAnimatedSprite = _enemyAtlas.CreateAnimatedSprite("enemy-attack");
            enemyAnimatedSprite.Scale = new Vector2(ENEMY_SCALE, ENEMY_SCALE);
            // -----------------------------------------

            _specterAttackAtlas = TextureAtlas.FromFile(
                Content,
                "textures/Specter-attack-atlas-definition.xml"
            );
            _hitEffectAtlas = TextureAtlas.FromFile(
                Content,
                "textures/combat/hit-effect-atlas-definition.xml"
            );

            _hitSprite = _hitEffectAtlas.CreateAnimatedSprite("attack-hit");
            _hitSprite.Scale = new Vector2(HIT_SCALE, HIT_SCALE);

            _specterAttackIdleSprite = _specterAttackAtlas.CreateAnimatedSprite("luka-idle");
            _specterAttackIdleSprite.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);
            _specterAttackSprite = _specterAttackAtlas.CreateAnimatedSprite("luka-attack");
            _specterAttackSprite.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            _currentSpecterSprite = _specterAttackIdleSprite;

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _uiAtlas = TextureAtlas.FromFile(
                Content,
                "Interface/Combat/interface-combat-atlas-definition.xml"
            );
            _uiTopLeft = _uiAtlas.CreateAnimatedSprite("top-left");
            _uiTopCenter = _uiAtlas.CreateAnimatedSprite("top-center");
            _uiTopRight = _uiAtlas.CreateAnimatedSprite("top-right");
            _uiMiddleLeft = _uiAtlas.CreateAnimatedSprite("middle-left");
            _uiMiddleCenter = _uiAtlas.CreateAnimatedSprite("middle-center");
            _uiMiddleRight = _uiAtlas.CreateAnimatedSprite("middle-right");
            _uiBottomLeft = _uiAtlas.CreateAnimatedSprite("down-left");
            _uiBottomCenter = _uiAtlas.CreateAnimatedSprite("down-center");
            _uiBottomRight = _uiAtlas.CreateAnimatedSprite("down-right");

            // --- UI LAYOUT ---
            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            int uiHeight = 250;

            _uiBoxMainContainer = new Rectangle(0, screenHeight - uiHeight, screenWidth, uiHeight);

            _uiBoxLeft = new Rectangle(
                _uiBoxMainContainer.X,
                _uiBoxMainContainer.Y,
                (int)(_uiBoxMainContainer.Width * 0.25f),
                _uiBoxMainContainer.Height
            );

            _uiBoxRight = new Rectangle(
                _uiBoxLeft.Right,
                _uiBoxMainContainer.Y,
                screenWidth - _uiBoxLeft.Width,
                _uiBoxMainContainer.Height
            );

            _menuStartPosition = new Vector2(_uiBoxLeft.X + 20, _uiBoxLeft.Y + 20);

            _player = new Player
            {
                Name = "Luka Specter",
                CurrentHP = PlayerStatus.CurrentHP,
                MaxHP = PlayerStatus.MaxHP,
                CurrentCordura = PlayerStatus.CurrentSanity,
                MaxCordura = PlayerStatus.MaxSanity,
                Balas = PlayerStatus.CurrentWeapon.CurrentAmmo,
                MaxBalas = PlayerStatus.CurrentWeapon.MaxAmmo,
                Perception = PlayerStatus.Perception,
            };

            float combatantY = screenHeight / 2 - 150;
            _specterPosition = new Vector2(200, combatantY);

            float enemyScaledWidth = enemyAnimatedSprite.Region.SourceRectangle.Width * ENEMY_SCALE;
            _enemy = new Enemy
            {
                Name = enemyName,
                CurrentHP = hp,
                MaxHP = hp,
                AnimatedSprite = enemyAnimatedSprite,
                Position = new Vector2(screenWidth - enemyScaledWidth - 200, combatantY),
            };

            _interactionPanel = new InteractionPanel(_uiFont, _uiAtlas, GraphicsDevice);
            _currentState = CombatState.Start;
            _previousKeyboardState = Keyboard.GetState();

            DecideEnemyNextMove();
        }

        private void DecideEnemyNextMove()
        {
            int roll = _diceRandom.Next(0, 100);
            if (roll < 70)
            {
                _enemyNextMove = EnemyIntent.Attack;
                _enemyIntentText = "ATACAR";
            }
            else
            {
                _enemyNextMove = EnemyIntent.Defend;
                _enemyIntentText = "DEFENDER";
            }
        }

        private void TriggerHitEffect(bool targetIsEnemy)
        {
            Vector2 targetTopLeft;
            float targetWidth,
                targetHeight;
            if (targetIsEnemy)
            {
                targetTopLeft = _enemy.Position;
                var r = _enemy.AnimatedSprite.Region.SourceRectangle;
                targetWidth = r.Width * _enemy.AnimatedSprite.Scale.X;
                targetHeight = r.Height * _enemy.AnimatedSprite.Scale.Y;
            }
            else
            {
                targetTopLeft = _specterPosition;
                var r = _currentSpecterSprite.Region.SourceRectangle;
                targetWidth = r.Width * _currentSpecterSprite.Scale.X;
                targetHeight = r.Height * _currentSpecterSprite.Scale.Y;
            }
            Vector2 targetCenter = new Vector2(
                targetTopLeft.X + (targetWidth / 2f),
                targetTopLeft.Y + (targetHeight / 2f)
            );
            float scaledHitSize = HIT_BASE_SIZE * HIT_SCALE;
            _hitTargetPosition = new Vector2(
                targetCenter.X - (scaledHitSize / 2f),
                targetCenter.Y - (scaledHitSize / 2f)
            );
            _isHitEffectActive = true;
            _hitSprite.CurrentFrame = 0;
        }

        private void ShowCombatMessage(string text, CombatState nextState, string speaker = null)
        {
            _interactionPanel.Show(text, null, speaker);
            _nextState = nextState;
            _currentState = CombatState.ShowMessage;
        }

        public override void Update(GameTime gameTime)
        {
            // --- LOGICA DE MUSICA ---
            if (_combatMusic != null && MediaPlayer.Queue.ActiveSong != _combatMusic)
            {
                MediaPlayer.Play(_combatMusic);
                MediaPlayer.IsRepeating = true;
                SettingsManager.ApplyMusicVolume();
            }
            // ------------------------

            KeyboardState currentKeyboardState = Keyboard.GetState();

            _enemy.AnimatedSprite.Update(gameTime);
            _currentSpecterSprite.Update(gameTime);

            if (_currentState == CombatState.Inventory)
            {
                _uiTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_uiTimer > SCROLL_WAIT_TIME)
                {
                    _scrollX += SCROLL_SPEED_X * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    _scrollY += SCROLL_SPEED_Y * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
            else
            {
                _uiTimer = 0;
                _scrollX = 0;
                _scrollY = 0;
            }

            if (_isHitEffectActive)
            {
                _hitSprite.Update(gameTime);
                if (
                    _hitSprite.Animation != null
                    && _hitSprite.CurrentFrame == _hitSprite.Animation.Frames.Count - 1
                )
                    _isHitEffectActive = false;
            }

            if (_isPlayerAttacking)
            {
                _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackTimer <= 0)
                {
                    _isPlayerAttacking = false;
                    _currentSpecterSprite = _specterAttackIdleSprite;
                    TriggerHitEffect(targetIsEnemy: true);
                    int playerDamage = PlayerStatus.CurrentWeapon.Damage;
                    _enemy.CurrentHP -= playerDamage;
                    CombatState next =
                        (_enemy.CurrentHP <= 0) ? CombatState.Won : CombatState.EnemyTurn;
                    ShowCombatMessage($"Luka ataca! HP del enemigo -{playerDamage}.", next, null);
                }
            }

            if (_interactionPanel.IsActive)
            {
                _interactionPanel.Update(gameTime);
                if (!_interactionPanel.IsActive)
                    _currentState = _nextState;
                _previousKeyboardState = currentKeyboardState;
                return;
            }

            switch (_currentState)
            {
                case CombatState.Start:
                    ShowCombatMessage(
                        $"{_enemy.Name} Inicia el combate",
                        CombatState.PlayerSelectAction,
                        null
                    );
                    break;

                case CombatState.PlayerSelectAction:
                    if (_isPlayerPhased)
                    {
                        _isPlayerPhased = false;
                        ShowCombatMessage(
                            "Luka se reintegra a la linea temporal.",
                            CombatState.PlayerSelectAction,
                            null
                        );
                        return;
                    }
                    if (_isPlayerDefending)
                        _isPlayerDefending = false;
                    HandlePlayerInput(currentKeyboardState);
                    break;

                case CombatState.SkillMenu:
                    HandleSkillMenuInput(currentKeyboardState);
                    break;

                case CombatState.Inventory:
                    HandleInventoryInput(currentKeyboardState);
                    break;

                case CombatState.PlayerAction:
                    break;

                case CombatState.EnemyTurn:
                    if (_stasisTurns > 0)
                    {
                        if (_stasisSkipTurn)
                        {
                            _stasisSkipTurn = false;
                            _stasisTurns--;
                            ShowCombatMessage(
                                $"GLITCH TEMPORAL! {_enemy.Name} esta congelado en estasis.",
                                CombatState.PlayerSelectAction,
                                null
                            );
                            return;
                        }
                        else
                        {
                            _stasisSkipTurn = true;
                            _stasisTurns--;
                        }
                    }
                    ShowCombatMessage($"Turno de {_enemy.Name}.", CombatState.EnemyAction, null);
                    break;

                case CombatState.EnemyAction:
                    bool damageTaken = false;
                    int damageAmount = 0;
                    if (_enemyNextMove == EnemyIntent.Attack)
                    {
                        if (_isPlayerPhased)
                        {
                            ShowCombatMessage(
                                $"{_enemy.Name} ataca el vacio... Luka no esta aqui.",
                                CombatState.PlayerSelectAction,
                                null
                            );
                            damageTaken = false;
                        }
                        else if (_precognitionTurns > 0)
                        {
                            int d20 = _diceRandom.Next(1, 21);
                            int checkValue = d20 + _player.Perception;
                            if (checkValue >= 15)
                            {
                                ShowCombatMessage(
                                    $"PREVISTO (Tirada: {checkValue}). Luka esquiva el golpe.",
                                    CombatState.PlayerSelectAction,
                                    null
                                );
                                damageTaken = false;
                            }
                            else
                            {
                                damageAmount = ENEMY_BASE_DAMAGE + SHOCK_DAMAGE;
                                damageTaken = true;
                                TriggerHitEffect(targetIsEnemy: false);
                                if (_isPlayerDefending)
                                    damageAmount /= 2;
                                ShowCombatMessage(
                                    $"ERROR DE CALCULO. Impacto critico: -{damageAmount} HP",
                                    (_player.CurrentHP - damageAmount <= 0)
                                        ? CombatState.Lost
                                        : CombatState.PlayerSelectAction,
                                    null
                                );
                            }
                        }
                        else
                        {
                            damageAmount = ENEMY_BASE_DAMAGE;
                            damageTaken = true;
                            TriggerHitEffect(targetIsEnemy: false);
                            if (_isPlayerDefending)
                                damageAmount /= 2;
                            ShowCombatMessage(
                                $"{_enemy.Name} ataca. -{damageAmount} HP",
                                (_player.CurrentHP - damageAmount <= 0)
                                    ? CombatState.Lost
                                    : CombatState.PlayerSelectAction,
                                null
                            );
                        }
                    }
                    else
                    {
                        ShowCombatMessage(
                            $"{_enemy.Name} se pone en guardia.",
                            CombatState.PlayerSelectAction,
                            null
                        );
                    }
                    if (damageTaken)
                    {
                        _player.CurrentHP -= damageAmount;
                        PlayerStatus.ModifyHP(-damageAmount);
                    }
                    DecideEnemyNextMove();
                    if (_precognitionTurns > 0)
                        _precognitionTurns--;
                    break;

                case CombatState.Won:
                    ShowCombatMessage("COMBATE GANADO!", CombatState.Won_End);
                    break;

                case CombatState.Won_End:
                    PlayerStatus.CurrentHP = _player.CurrentHP;
                    PlayerStatus.CurrentSanity = _player.CurrentCordura;
                    ItemData w = PlayerStatus.CurrentWeapon;
                    w.CurrentAmmo = _player.Balas;
                    PlayerStatus.CurrentWeapon = w;

                    // --- LOGICA DE SALIDA Y PERSISTENCIA ---
                    if (_enemyType == "Boss")
                    {
                        // AL GANAR AL JEFE: Viaje a habitacion principal
                        _game.ChangeScreen(new GameplayScreen(_game));
                    }
                    else
                    {
                        // AL GANAR A COMUN: Marcar como muerto y volver
                        if (_enemyId != -1)
                        {
                            PlayerStatus.MarkEnemyAsDefeated(_returnMapName, _enemyId);
                        }
                        _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    }
                    break;

                case CombatState.Lost:
                    ShowCombatMessage("HAS CAIDO...", CombatState.Lost_End);
                    break;
                case CombatState.Lost_End:
                    _game.ChangeScreen(new MainMenuScreen(_game));
                    break;
            }
            _previousKeyboardState = currentKeyboardState;
        }

        private void HandlePlayerInput(KeyboardState kbs)
        {
            if (
                (kbs.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                || (kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            )
            {
                _selectedOption--;
                if (_selectedOption < 0)
                    _selectedOption = _menuOptions.Length - 1;
            }
            if (
                (kbs.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                || (kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            )
            {
                _selectedOption++;
                if (_selectedOption >= _menuOptions.Length)
                    _selectedOption = 0;
            }
            if (
                (kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                || (kbs.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            )
                PerformPlayerAction();
        }

        private void HandleSkillMenuInput(KeyboardState kbs)
        {
            if (
                (kbs.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                || (kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            )
            {
                _selectedSkillOption--;
                if (_selectedSkillOption < 0)
                    _selectedSkillOption = _skillOptions.Length - 1;
            }
            if (
                (kbs.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                || (kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            )
            {
                _selectedSkillOption++;
                if (_selectedSkillOption >= _skillOptions.Length)
                    _selectedSkillOption = 0;
            }
            if (
                (kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                || (kbs.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            )
                PerformSkillAction();
            if (kbs.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
                _currentState = CombatState.PlayerSelectAction;
        }

        private void HandleInventoryInput(KeyboardState kbs)
        {
            if (
                (kbs.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                || (kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            )
            {
                _selectedItemIndex--;
                _scrollX = 0;
                _scrollY = 0;
                _uiTimer = 0;
                if (_selectedItemIndex < 0)
                    _selectedItemIndex = _combatInventory.Count - 1;
            }
            if (
                (kbs.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                || (kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            )
            {
                _selectedItemIndex++;
                _scrollX = 0;
                _scrollY = 0;
                _uiTimer = 0;
                if (_selectedItemIndex >= _combatInventory.Count)
                    _selectedItemIndex = 0;
            }
            if (
                (kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                || (kbs.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            )
                PerformItemAction();
            if (kbs.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
                _currentState = CombatState.PlayerSelectAction;
        }

        private void PerformPlayerAction()
        {
            string action = _menuOptions[_selectedOption];
            switch (action)
            {
                case "ATAQUE":
                    if (_player.Balas > 0)
                    {
                        _player.Balas--;
                        ItemData w = PlayerStatus.CurrentWeapon;
                        w.CurrentAmmo = _player.Balas;
                        PlayerStatus.CurrentWeapon = w;
                        _isPlayerAttacking = true;
                        _attackTimer = ATTACK_DURATION;
                        _currentSpecterSprite = _specterAttackSprite;
                        _currentSpecterSprite.CurrentFrame = 0;
                        _currentState = CombatState.PlayerAction;
                    }
                    else
                        ShowCombatMessage(
                            "Clic! El arma esta vacia...",
                            CombatState.PlayerSelectAction,
                            null
                        );
                    break;
                case "GLITCH":
                    _currentState = CombatState.SkillMenu;
                    _selectedSkillOption = 0;
                    break;
                case "DEFENSA":
                    _isPlayerDefending = true;
                    int rec = Math.Min(
                        RECOVERY_DEFENSE_CORDURA,
                        _player.MaxCordura - _player.CurrentCordura
                    );
                    _player.CurrentCordura += rec;
                    PlayerStatus.ModifySanity(rec);
                    ShowCombatMessage(
                        $"Luka adopta una postura defensiva. (+{rec} Cordura)",
                        CombatState.EnemyTurn,
                        null
                    );
                    break;
                case "OBJETOS":
                    _combatInventory = PlayerStatus.Inventory;
                    if (_combatInventory.Count > 0)
                    {
                        _currentState = CombatState.Inventory;
                        _selectedItemIndex = 0;
                    }
                    else
                        ShowCombatMessage(
                            "Inventario vacio.",
                            CombatState.PlayerSelectAction,
                            null
                        );
                    break;
                case "ESCAPAR":
                    if (_enemyType == "Boss")
                        ShowCombatMessage(
                            "No puedes escapar de esta pelea.",
                            CombatState.PlayerSelectAction,
                            null
                        );
                    else
                        _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    break;
            }
        }

        private void PerformSkillAction()
        {
            string skill = _skillOptions[_selectedSkillOption];
            switch (skill)
            {
                case "PREVER":
                    if (_player.CurrentCordura >= COST_PRECOGNITION)
                    {
                        _player.CurrentCordura -= COST_PRECOGNITION;
                        PlayerStatus.ModifySanity(-COST_PRECOGNITION);
                        _precognitionTurns = 2;
                        ShowCombatMessage(
                            "Susurros del tiempo revelan el futuro inmediato.",
                            CombatState.EnemyTurn,
                            "Luka"
                        );
                    }
                    else
                        ShowCombatMessage(
                            $"No tienes suficiente Cordura ({COST_PRECOGNITION})!",
                            CombatState.SkillMenu,
                            null
                        );
                    break;
                case "ESTASIS":
                    if (_player.CurrentCordura >= COST_STASIS)
                    {
                        _player.CurrentCordura -= COST_STASIS;
                        PlayerStatus.ModifySanity(-COST_STASIS);
                        _stasisTurns = 4;
                        _stasisSkipTurn = true;
                        ShowCombatMessage(
                            "Burbuja de estasis aplicada.",
                            CombatState.EnemyTurn,
                            null
                        );
                    }
                    else
                        ShowCombatMessage(
                            $"No tienes suficiente Cordura ({COST_STASIS})!",
                            CombatState.SkillMenu,
                            null
                        );
                    break;
                case "RECARGAR":
                    if (_player.Balas >= _player.MaxBalas)
                        ShowCombatMessage(
                            "El cargador ya esta lleno.",
                            CombatState.SkillMenu,
                            null
                        );
                    else if (_player.CurrentCordura >= COST_RELOAD)
                    {
                        _player.CurrentCordura -= COST_RELOAD;
                        PlayerStatus.ModifySanity(-COST_RELOAD);
                        _player.Balas = _player.MaxBalas;
                        ItemData w = PlayerStatus.CurrentWeapon;
                        w.CurrentAmmo = _player.Balas;
                        PlayerStatus.CurrentWeapon = w;
                        ShowCombatMessage(
                            $"Glitch temporal: municion materializada (-{COST_RELOAD} Cordura)",
                            CombatState.EnemyTurn,
                            "Luka"
                        );
                    }
                    else
                        ShowCombatMessage(
                            $"No hay suficiente cordura ({COST_RELOAD}).",
                            CombatState.SkillMenu,
                            null
                        );
                    break;
                case "DESFASE":
                    if (_player.CurrentCordura >= COST_PHASE)
                    {
                        _player.CurrentCordura -= COST_PHASE;
                        PlayerStatus.ModifySanity(-COST_PHASE);
                        _isPlayerPhased = true;
                        ShowCombatMessage(
                            "Salto temporal. Luka se desintegra.",
                            CombatState.EnemyTurn,
                            null
                        );
                    }
                    else
                        ShowCombatMessage(
                            $"No tienes suficiente Cordura ({COST_PHASE}).",
                            CombatState.SkillMenu,
                            null
                        );
                    break;
                case "ATRAS":
                    _currentState = CombatState.PlayerSelectAction;
                    break;
            }
        }

        private void PerformItemAction()
        {
            ItemData item = _combatInventory[_selectedItemIndex];
            string message = $"Usaste {item.Name}. ";
            if (item.Name == "Paquete de curitas" || item.Name == "Manzanas")
            {
                int heal = Math.Min(HEAL_HP_SMALL, _player.MaxHP - _player.CurrentHP);
                _player.CurrentHP += heal;
                PlayerStatus.ModifyHP(heal);
                message += $"+{heal} HP.";
            }
            else if (item.Name == "Pastillas de cordura" || item.Name == "Sedante")
            {
                int heal = Math.Min(HEAL_SANITY_SMALL, _player.MaxCordura - _player.CurrentCordura);
                _player.CurrentCordura += heal;
                PlayerStatus.ModifySanity(heal);
                message += $"+{heal} Cordura.";
            }
            else
                message += "No tuvo efecto en combate.";

            int realIndex = PlayerStatus.Inventory.FindIndex(x => x.Name == item.Name);
            if (realIndex != -1)
            {
                ItemData updated = PlayerStatus.Inventory[realIndex];
                updated.CurrentAmmo--;
                if (updated.CurrentAmmo <= 0)
                    PlayerStatus.Inventory.RemoveAt(realIndex);
                else
                    PlayerStatus.Inventory[realIndex] = updated;
            }
            _combatInventory = PlayerStatus.Inventory;
            ShowCombatMessage(message, CombatState.EnemyTurn, null);
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            SpriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);
            SpriteBatch.End();

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            const float uiScale = 0.8f;

            if (!_isPlayerPhased)
            {
                _currentSpecterSprite.Draw(SpriteBatch, _specterPosition);
                if (_isPlayerDefending)
                {
                    Vector2 shieldPos = new Vector2(_specterPosition.X, _specterPosition.Y - 40);
                    SpriteBatch.DrawString(
                        _uiFont,
                        "[ ESCUDO ]",
                        shieldPos,
                        Color.Gold,
                        0f,
                        Vector2.Zero,
                        0.8f,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
            _enemy.AnimatedSprite.Draw(SpriteBatch, _enemy.Position);

            Vector2 statusPos = new Vector2(_enemy.Position.X, _enemy.Position.Y - 40);
            if (_precognitionTurns > 0)
            {
                Color c = (_enemyNextMove == EnemyIntent.Attack) ? Color.Red : Color.Cyan;
                SpriteBatch.DrawString(
                    _uiFont,
                    "[ ! ]",
                    statusPos,
                    c,
                    0f,
                    Vector2.Zero,
                    1.0f,
                    SpriteEffects.None,
                    0f
                );
                SpriteBatch.DrawString(
                    _uiFont,
                    _enemyIntentText,
                    statusPos - new Vector2(50, 30),
                    Color.White * 0.8f,
                    0f,
                    Vector2.Zero,
                    0.7f,
                    SpriteEffects.None,
                    0f
                );
                statusPos.Y -= 30;
            }
            if (_stasisTurns > 0)
                SpriteBatch.DrawString(
                    _uiFont,
                    "[ ESTASIS ]",
                    statusPos,
                    Color.CornflowerBlue,
                    0f,
                    Vector2.Zero,
                    0.8f,
                    SpriteEffects.None,
                    0f
                );
            if (_isHitEffectActive)
                _hitSprite.Draw(SpriteBatch, _hitTargetPosition);

            bool showCombatUI =
                _currentState != CombatState.Start
                && _currentState != CombatState.Won
                && _currentState != CombatState.Lost
                && _currentState != CombatState.Won_End
                && _currentState != CombatState.Lost_End;

            if (showCombatUI)
            {
                DrawNineSlicePanel(SpriteBatch, _uiBoxLeft);
                string[] options =
                    (_currentState == CombatState.SkillMenu) ? _skillOptions : _menuOptions;
                int selected =
                    (_currentState == CombatState.SkillMenu)
                        ? _selectedSkillOption
                        : _selectedOption;

                if (_currentState == CombatState.Inventory)
                {
                    options = _menuOptions;
                    selected = _selectedOption;
                }

                for (int i = 0; i < options.Length; i++)
                {
                    Color color = (i == selected) ? _menuSelectedColor : _menuNormalColor;
                    if (
                        _currentState == CombatState.Inventory
                        || (_currentState == CombatState.SkillMenu && options != _skillOptions)
                    )
                        color = Color.Gray * 0.7f;
                    else if (
                        _currentState != CombatState.SkillMenu
                        && _currentState != CombatState.PlayerSelectAction
                    )
                        color *= 0.5f;
                    string text = $"[ {options[i]} ]";
                    Vector2 pos = new Vector2(
                        _menuStartPosition.X,
                        _menuStartPosition.Y + (i * 40)
                    );
                    SpriteBatch.DrawString(
                        _uiFont,
                        text,
                        pos,
                        color,
                        0f,
                        Vector2.Zero,
                        uiScale,
                        SpriteEffects.None,
                        0f
                    );
                }

                if (_currentState == CombatState.Inventory)
                {
                    DrawNineSlicePanel(SpriteBatch, _uiBoxRight);
                    Rectangle scissorList = new Rectangle(
                        _uiBoxRight.X + 15,
                        _uiBoxRight.Y + 15,
                        (_uiBoxRight.Width / 2) - 30,
                        _uiBoxRight.Height - 30
                    );
                    SpriteBatch.End();
                    SpriteBatch.Begin(
                        samplerState: SamplerState.PointClamp,
                        rasterizerState: _scissorRasterizerState
                    );
                    GraphicsDevice.ScissorRectangle = scissorList;
                    Vector2 listPos = new Vector2(scissorList.X, scissorList.Y);
                    for (int i = 0; i < _combatInventory.Count; i++)
                    {
                        bool isSelected = (i == _selectedItemIndex);
                        Color color = isSelected ? _menuSelectedColor : _menuNormalColor;
                        string itemName = $"[ {_combatInventory[i].Name} ]";
                        string qty = $" x{_combatInventory[i].CurrentAmmo}";
                        float maxNameWidth =
                            scissorList.Width - _uiFont.MeasureString(qty).X * uiScale;
                        Vector2 pos = listPos + new Vector2(0, i * 30);
                        if (isSelected)
                        {
                            float nameWidth = _uiFont.MeasureString(itemName).X * uiScale;
                            float xOffset = 0;
                            if (nameWidth > maxNameWidth)
                            {
                                float scrollMax = nameWidth - maxNameWidth + 20;
                                xOffset = _scrollX % (scrollMax + 50);
                                if (xOffset > scrollMax)
                                    xOffset = scrollMax;
                            }
                            SpriteBatch.DrawString(
                                _uiFont,
                                itemName,
                                pos - new Vector2(xOffset, 0),
                                color,
                                0f,
                                Vector2.Zero,
                                uiScale,
                                SpriteEffects.None,
                                0f
                            );
                            SpriteBatch.DrawString(
                                _uiFont,
                                qty,
                                new Vector2(
                                    scissorList.Right - _uiFont.MeasureString(qty).X * uiScale,
                                    pos.Y
                                ),
                                color,
                                0f,
                                Vector2.Zero,
                                uiScale,
                                SpriteEffects.None,
                                0f
                            );
                        }
                        else
                        {
                            string displayName = TruncateText(
                                _uiFont,
                                itemName,
                                maxNameWidth,
                                uiScale
                            );
                            SpriteBatch.DrawString(
                                _uiFont,
                                displayName + qty,
                                pos,
                                color,
                                0f,
                                Vector2.Zero,
                                uiScale,
                                SpriteEffects.None,
                                0f
                            );
                        }
                    }
                    SpriteBatch.End();
                    Rectangle scissorDesc = new Rectangle(
                        _uiBoxRight.X + (_uiBoxRight.Width / 2) + 10,
                        _uiBoxRight.Y + 20,
                        (_uiBoxRight.Width / 2) - 30,
                        _uiBoxRight.Height - 40
                    );
                    SpriteBatch.Begin(
                        samplerState: SamplerState.PointClamp,
                        rasterizerState: _scissorRasterizerState
                    );
                    GraphicsDevice.ScissorRectangle = scissorDesc;
                    if (_combatInventory.Count > 0)
                    {
                        string desc = _combatInventory[_selectedItemIndex].Description;
                        string wrappedDesc = WrapText(_uiFont, desc, scissorDesc.Width, uiScale);
                        float textHeight = _uiFont.MeasureString(wrappedDesc).Y * uiScale;
                        float yOffset = 0;
                        if (textHeight > scissorDesc.Height)
                        {
                            float scrollMax = textHeight - scissorDesc.Height;
                            yOffset = _scrollY % (scrollMax + 50);
                            if (yOffset > scrollMax)
                                yOffset = scrollMax;
                        }
                        Vector2 descPos = new Vector2(scissorDesc.X, scissorDesc.Y - yOffset);
                        SpriteBatch.DrawString(
                            _uiFont,
                            wrappedDesc,
                            descPos,
                            Color.Gray,
                            0f,
                            Vector2.Zero,
                            uiScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                    SpriteBatch.End();
                    SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                }
                else
                {
                    DrawNineSlicePanel(SpriteBatch, _uiBoxRight);
                    float padding = 30f;
                    float statsAreaX = _uiBoxRight.X + padding;
                    float rightAlignX = _uiBoxRight.Right - padding;
                    float currentY = _uiBoxRight.Y + 20;
                    SpriteBatch.DrawString(
                        _uiFont,
                        _player.Name,
                        new Vector2(statsAreaX, currentY),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        uiScale,
                        SpriteEffects.None,
                        0f
                    );
                    string balasText = $"Balas: {_player.Balas}/{_player.MaxBalas}";
                    SpriteBatch.DrawString(
                        _uiFont,
                        balasText,
                        new Vector2(
                            rightAlignX - (_uiFont.MeasureString(balasText).X * uiScale),
                            currentY
                        ),
                        Color.Yellow,
                        0f,
                        Vector2.Zero,
                        uiScale,
                        SpriteEffects.None,
                        0f
                    );
                    currentY += 45;
                    float hpW = _uiFont.MeasureString("HP").X * uiScale;
                    float corW = _uiFont.MeasureString("Cordura").X * uiScale;
                    float maxLabel = Math.Max(hpW, corW);
                    float barStart = statsAreaX + maxLabel + 10;
                    float valW = _uiFont.MeasureString("100/100").X * uiScale;
                    float valStart = rightAlignX - valW;
                    float barW = valStart - barStart - 10;
                    DrawStatBar(
                        "HP",
                        _player.CurrentHP,
                        _player.MaxHP,
                        new Vector2(statsAreaX, currentY),
                        _hpColor,
                        uiScale,
                        barStart,
                        barW,
                        valStart
                    );
                    currentY += 35;
                    DrawStatBar(
                        "Cordura",
                        _player.CurrentCordura,
                        _player.MaxCordura,
                        new Vector2(statsAreaX, currentY),
                        _corduraColor,
                        uiScale,
                        barStart,
                        barW,
                        valStart
                    );
                }
            }
            SpriteBatch.End();
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _interactionPanel.Draw(gameTime, SpriteBatch);
            SpriteBatch.End();
        }

        private string WrapText(SpriteFont font, string text, float maxLineWidth, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = font.MeasureString(" ").X * scale;
            foreach (string word in words)
            {
                Vector2 size = font.MeasureString(word) * scale;
                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }
            return sb.ToString();
        }

        private string TruncateText(SpriteFont font, string text, float maxWidth, float scale)
        {
            Vector2 size = font.MeasureString(text) * scale;
            if (size.X <= maxWidth)
                return text;
            for (int i = text.Length; i > 0; i--)
            {
                string shortText = text.Substring(0, i) + "...";
                if ((font.MeasureString(shortText).X * scale) <= maxWidth)
                    return shortText;
            }
            return "...";
        }

        private void DrawStatBar(
            string label,
            int current,
            int max,
            Vector2 position,
            Color barColor,
            float uiScale,
            float barStartX,
            float barWidth,
            float valueTextStartX
        )
        {
            SpriteBatch.DrawString(
                _uiFont,
                label,
                position,
                barColor,
                0f,
                Vector2.Zero,
                uiScale,
                SpriteEffects.None,
                0f
            );
            Vector2 labelSize = _uiFont.MeasureString(label) * uiScale;
            int barHeight = 20;
            float barY = position.Y + (labelSize.Y / 2) - (barHeight / 2);
            string statText = $"{current}/{max}";
            SpriteBatch.DrawString(
                _uiFont,
                statText,
                new Vector2(valueTextStartX, position.Y),
                barColor,
                0f,
                Vector2.Zero,
                uiScale,
                SpriteEffects.None,
                0f
            );
            if (barWidth < 0)
                barWidth = 0;
            float percent = Math.Clamp((float)current / max, 0f, 1f);
            SpriteBatch.Draw(
                _pixel,
                new Rectangle((int)barStartX, (int)barY, (int)barWidth, barHeight),
                _barBackgroundColor
            );
            SpriteBatch.Draw(
                _pixel,
                new Rectangle((int)barStartX, (int)barY, (int)(barWidth * percent), barHeight),
                barColor
            );
        }

        private void DrawNineSlicePanel(SpriteBatch spriteBatch, Rectangle destination)
        {
            const int sourceSpriteSize = 64;
            const float scale = 1.0f;
            int cornerSize = (int)(sourceSpriteSize * scale);
            Texture2D texture = _uiTopLeft.Region.Texture;
            spriteBatch.Draw(
                texture,
                new Rectangle(destination.X, destination.Y, cornerSize, cornerSize),
                _uiTopLeft.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Y,
                    cornerSize,
                    cornerSize
                ),
                _uiTopRight.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X,
                    destination.Bottom - cornerSize,
                    cornerSize,
                    cornerSize
                ),
                _uiBottomLeft.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Bottom - cornerSize,
                    cornerSize,
                    cornerSize
                ),
                _uiBottomRight.Region.SourceRectangle,
                Color.White
            );
            Rectangle tC = _uiTopCenter.Region.SourceRectangle;
            tC.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                tC,
                Color.White
            );
            Rectangle bC = _uiBottomCenter.Region.SourceRectangle;
            bC.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Bottom - cornerSize,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                bC,
                Color.White
            );
            Rectangle mL = _uiMiddleLeft.Region.SourceRectangle;
            mL.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                mL,
                Color.White
            );
            Rectangle mR = _uiMiddleRight.Region.SourceRectangle;
            mR.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                mR,
                Color.White
            );
            Rectangle mC = _uiMiddleCenter.Region.SourceRectangle;
            mC.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y + cornerSize,
                    destination.Width - (cornerSize * 2),
                    destination.Height - (cornerSize * 2)
                ),
                mC,
                Color.White
            );
        }
    }
}
