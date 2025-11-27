using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TiledSharp;

namespace Sequence_Break
{
    public class TiledMapRenderer
    {
        // --- ESTRUCTURA DE DATOS PARA ENEMIGOS (NECESARIA PARA CASESCREEN) ---
        public struct EnemyData
        {
            public int Id;
            public List<Vector2> Path;
            public float Speed;
            public float VisionRange;
        }

        // ---------------------------------------------------------------------

        private TmxMap _map;
        private int _tileWidth;
        private int _tileHeight;

        private Dictionary<int, Texture2D> _tilesetTextures;
        private Dictionary<Texture2D, int> _textureTilesWide;

        // --- Constantes para las flags de Tiled ---
        private const uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
        private const uint FLIPPED_VERTICALLY_FLAG = 0x40000000;
        private const uint FLIPPED_DIAGONALLY_FLAG = 0x20000000;

        // Constructor
        public TiledMapRenderer(
            ContentManager content,
            string mapPath,
            string tilesetFolderInContent
        )
        {
            _map = new TmxMap(mapPath);
            _tileWidth = _map.TileWidth;
            _tileHeight = _map.TileHeight;

            _tilesetTextures = new Dictionary<int, Texture2D>();
            _textureTilesWide = new Dictionary<Texture2D, int>();

            foreach (var tileset in _map.Tilesets)
            {
                string textureName = Path.GetFileNameWithoutExtension(tileset.Image.Source);
                string contentPath = $"{tilesetFolderInContent}/{textureName}";

                Texture2D texture = content.Load<Texture2D>(contentPath);

                _tilesetTextures.Add(tileset.FirstGid, texture);
                _textureTilesWide.Add(texture, texture.Width / _tileWidth);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
        {
            spriteBatch.Begin(
                transformMatrix: transformMatrix,
                samplerState: SamplerState.PointClamp
            );

            foreach (var layer in _map.Layers)
            {
                if (layer.GetType() != typeof(TmxLayer) || !layer.Visible)
                {
                    continue;
                }

                var tileLayer = (TmxLayer)layer;

                foreach (var tile in tileLayer.Tiles)
                {
                    if (tile.Gid == 0)
                        continue;

                    uint rawGid = (uint)tile.Gid;
                    uint cleanGid =
                        rawGid
                        & ~(
                            FLIPPED_HORIZONTALLY_FLAG
                            | FLIPPED_VERTICALLY_FLAG
                            | FLIPPED_DIAGONALLY_FLAG
                        );

                    int firstGid = _tilesetTextures
                        .Keys.OrderByDescending(k => k)
                        .FirstOrDefault(k => cleanGid >= k);

                    if (firstGid == 0)
                        continue;

                    Texture2D texture = _tilesetTextures[firstGid];
                    int tilesetTilesWide = _textureTilesWide[texture];

                    int tileGid = (int)cleanGid - firstGid;
                    int sourceRectX = (tileGid % tilesetTilesWide) * _tileWidth;
                    int sourceRectY = (tileGid / tilesetTilesWide) * _tileHeight;
                    Rectangle sourceRect = new Rectangle(
                        sourceRectX,
                        sourceRectY,
                        _tileWidth,
                        _tileHeight
                    );

                    SpriteEffects effects = SpriteEffects.None;
                    float rotation = 0f;
                    Vector2 origin = Vector2.Zero;

                    if (tile.DiagonalFlip)
                    {
                        if (tile.HorizontalFlip)
                        {
                            rotation = MathHelper.PiOver2;
                            effects |= SpriteEffects.FlipVertically;
                        }
                        else if (tile.VerticalFlip)
                        {
                            rotation = -MathHelper.PiOver2;
                            effects |= SpriteEffects.FlipVertically;
                        }
                        else
                        {
                            rotation = MathHelper.PiOver2;
                        }
                    }
                    else
                    {
                        if (tile.HorizontalFlip)
                            effects |= SpriteEffects.FlipHorizontally;
                        if (tile.VerticalFlip)
                            effects |= SpriteEffects.FlipVertically;
                    }

                    int screenX = tile.X * _tileWidth;
                    int screenY = tile.Y * _tileHeight;
                    Vector2 drawPosition;
                    if (rotation != 0f)
                    {
                        origin = new Vector2(_tileWidth / 2f, _tileHeight / 2f);
                        drawPosition = new Vector2(screenX + origin.X, screenY + origin.Y);
                    }
                    else
                    {
                        drawPosition = new Vector2(screenX, screenY);
                    }

                    spriteBatch.Draw(
                        texture,
                        drawPosition,
                        sourceRect,
                        Color.White * (float)layer.Opacity,
                        rotation,
                        origin,
                        1.0f,
                        effects,
                        0f
                    );
                }
            }
            spriteBatch.End();
        }

        public List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionBarriers = new List<Rectangle>();

            if (!_map.ObjectGroups.Contains("Collisions"))
            {
                return collisionBarriers;
            }

            var objectGroup = _map.ObjectGroups["Collisions"];

            foreach (var obj in objectGroup.Objects)
            {
                collisionBarriers.Add(
                    new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height)
                );
            }
            return collisionBarriers;
        }

