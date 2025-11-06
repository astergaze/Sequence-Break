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
        private TmxMap _map;
        private int _tileWidth;
        private int _tileHeight;

        // Diccionario de texturas.
        // La "Key" (clave) es el FirstGid del tileset, que nos dice qué tiles le pertenecen.
        // El "Value" (valor) es la textura (imagen PNG) cargada.
        private Dictionary<int, Texture2D> _tilesetTextures;

        // También necesitamos un diccionario para guardar el "ancho en tiles" de cada textura
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
            // 1. Carga los datos del mapa
            _map = new TmxMap(mapPath);

            // 2. Guarda datos del mapa
            _tileWidth = _map.TileWidth;
            _tileHeight = _map.TileHeight;

            // 3. Inicializa los diccionarios
            _tilesetTextures = new Dictionary<int, Texture2D>();
            _textureTilesWide = new Dictionary<Texture2D, int>();

            // 4. Itera sobre cada tileset que el mapa usa
            foreach (var tileset in _map.Tilesets)
            {
                // Tiled guarda la ruta a la imagen, ej: "../textures/RecepcionTileset.png"
                // Nosotros solo queremos el nombre del archivo: "RecepcionTileset"
                string textureName = Path.GetFileNameWithoutExtension(tileset.Image.Source);

                // Construimos la ruta de MonoGame Content, ej: "maps/demo/textures/RecepcionTileset"
                string contentPath = $"{tilesetFolderInContent}/{textureName}";

                // Cargamos la textura
                Texture2D texture = content.Load<Texture2D>(contentPath);

                // La guardamos en el diccionario, usando su FirstGid como clave
                _tilesetTextures.Add(tileset.FirstGid, texture);

                // Guardamos el cálculo de su "ancho en tiles"
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
                    {
                        continue;
                    }

                    // 1. Obtenemos el GID "crudo" (con flags)
                    uint rawGid = (uint)tile.Gid;

                    // 2. Limpiamos las flags para obtener el GID de índice
                    uint cleanGid =
                        rawGid
                        & ~(
                            FLIPPED_HORIZONTALLY_FLAG
                            | FLIPPED_VERTICALLY_FLAG
                            | FLIPPED_DIAGONALLY_FLAG
                        );

                    // 3. ver que textura usar
                    // Buscamos en nuestro diccionario la clave (FirstGid) más alta
                    // que sea menor o igual a nuestro cleanGid.
                    int firstGid = _tilesetTextures
                        .Keys.OrderByDescending(k => k)
                        .FirstOrDefault(k => cleanGid >= k);

                    if (firstGid == 0)
                        continue; // No se encontró textura para este GID

                    Texture2D texture = _tilesetTextures[firstGid];
                    int tilesetTilesWide = _textureTilesWide[texture];

                    // 4. Calculamos el ID local del tile (relativo a su propio tileset)
                    int tileGid = (int)cleanGid - firstGid;

                    // 5. Calculamos el SourceRectangle
                    int sourceRectX = (tileGid % tilesetTilesWide) * _tileWidth;
                    int sourceRectY = (tileGid / tilesetTilesWide) * _tileHeight;
                    Rectangle sourceRect = new Rectangle(
                        sourceRectX,
                        sourceRectY,
                        _tileWidth,
                        _tileHeight
                    );

                    // 6. Aplicamos efectos
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
                        {
                            effects |= SpriteEffects.FlipHorizontally;
                        }
                        if (tile.VerticalFlip)
                        {
                            effects |= SpriteEffects.FlipVertically;
                        }
                    }

                    // 7. Ajustamos el Dibujo para Rotación
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

                    // 8. dibuja el tile
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
                Console.WriteLine(
                    "ADVERTENCIA: El mapa no contiene una capa de objetos llamada 'Collisions'."
                );
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
                if (!obj.Properties.TryGetValue("Name", out string name))
                {
                    Console.WriteLine(
                        $"ADVERTENCIA: Objeto de interaccion en ({obj.X}, {obj.Y}) no tiene propiedad 'Name'."
                    );
                    continue;
                }
                obj.Properties.TryGetValue("TargetMap", out string targetMap);
                obj.Properties.TryGetValue("TargetSpawn", out string targetSpawn);

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
                if (obj.Properties.TryGetValue("Name", out string name) && name == spawnName)
                {
                    return new Vector2((float)obj.X, (float)obj.Y);
                }
            }

            Console.WriteLine(
                $"ADVERTENCIA: No se encontro el punto de spawn '{spawnName}' en la capa 'Spawns'."
            );
            return Vector2.Zero;
        }
    }
}
