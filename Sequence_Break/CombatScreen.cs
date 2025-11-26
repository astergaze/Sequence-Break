using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Sequence_Break
{
    public class CombatScreen : Screen
    {
        // --- CONSTANTES DE BALANCEO (CONFIGURACION) ---
        // Costos de Habilidades
        private const int COST_PRECOGNITION = 10;
        private const int COST_STASIS = 15;
        private const int COST_RELOAD = 15;
        private const int COST_PHASE = 20;

        // Valores de Combate
        private const int RECOVERY_DEFENSE_CORDURA = 5; // Cuanta cordura recupera defender
        private const int ENEMY_BASE_DAMAGE = 10; // Dano base del enemigo
        private const int SHOCK_DAMAGE = 5; // Dano extra al fallar prediccion

        // --- CLASES DE COMBATIENTES ---
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

        // --- VARIABLES DE UI ---
        private SpriteFont _uiFont;
        private Texture2D _pixel;
        private Texture2D _backgroundTexture;

        // --- SPRITES ---
        private TextureAtlas _enemyAtlas;
        private const float ENEMY_SCALE = 3.0f;

        // --- ANIMACION JUGADOR ---
        private TextureAtlas _specterAttackAtlas;
        private AnimatedSprite _specterAttackSprite;
        private AnimatedSprite _specterAttackIdleSprite;
        private AnimatedSprite _currentSpecterSprite;
        private Vector2 _specterPosition;
        private const float PLAYER_SCALE = 3.0f;

        // --- HIT EFFECT ---
        private TextureAtlas _hitEffectAtlas;
        private AnimatedSprite _hitSprite;
        private bool _isHitEffectActive;
        private Vector2 _hitTargetPosition;
        private const float HIT_SCALE = 3.0f;
        private const int HIT_BASE_SIZE = 64;

        // --- CONTROL DE ATAQUE ---
        private bool _isPlayerAttacking = false;
        private float _attackTimer = 0f;
        private const float ATTACK_DURATION = 0.5f;

        // --- PRECOGNICION / PREVER ---
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

        // --- ESTASIS ---
        private int _stasisTurns = 0;
        private bool _stasisSkipTurn = false;

        // --- DESFASE ---
        private bool _isPlayerPhased = false;
        private bool _isPlayerDefending = false;

        // --- COMBATIENTES ---
        private Player _player;
        private Enemy _enemy;

        // --- MAQUINA DE ESTADOS ---
        private enum CombatState
        {
            Start,
            PlayerSelectAction,
            SkillMenu,
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

        // --- MENUS ---
        private string[] _menuOptions = { "ATAQUE", "GLITCH", "DEFENSA", "OBJETOS", "ESCAPAR" };
        private int _selectedOption = 0;

        private string[] _skillOptions = { "PREVER", "ESTASIS", "RECARGAR", "DESFASE", "ATRAS" };
        private int _selectedSkillOption = 0;
        private Rectangle _uiBoxSkills;

        // --- UI ATLAS & POSICIONES ---
        private TextureAtlas _uiAtlas;
        private Sprite _uiTopLeft,
            _uiTopCenter,
            _uiTopRight;
        private Sprite _uiMiddleLeft,
            _uiMiddleCenter,
            _uiMiddleRight;
        private Sprite _uiBottomLeft,
            _uiBottomCenter,
            _uiBottomRight;

        private Rectangle _uiBoxMain;
        private Rectangle _uiBoxLeft;
        private Vector2 _menuStartPosition;

        // Colores
        private Color _menuNormalColor = Color.White;
        private Color _menuSelectedColor = new Color(112, 56, 168);
        private Color _hpColor = new Color(111, 19, 175);
        private Color _corduraColor = new Color(124, 176, 255);
        private Color _barBackgroundColor = new Color(40, 40, 40);

        private InteractionPanel _interactionPanel;
        private KeyboardState _previousKeyboardState;

        private string _returnMapName;
        private Vector2 _returnPosition;

        public CombatScreen(Game1 game, string returnMap, Vector2 returnPos)
            : base(game)
        {
            _returnMapName = returnMap;
            _returnPosition = returnPos;
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
                Console.WriteLine("ERROR: Fondo no encontrado.");
                throw;
            }

            // Cargar Enemigo
            _enemyAtlas = TextureAtlas.FromFile(
                Content,
                "textures/enemies/demo/enemy-1-texture-atlas.xml"
            );
            AnimatedSprite enemyAnimatedSprite = _enemyAtlas.CreateAnimatedSprite("enemy-attack");
            enemyAnimatedSprite.Scale = new Vector2(ENEMY_SCALE, ENEMY_SCALE);

            // Cargar Jugador & Hit
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
            _isPlayerAttacking = false;

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Cargar UI Atlas
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

            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            int uiHeight = 250;
            _uiBoxMain = new Rectangle(0, screenHeight - uiHeight, screenWidth, uiHeight);
            _uiBoxLeft = new Rectangle(
                _uiBoxMain.X,
                _uiBoxMain.Y,
                (int)(_uiBoxMain.Width * 0.25f),
                _uiBoxMain.Height
            );
            _uiBoxSkills = _uiBoxLeft;
            _menuStartPosition = new Vector2(_uiBoxMain.X + 20, _uiBoxLeft.Y + 20);

            // Inicializar Jugador
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
                Name = "Disonancia",
                CurrentHP = 80,
                MaxHP = 80,
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
                var sourceRect = _enemy.AnimatedSprite.Region.SourceRectangle;
                targetWidth = sourceRect.Width * _enemy.AnimatedSprite.Scale.X;
                targetHeight = sourceRect.Height * _enemy.AnimatedSprite.Scale.Y;
            }
            else
            {
                targetTopLeft = _specterPosition;
                var sourceRect = _currentSpecterSprite.Region.SourceRectangle;
                targetWidth = sourceRect.Width * _currentSpecterSprite.Scale.X;
                targetHeight = sourceRect.Height * _currentSpecterSprite.Scale.Y;
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
            KeyboardState currentKeyboardState = Keyboard.GetState();

            _enemy.AnimatedSprite.Update(gameTime);
            _currentSpecterSprite.Update(gameTime);

            // Update Hit Effect
            if (_isHitEffectActive)
            {
                _hitSprite.Update(gameTime);
                if (
                    _hitSprite.Animation != null
                    && _hitSprite.CurrentFrame == _hitSprite.Animation.Frames.Count - 1
                )
                    _isHitEffectActive = false;
            }

            // Update Animacion Ataque
            if (_isPlayerAttacking)
            {
                _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackTimer <= 0)
                {
                    _isPlayerAttacking = false;
                    _currentSpecterSprite = _specterAttackIdleSprite;
                    TriggerHitEffect(targetIsEnemy: true);

                    // CALCULO DE ATAQUE BASADO EN EL ARMA
                    int playerDamage = PlayerStatus.CurrentWeapon.Damage;
                    _enemy.CurrentHP -= playerDamage;

                    CombatState next =
                        (_enemy.CurrentHP <= 0) ? CombatState.Won : CombatState.EnemyTurn;
                    ShowCombatMessage(
                        $"Luka ataca! HP del enemigo: -25 {playerDamage}.",
                        next,
                        null
                    );
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
                    // REINTEGRACION DESPUES DEL DESFASE
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
                    // RESETEAR DEFENSA
                    if (_isPlayerDefending)
                    {
                        _isPlayerDefending = false;
                    }

                    HandlePlayerInput(currentKeyboardState);
                    break;

                case CombatState.SkillMenu:
                    HandleSkillMenuInput(currentKeyboardState);
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
                            int difficultyClass = 15;

                            if (checkValue >= difficultyClass)
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
                                // FALLO CRITICO EN PREDICCION
                                damageAmount = ENEMY_BASE_DAMAGE + SHOCK_DAMAGE;
                                damageTaken = true;

                                // APLICAR DEFENSA ANTES DE MOSTRAR MENSAJE
                                if (_isPlayerDefending)
                                {
                                    damageAmount /= 2;
                                }

                                TriggerHitEffect(targetIsEnemy: false);

                                string msg =
                                    $"FALLO DE CALCULO (Tirada: {checkValue}). Impacto critico: -{damageAmount} HP";
                                CombatState next =
                                    (_player.CurrentHP - damageAmount <= 0)
                                        ? CombatState.Lost
                                        : CombatState.PlayerSelectAction;

                                ShowCombatMessage(msg, next, null);
                            }
                        }
                        else
                        {
                            // ATAQUE NORMAL
                            damageAmount = ENEMY_BASE_DAMAGE;
                            damageTaken = true;

                            // APLICAR DEFENSA ANTES DE MOSTRAR MENSAJE
                            if (_isPlayerDefending)
                            {
                                damageAmount /= 2;
                            }

                            TriggerHitEffect(targetIsEnemy: false);

                            string msg = $"{_enemy.Name} ataca. - {damageAmount} HP.";
                            CombatState next =
                                (_player.CurrentHP - damageAmount <= 0)
                                    ? CombatState.Lost
                                    : CombatState.PlayerSelectAction;

                            ShowCombatMessage(msg, next, null);
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
                        // Ya calculamos la defensa arriba para mostrar el numero correcto
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
                    ItemData tempWeapon = PlayerStatus.CurrentWeapon;
                    tempWeapon.CurrentAmmo = _player.Balas;
                    PlayerStatus.CurrentWeapon = tempWeapon;

                    _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
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
            {
                PerformPlayerAction();
            }
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
            {
                PerformSkillAction();
            }
            if (kbs.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                _currentState = CombatState.PlayerSelectAction;
            }
        }

        private void PerformPlayerAction()
        {
            string action = _menuOptions[_selectedOption];
            string message = "";
            string speaker = null;

            switch (action)
            {
                case "ATAQUE":
                    if (_player.Balas > 0)
                    {
                        _player.Balas--;
                        ItemData updatedWeapon = PlayerStatus.CurrentWeapon;
                        updatedWeapon.CurrentAmmo = _player.Balas;
                        PlayerStatus.CurrentWeapon = updatedWeapon;

                        _isPlayerAttacking = true;
                        _attackTimer = ATTACK_DURATION;
                        _currentSpecterSprite = _specterAttackSprite;
                        _currentSpecterSprite.CurrentFrame = 0;
                        _currentState = CombatState.PlayerAction;
                    }
                    else
                    {
                        ShowCombatMessage(
                            "Click! El arma esta vacia...",
                            CombatState.PlayerSelectAction,
                            null
                        );
                    }
                    break;

                case "GLITCH":
                    _currentState = CombatState.SkillMenu;
                    _selectedSkillOption = 0;
                    break;

                case "DEFENSA":
                    // --- LOGICA DEFENSA ---
                    _isPlayerDefending = true;

                    // Recuperar cordura usando constante y clamp
                    _player.CurrentCordura += RECOVERY_DEFENSE_CORDURA;
                    if (_player.CurrentCordura > _player.MaxCordura)
                        _player.CurrentCordura = _player.MaxCordura;

                    PlayerStatus.ModifySanity(RECOVERY_DEFENSE_CORDURA);

                    message =
                        $"Luka adopta una postura defensiva. (+{RECOVERY_DEFENSE_CORDURA} Cordura)";
                    ShowCombatMessage(message, CombatState.EnemyTurn, speaker);
                    break;

                case "OBJETOS":
                    Console.WriteLine("Abriendo inventario... (no implementado)");
                    break;

                case "ESCAPAR":
                    _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    break;
            }
        }

        private void PerformSkillAction()
        {
            string skill = _skillOptions[_selectedSkillOption];
            string message = "";

            switch (skill)
            {
                case "PREVER":
                    if (_player.CurrentCordura >= COST_PRECOGNITION)
                    {
                        _player.CurrentCordura -= COST_PRECOGNITION;
                        PlayerStatus.ModifySanity(-COST_PRECOGNITION);
                        _precognitionTurns = 2;
                        message = "Susurros del tiempo revelan el futuro inmediato.";
                        ShowCombatMessage(message, CombatState.EnemyTurn, "Luka");
                    }
                    else
                    {
                        ShowCombatMessage(
                            $"No tienes suficiente Cordura ({COST_PRECOGNITION})!",
                            CombatState.SkillMenu,
                            null
                        );
                    }
                    break;

                case "ESTASIS":
                    if (_player.CurrentCordura >= COST_STASIS)
                    {
                        _player.CurrentCordura -= COST_STASIS;
                        PlayerStatus.ModifySanity(-COST_STASIS);
                        _stasisTurns = 4;
                        _stasisSkipTurn = true;
                        message = "Burbuja de estasis aplicada. El enemigo se ralentiza.";
                        ShowCombatMessage(message, CombatState.EnemyTurn, null);
                    }
                    else
                    {
                        ShowCombatMessage(
                            $"No tienes suficiente Cordura ({COST_STASIS})!",
                            CombatState.SkillMenu,
                            null
                        );
                    }
                    break;

                case "RECARGAR":
                    if (_player.Balas >= _player.MaxBalas)
                    {
                        ShowCombatMessage(
                            "El cargador ya esta lleno.",
                            CombatState.SkillMenu,
                            null
                        );
                    }
                    else if (_player.CurrentCordura >= COST_RELOAD)
                    {
                        _player.CurrentCordura -= COST_RELOAD;
                        PlayerStatus.ModifySanity(-COST_RELOAD);
                        _player.Balas = _player.MaxBalas;
                        ItemData weapon = PlayerStatus.CurrentWeapon;
                        weapon.CurrentAmmo = _player.Balas;
                        PlayerStatus.CurrentWeapon = weapon;
                        message =
                            $"Glitch temporal: municion materializada (-{COST_RELOAD} Cordura)";
                        ShowCombatMessage(message, CombatState.EnemyTurn, "Luka");
                    }
                    else
                    {
                        ShowCombatMessage(
                            $"No hay suficiente cordura ({COST_RELOAD}) para recargar.",
                            CombatState.SkillMenu,
                            null
                        );
                    }
                    break;

                case "DESFASE":
                    if (_player.CurrentCordura >= COST_PHASE)
                    {
                        _player.CurrentCordura -= COST_PHASE;
                        PlayerStatus.ModifySanity(-COST_PHASE);
                        _isPlayerPhased = true;
                        message = "Salto temporal. Luka se desintegra en estatica.";
                        ShowCombatMessage(message, CombatState.EnemyTurn, null);
                    }
                    else
                    {
                        ShowCombatMessage(
                            $"No tienes suficiente Cordura ({COST_PHASE}).",
                            CombatState.SkillMenu,
                            null
                        );
                    }
                    break;

                case "ATRAS":
                    _currentState = CombatState.PlayerSelectAction;
                    break;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            SpriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);
            SpriteBatch.End();

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            const float uiScale = 0.8f;

            // --- DIBUJAR JUGADOR (Solo si no esta desfasado) ---
            if (!_isPlayerPhased)
            {
                _currentSpecterSprite.Draw(SpriteBatch, _specterPosition);

                // Indicador de Defensa
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

            // --- INDICADORES (PREVER) ---
            Vector2 statusPos = new Vector2(_enemy.Position.X, _enemy.Position.Y - 40);
            if (_precognitionTurns > 0)
            {
                Color intentColor = (_enemyNextMove == EnemyIntent.Attack) ? Color.Red : Color.Cyan;
                SpriteBatch.DrawString(
                    _uiFont,
                    "[ ! ]",
                    statusPos,
                    intentColor,
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

            // --- INDICADORES (ESTASIS) ---
            if (_stasisTurns > 0)
            {
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
            }

            if (_isHitEffectActive)
            {
                _hitSprite.Draw(SpriteBatch, _hitTargetPosition);
            }

            bool showCombatUI =
                _currentState != CombatState.Start
                && _currentState != CombatState.Won
                && _currentState != CombatState.Lost
                && _currentState != CombatState.Won_End
                && _currentState != CombatState.Lost_End;

            if (showCombatUI)
            {
                DrawNineSlicePanel(SpriteBatch, _uiBoxMain);
                DrawNineSlicePanel(SpriteBatch, _uiBoxLeft);

                if (_currentState == CombatState.SkillMenu)
                {
                    DrawNineSlicePanel(SpriteBatch, _uiBoxSkills);
                    for (int i = 0; i < _skillOptions.Length; i++)
                    {
                        Color color =
                            (i == _selectedSkillOption) ? _menuSelectedColor : _menuNormalColor;
                        string optionText = $"[ {_skillOptions[i]} ]";
                        Vector2 position = new Vector2(
                            _menuStartPosition.X,
                            _menuStartPosition.Y + (i * 40)
                        );
                        SpriteBatch.DrawString(
                            _uiFont,
                            optionText,
                            position,
                            color,
                            0f,
                            Vector2.Zero,
                            uiScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
                else
                {
                    for (int i = 0; i < _menuOptions.Length; i++)
                    {
                        Color color;
                        if (_currentState == CombatState.PlayerSelectAction)
                            color = (i == _selectedOption) ? _menuSelectedColor : _menuNormalColor;
                        else
                            color =
                                ((i == _selectedOption) ? _menuSelectedColor : _menuNormalColor)
                                * 0.5f;

                        string optionText = $"[ {_menuOptions[i]} ]";
                        Vector2 position = new Vector2(
                            _menuStartPosition.X,
                            _menuStartPosition.Y + (i * 40)
                        );
                        SpriteBatch.DrawString(
                            _uiFont,
                            optionText,
                            position,
                            color,
                            0f,
                            Vector2.Zero,
                            uiScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }

                float padding = 30f;
                float statsAreaX = _uiBoxLeft.Right + padding;
                float rightAlignX = _uiBoxMain.Right - padding;
                float currentY = _uiBoxLeft.Top + 20;

                Vector2 namePosition = new Vector2(statsAreaX, currentY);
                SpriteBatch.DrawString(
                    _uiFont,
                    _player.Name,
                    namePosition,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    uiScale,
                    SpriteEffects.None,
                    0f
                );

                string balasText = $"Balas: {_player.Balas}/{_player.MaxBalas}";
                Vector2 balasTextSize = _uiFont.MeasureString(balasText) * uiScale;
                Vector2 balasPosition = new Vector2(rightAlignX - balasTextSize.X, currentY);
                SpriteBatch.DrawString(
                    _uiFont,
                    balasText,
                    balasPosition,
                    Color.Yellow,
                    0f,
                    Vector2.Zero,
                    uiScale,
                    SpriteEffects.None,
                    0f
                );

                currentY += 45;

                float hpLabelWidth = _uiFont.MeasureString("HP").X * uiScale;
                float corduraLabelWidth = _uiFont.MeasureString("Cordura").X * uiScale;
                float maxLabelWidth = Math.Max(hpLabelWidth, corduraLabelWidth);
                float barStartX = statsAreaX + maxLabelWidth + 10;
                string maxValueText = "100/100";
                float valueTextStartX =
                    rightAlignX - (_uiFont.MeasureString(maxValueText).X * uiScale);
                float fixedBarWidth = valueTextStartX - barStartX - 10;
                Vector2 barRowPosition = new Vector2(statsAreaX, currentY);

                DrawStatBar(
                    "HP",
                    _player.CurrentHP,
                    _player.MaxHP,
                    barRowPosition,
                    _hpColor,
                    uiScale,
                    barStartX,
                    fixedBarWidth,
                    valueTextStartX
                );
                currentY += 35;
                barRowPosition.Y = currentY;
                DrawStatBar(
                    "Cordura",
                    _player.CurrentCordura,
                    _player.MaxCordura,
                    barRowPosition,
                    _corduraColor,
                    uiScale,
                    barStartX,
                    fixedBarWidth,
                    valueTextStartX
                );
            }

            SpriteBatch.End();

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _interactionPanel.Draw(gameTime, SpriteBatch);
            SpriteBatch.End();
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
            Vector2 textPos = new Vector2(valueTextStartX, position.Y);
            SpriteBatch.DrawString(
                _uiFont,
                statText,
                textPos,
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
            Rectangle bgRect = new Rectangle((int)barStartX, (int)barY, (int)barWidth, barHeight);
            SpriteBatch.Draw(_pixel, bgRect, _barBackgroundColor);
            Rectangle fgRect = new Rectangle(
                (int)barStartX,
                (int)barY,
                (int)(barWidth * percent),
                barHeight
            );
            SpriteBatch.Draw(_pixel, fgRect, barColor);
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
            Rectangle topCenterSource = _uiTopCenter.Region.SourceRectangle;
            topCenterSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                topCenterSource,
                Color.White
            );
            Rectangle bottomCenterSource = _uiBottomCenter.Region.SourceRectangle;
            bottomCenterSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Bottom - cornerSize,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                bottomCenterSource,
                Color.White
            );
            Rectangle middleLeftSource = _uiMiddleLeft.Region.SourceRectangle;
            middleLeftSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                middleLeftSource,
                Color.White
            );
            Rectangle middleRightSource = _uiMiddleRight.Region.SourceRectangle;
            middleRightSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                middleRightSource,
                Color.White
            );
            Rectangle middleCenterSource = _uiMiddleCenter.Region.SourceRectangle;
            middleCenterSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y + cornerSize,
                    destination.Width - (cornerSize * 2),
                    destination.Height - (cornerSize * 2)
                ),
                middleCenterSource,
                Color.White
            );
        }
    }
}