        public List<CaseScreen.InteractableObject> GetInteractableObjects()
        {
            var interactableObjects = new List<CaseScreen.InteractableObject>();

            if (!_map.ObjectGroups.Contains("Interactions"))
            {
                Console.WriteLine(
                    "ADVERTENCIA: El mapa no contiene una capa de objetos llamada 'Interactions'."
                );
                return interactableObjects;
            }

            var objectGroup = _map.ObjectGroups["Interactions"];

            foreach (var obj in objectGroup.Objects)
            {
                // CORRECCION: Priorizamos el nombre nativo de Tiled.
                string name = obj.Name;
                if (string.IsNullOrEmpty(name))
                {
                    // Fallback a Custom Properties
                    if (!obj.Properties.TryGetValue("Name", out name))
                    {
                        // Si no tiene nombre en ningun lado, lo saltamos
                        continue;
                    }
                }

                obj.Properties.TryGetValue("TargetMap", out string targetMap);
                obj.Properties.TryGetValue("TargetSpawn", out string targetSpawn);
                obj.Properties.TryGetValue("Message", out string message);

                const int padding = 8;
                Rectangle triggerZone = new Rectangle(
                    (int)obj.X,
                    (int)obj.Y,
                    (int)obj.Width,
                    (int)obj.Height
                );
                triggerZone.Inflate(padding, padding);

                interactableObjects.Add(
                    new CaseScreen.InteractableObject
                    {
                        Name = name,
                        TriggerZone = triggerZone,
                        TargetMap = targetMap,
                        TargetSpawn = targetSpawn,
                        Message = message,
                    }
                );
            }
            return interactableObjects;
        }

        public Vector2 GetSpawnPoint(string spawnName)
        {
            if (!_map.ObjectGroups.Contains("Spawns"))
            {
                Console.WriteLine(
                    "ADVERTENCIA: El mapa no contiene una capa de objetos llamada 'Spawns'."
                );
                return Vector2.Zero;
            }

            var objectGroup = _map.ObjectGroups["Spawns"];
            foreach (var obj in objectGroup.Objects)
            {
                // CORRECCION: Priorizamos el nombre nativo.
                string name = obj.Name;
                if (string.IsNullOrEmpty(name))
                {
                    obj.Properties.TryGetValue("Name", out name);
                }

                if (name == spawnName)
                {
                    return new Vector2((float)obj.X, (float)obj.Y);
                }
            }

            Console.WriteLine(
                $"ADVERTENCIA: No se encontro el punto de spawn '{spawnName}' en la capa 'Spawns'."
            );
            return Vector2.Zero;
        }

        // --- NUEVO METODO: Extraer configuración de enemigos ---
        // Esto permite leer los paths sin exponer _map como publico
        public List<EnemyData> GetEnemiesConfiguration(string layerName)
        {
            var enemiesData = new List<EnemyData>();

            if (!_map.ObjectGroups.Contains(layerName))
            {
                return enemiesData;
            }

            var enemyLayer = _map.ObjectGroups[layerName];

            foreach (var obj in enemyLayer.Objects)
            {
                if (obj.Points == null || obj.Points.Count == 0)
                    continue;

                float speed = 2.0f;
                if (obj.Properties.ContainsKey("speed"))
                    float.TryParse(obj.Properties["speed"], out speed);

                float vision = 150.0f;
                if (obj.Properties.ContainsKey("vision"))
                    float.TryParse(obj.Properties["vision"], out vision);

                List<Vector2> path = new List<Vector2>();
                foreach (var p in obj.Points)
                {
                    path.Add(new Vector2((float)(obj.X + p.X), (float)(obj.Y + p.Y)));
                }

                enemiesData.Add(
                    new EnemyData
                    {
                        Id = obj.Id, // <--- GUARDAMOS EL ID UNICO DE TILED
                        Path = path,
                        Speed = speed,
                        VisionRange = vision,
                    }
                );
            }

            return enemiesData;
        }
        // --------------------------------------------------------
    }
}
