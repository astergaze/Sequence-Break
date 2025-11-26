using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace Sequence_Break
{
    public class InventoryMenu
    {
        // Estados nav
        private enum InventoryState
        {
            Sidebar,
            ItemList,
            ItemActions,
        }

        public bool IsActive { get; private set; }

        private Game1 _game;
        private SpriteFont _font;
        private Texture2D _pixel;
        private GraphicsDevice _graphics;

        // Estados
        private InventoryState _currentState = InventoryState.Sidebar;
        private KeyboardState _prevKbState;
        private MouseState _prevMouseState;

        // Navegacion
        private string[] _tabs = { "Equipamiento", "Objetos", "Objetos clave" };
        private int _selectedTabIndex = 0;

        // Seleccion
        private int _selectedItemIndex = 0;
        private int _selectedActionIndex = 0;

        // Acciones disponibles
        private string[] _equipActions = { "Cambiar", "Inspeccionar", "Tirar" };
        private string[] _objActions = { "Usar", "Inspeccionar", "Tirar" };
        private string[] _keyActions = { "Inspeccionar" };

        // Scroll Descripcion
        private bool _showDescription = false;
        private float _scrollPosition = 0;
        private float _maxScroll = 0;

        // UI Config
        private float _uiScale = 0.65f;
        private int _sidebarWidth = 350;
        private Rectangle _contentRect;

        // Colores
        private Color _sidebarColor = new Color(0, 0, 0) * 0.6f;
        private Color _contentPanelColor = new Color(20, 30, 50);
        private Color _descriptionPanelColor = new Color(15, 20, 35);
        private Color _textColorHeader = new Color(160, 100, 220);
        private Color _textColorValue = new Color(100, 100, 200);
        private Color _highlightColor = new Color(255, 255, 100);
        private Color _descTextColor = new Color(200, 200, 200);
        private Color _buttonNormalColor = new Color(30, 35, 50);
        private Color _buttonSelectedColor = new Color(50, 40, 80);
        private Color _focusColor = new Color(160, 100, 220);
        private Color _dimColor = Color.Gray * 0.7f;
        private Color _actionSelectedColor = new Color(160, 100, 220);
        private Color _actionNormalColor = new Color(150, 150, 150);

        public InventoryMenu(Game1 game, SpriteFont font, GraphicsDevice graphics)
        {
            _game = game;
            _font = font;
            _graphics = graphics;

            _pixel = new Texture2D(graphics, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int contentWidth = 800;
            int contentHeight = 600;
            _contentRect = new Rectangle(
                _sidebarWidth + 50,
                (graphics.Viewport.Height - contentHeight) / 2,
                contentWidth,
                contentHeight
            );
        }

        // Helper para obtener la lista actual desde PlayerStatus
        private List<ItemData> CurrentList
        {
            get
            {
                if (_tabs[_selectedTabIndex] == "Objetos")
                    return PlayerStatus.Inventory;
                if (_tabs[_selectedTabIndex] == "Objetos clave")
                    return PlayerStatus.KeyItems;
                return null;
            }
        }

        private string[] CurrentActions
        {
            get
            {
                if (_tabs[_selectedTabIndex] == "Equipamiento")
                    return _equipActions;
                if (_tabs[_selectedTabIndex] == "Objetos")
                    return _objActions;
                return _keyActions;
            }
        }

        private ItemData SelectedItemData
        {
            get
            {
                if (_tabs[_selectedTabIndex] == "Equipamiento")
                    return PlayerStatus.CurrentWeapon;
                var list = CurrentList;
                if (list != null && list.Count > 0 && _selectedItemIndex < list.Count)
                    return list[_selectedItemIndex];
                return new ItemData();
            }
        }

        public void Show()
        {
            IsActive = true;
            _game.IsMouseVisible = true;
            _currentState = InventoryState.Sidebar;
            ResetSelection();
            _prevKbState = Keyboard.GetState();
            _prevMouseState = Mouse.GetState();
        }

        public void Hide()
        {
            IsActive = false;
        }

        public void Toggle()
        {
            if (IsActive)
                Hide();
            else
                Show();
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive)
                return;

            KeyboardState kbs = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            if (_showDescription)
            {
                int scrollDelta = ms.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
                if (scrollDelta != 0)
                {
                    _scrollPosition -= scrollDelta * 0.2f;
                    _scrollPosition = Math.Clamp(_scrollPosition, 0, _maxScroll);
                }
            }

            switch (_currentState)
            {
                case InventoryState.Sidebar:
                    UpdateSidebar(kbs);
                    break;
                case InventoryState.ItemList:
                    UpdateItemList(kbs);
                    break;
                case InventoryState.ItemActions:
                    UpdateItemActions(kbs);
                    break;
            }

            if (
                (kbs.IsKeyDown(Keys.Tab) && !_prevKbState.IsKeyDown(Keys.Tab))
                || (kbs.IsKeyDown(Keys.I) && !_prevKbState.IsKeyDown(Keys.I))
            )
            {
                Hide();
            }

            _prevKbState = kbs;
            _prevMouseState = ms;
        }

        private void UpdateSidebar(KeyboardState kbs)
        {
            if (kbs.IsKeyDown(Keys.W) && !_prevKbState.IsKeyDown(Keys.W))
            {
                _selectedTabIndex--;
                if (_selectedTabIndex < 0)
                    _selectedTabIndex = _tabs.Length - 1;
                ResetSelection();
            }
            if (kbs.IsKeyDown(Keys.S) && !_prevKbState.IsKeyDown(Keys.S))
            {
                _selectedTabIndex++;
                if (_selectedTabIndex >= _tabs.Length)
                    _selectedTabIndex = 0;
                ResetSelection();
            }

            if (kbs.IsKeyDown(Keys.E) && !_prevKbState.IsKeyDown(Keys.E))
            {
                bool canEnter = true;
                if (CurrentList != null && CurrentList.Count == 0)
                    canEnter = false;

                if (canEnter)
                {
                    _currentState = InventoryState.ItemList;
                    _selectedItemIndex = 0;
                }
            }

            if (kbs.IsKeyDown(Keys.Escape) && !_prevKbState.IsKeyDown(Keys.Escape))
            {
                Hide();
            }
        }

        private void UpdateItemList(KeyboardState kbs)
        {
            var list = CurrentList;
            int count = (list != null) ? list.Count : 1;

            if (kbs.IsKeyDown(Keys.W) && !_prevKbState.IsKeyDown(Keys.W))
            {
                _selectedItemIndex--;
                if (_selectedItemIndex < 0)
                    _selectedItemIndex = count - 1;
                _showDescription = false;
            }
            if (kbs.IsKeyDown(Keys.S) && !_prevKbState.IsKeyDown(Keys.S))
            {
                _selectedItemIndex++;
                if (_selectedItemIndex >= count)
                    _selectedItemIndex = 0;
                _showDescription = false;
            }

            if (kbs.IsKeyDown(Keys.E) && !_prevKbState.IsKeyDown(Keys.E))
            {
                _currentState = InventoryState.ItemActions;
                _selectedActionIndex = 0;
            }

            if (kbs.IsKeyDown(Keys.Escape) && !_prevKbState.IsKeyDown(Keys.Escape))
            {
                _currentState = InventoryState.Sidebar;
                _showDescription = false;
            }
        }

        private void UpdateItemActions(KeyboardState kbs)
        {
            string[] actions = CurrentActions;

            if (kbs.IsKeyDown(Keys.W) && !_prevKbState.IsKeyDown(Keys.W))
            {
                _selectedActionIndex--;
                if (_selectedActionIndex < 0)
                    _selectedActionIndex = actions.Length - 1;
            }
            if (kbs.IsKeyDown(Keys.S) && !_prevKbState.IsKeyDown(Keys.S))
            {
                _selectedActionIndex++;
                if (_selectedActionIndex >= actions.Length)
                    _selectedActionIndex = 0;
            }

            if (kbs.IsKeyDown(Keys.E) && !_prevKbState.IsKeyDown(Keys.E))
            {
                ExecuteAction(actions[_selectedActionIndex]);
            }

            if (kbs.IsKeyDown(Keys.Escape) && !_prevKbState.IsKeyDown(Keys.Escape))
            {
                _currentState = InventoryState.ItemList;
                _showDescription = false;
            }
        }

        private void ExecuteAction(string action)
        {
            if (action == "Inspeccionar")
            {
                _showDescription = !_showDescription;
                _scrollPosition = 0;
            }
            else if (action == "Usar")
            {
                // EJEMPLO: Usar item desde PlayerStatus
                if (SelectedItemData.Name == "Pastillas de cordura")
                {
                    PlayerStatus.ModifySanity(20);
                    Console.WriteLine("Cordura restaurada!");
                    // TO DO: Logica para eliminar y manejar cantidad de items
                }
            }
            else
            {
                Console.WriteLine($"Accion: {action} -> {SelectedItemData.Name}");
            }
        }

        private void ResetSelection()
        {
            _selectedItemIndex = 0;
            _selectedActionIndex = 0;
            _showDescription = false;
            _scrollPosition = 0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            DrawSidebar(spriteBatch);
            spriteBatch.Draw(_pixel, _contentRect, _contentPanelColor);
            DrawContent(spriteBatch);
        }

        private void DrawSidebar(SpriteBatch spriteBatch)
        {
            Rectangle sidebarRect = new Rectangle(0, 0, _sidebarWidth, _graphics.Viewport.Height);
            spriteBatch.Draw(_pixel, sidebarRect, _sidebarColor);

            // STATS DESDE PLAYERSTATUS
            Vector2 statsPos = new Vector2(20, 40);
            DrawScaledString(spriteBatch, "HP:", statsPos, _textColorHeader);
            // Leemos HP actual y Maximo
            DrawScaledString(
                spriteBatch,
                $"{PlayerStatus.CurrentHP}/{PlayerStatus.MaxHP}",
                statsPos + new Vector2(50, 0),
                _textColorHeader
            );

            statsPos.Y += 60;
            DrawScaledString(spriteBatch, "Cordura:", statsPos, _textColorValue);
            // Leemos Cordura actual y Maxima
            DrawScaledString(
                spriteBatch,
                $"{PlayerStatus.CurrentSanity}/{PlayerStatus.MaxSanity}",
                statsPos + new Vector2(140, 0),
                _textColorValue
            );

            int startY = 250;
            int buttonHeight = 80;
            int buttonGap = 20;

            for (int i = 0; i < _tabs.Length; i++)
            {
                bool isSelected = (_selectedTabIndex == i);
                Rectangle btnRect = new Rectangle(
                    20,
                    startY + (i * (buttonHeight + buttonGap)),
                    _sidebarWidth - 40,
                    buttonHeight
                );

                Color btnColor = isSelected ? _buttonSelectedColor : _buttonNormalColor;
                if (_currentState != InventoryState.Sidebar && isSelected)
                {
                    btnColor = Color.Lerp(btnColor, Color.Black, 0.3f);
                }

                spriteBatch.Draw(_pixel, btnRect, btnColor);

                if (isSelected)
                {
                    Color borderColor =
                        (_currentState == InventoryState.Sidebar) ? _textColorHeader : Color.Gray;
                    DrawBorder(spriteBatch, btnRect, borderColor, 2);
                }

                string text = _tabs[i];
                Vector2 textSize = _font.MeasureString(text) * _uiScale;
                Vector2 textPos = new Vector2(
                    btnRect.Center.X - (textSize.X / 2),
                    btnRect.Center.Y - (textSize.Y / 2)
                );

                Color txtColor = Color.White;
                if (isSelected && _currentState == InventoryState.Sidebar)
                    txtColor = Color.White;
                else if (isSelected)
                    txtColor = Color.LightGray;
                else
                    txtColor = Color.Gray;

                DrawScaledString(spriteBatch, text, textPos, txtColor);
            }
        }

        private void DrawContent(SpriteBatch spriteBatch)
        {
            Vector2 contentPos = new Vector2(_contentRect.X + 30, _contentRect.Y + 30);
            string currentTab = _tabs[_selectedTabIndex];

            Vector2 titleSize = _font.MeasureString(currentTab) * _uiScale;
            Vector2 titlePos = new Vector2(
                _contentRect.Center.X - (titleSize.X / 2),
                _contentRect.Y + 30
            );
            DrawScaledString(spriteBatch, currentTab, titlePos, Color.White);

            contentPos.Y += 60;

            if (currentTab == "Equipamiento")
            {
                DrawSingleItem(spriteBatch, contentPos, PlayerStatus.CurrentWeapon, _equipActions);
            }
            else
            {
                List<ItemData> list =
                    (currentTab == "Objetos") ? PlayerStatus.Inventory : PlayerStatus.KeyItems;
                string[] actions = (currentTab == "Objetos") ? _objActions : _keyActions;
                DrawItemList(spriteBatch, contentPos, list, actions);
            }

            if (_showDescription)
            {
                DrawDescriptionPanel(spriteBatch);
            }
        }

        private void DrawSingleItem(
            SpriteBatch spriteBatch,
            Vector2 pos,
            ItemData item,
            string[] actions
        )
        {
            bool hasFocus = (
                _currentState == InventoryState.ItemList
                || _currentState == InventoryState.ItemActions
            );
            Color nameColor = hasFocus ? _textColorHeader : _dimColor;

            DrawScaledString(spriteBatch, item.Name, pos, nameColor);

            string ammoText = $"Municion: {item.CurrentAmmo}/{item.MaxAmmo}";
            float ammoWidth = _font.MeasureString(ammoText).X * _uiScale;
            Vector2 ammoPos = new Vector2(_contentRect.Right - 30 - ammoWidth, pos.Y);
            DrawScaledString(spriteBatch, ammoText, ammoPos, _highlightColor);

            if (hasFocus)
            {
                DrawActionsBelow(spriteBatch, pos, actions);
            }
        }

        private void DrawItemList(
            SpriteBatch spriteBatch,
            Vector2 pos,
            List<ItemData> list,
            string[] actions
        )
        {
            if (list.Count == 0)
            {
                DrawScaledString(spriteBatch, "Vacio...", pos, Color.Gray);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                bool isSelected = (_selectedItemIndex == i);
                bool sectionFocus = (
                    _currentState == InventoryState.ItemList
                    || _currentState == InventoryState.ItemActions
                );

                Color nameColor =
                    (isSelected && sectionFocus)
                        ? _textColorHeader
                        : (sectionFocus ? Color.White : _dimColor);
                if (!sectionFocus)
                    nameColor = _dimColor;

                DrawScaledString(spriteBatch, list[i].Name, pos, nameColor);

                if (isSelected && sectionFocus)
                {
                    pos = DrawActionsBelow(spriteBatch, pos, actions);
                }
                else
                {
                    pos.Y += 35;
                }
            }
        }

        private Vector2 DrawActionsBelow(SpriteBatch spriteBatch, Vector2 pos, string[] actions)
        {
            pos.Y += 30;
            float actionSpacing = 30;

            if (_currentState == InventoryState.ItemActions)
            {
                for (int j = 0; j < actions.Length; j++)
                {
                    bool isActionSelected = (_selectedActionIndex == j);
                    Color actionColor = isActionSelected
                        ? _actionSelectedColor
                        : _actionNormalColor;
                    string text = actions[j];

                    if (isActionSelected)
                        text = $"[ {text} ]";

                    DrawScaledString(spriteBatch, text, pos, actionColor);
                    pos.Y += actionSpacing;
                }
            }
            else
            {
                // Preview tenue de acciones
                for (int j = 0; j < actions.Length; j++)
                {
                    DrawScaledString(spriteBatch, actions[j], pos, Color.Gray * 0.5f);
                    pos.Y += actionSpacing;
                }
            }

            pos.Y += 10;
            return pos;
        }

        private void DrawDescriptionPanel(SpriteBatch spriteBatch)
        {
            int descHeight = 250;
            int margin = 20;
            Rectangle descRect = new Rectangle(
                _contentRect.X + margin,
                _contentRect.Bottom - descHeight - margin,
                _contentRect.Width - (margin * 2),
                descHeight
            );

            spriteBatch.Draw(_pixel, descRect, _descriptionPanelColor);
            DrawBorder(spriteBatch, descRect, _textColorHeader, 1);

            ItemData item = SelectedItemData;

            Vector2 statsPos = new Vector2(descRect.X + 20, descRect.Y + 20);
            string statText =
                (item.Damage > 0) ? $"Fuerza: {item.Damage}" : $"Cantidad: {item.CurrentAmmo}";
            DrawScaledString(spriteBatch, statText, statsPos, _highlightColor);

            int textStartY = (int)statsPos.Y + 35;
            Rectangle scissorRect = new Rectangle(
                descRect.X + 10,
                textStartY,
                descRect.Width - 20,
                descRect.Height - 60
            );

            string wrappedText = WrapText(_font, item.Description, descRect.Width - 40);
            Vector2 textSize = _font.MeasureString(wrappedText) * _uiScale;
            _maxScroll = Math.Max(0, textSize.Y - scissorRect.Height);

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                new RasterizerState { ScissorTestEnable = true }
            );

            _graphics.ScissorRectangle = scissorRect;
            Vector2 textPos = new Vector2(descRect.X + 20, textStartY - _scrollPosition);
            DrawScaledString(spriteBatch, wrappedText, textPos, _descTextColor);

            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        private string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = font.MeasureString(" ").X * _uiScale;

            foreach (string word in words)
            {
                Vector2 size = font.MeasureString(word) * _uiScale;
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

        private void DrawScaledString(
            SpriteBatch spriteBatch,
            string text,
            Vector2 position,
            Color color
        )
        {
            spriteBatch.DrawString(
                _font,
                text,
                position,
                color,
                0f,
                Vector2.Zero,
                _uiScale,
                SpriteEffects.None,
                0f
            );
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(
                _pixel,
                new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(
                _pixel,
                new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height),
                color
            );
        }
    }
}
