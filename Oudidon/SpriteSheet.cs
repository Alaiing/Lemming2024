using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Oudidon
{
    public class SpriteSheet
    {
        public struct Animation
        {
            public int startingFrame;
            public int endFrame;
            public float speed;
            public Point origin;
            public readonly int FrameCount => Math.Abs(endFrame - startingFrame) + 1;
            public readonly int AnimationDirection => Math.Sign(endFrame - startingFrame);
        }


        private readonly List<Texture2D> _layers = new();
        public int LayerCount => _layers.Count;
        private Rectangle[] allFrames;
        private Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        public int FrameCount => allFrames.Length;

        private Point _defaultOrigin;
        public Point DefaultPivot => _defaultOrigin;

        public int FrameWidth;
        public int FrameHeight;

        public int LeftMargin;
        public int RightMargin;
        public int TopMargin;
        public int BottomMargin;

        public SpriteSheet(ContentManager content, string asset, int frameWidth, int frameHeight, Point defaultOrigin)
        {
            Texture2D baseTexture = content.Load<Texture2D>(asset);
            _defaultOrigin = defaultOrigin;
            LeftMargin = defaultOrigin.X;
            RightMargin = frameWidth - defaultOrigin.X;
            TopMargin = defaultOrigin.Y;
            BottomMargin = frameHeight - defaultOrigin.Y;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            InitFrames(baseTexture, frameWidth, frameHeight);
            _layers.Add(baseTexture);
        }

        private void InitFrames(Texture2D baseTexture, int spriteWidth, int spriteHeight)
        {
            int xCount = baseTexture.Width / spriteWidth;
            int yCount = baseTexture.Height / spriteHeight;
            allFrames = new Rectangle[xCount * yCount];

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    allFrames[x + y * xCount] = new Rectangle(x * spriteWidth, y * spriteHeight, spriteWidth, spriteHeight);
                }
            }
        }

        public void AddLayer(ContentManager content, string asset)
        {
            _layers.Add(content.Load<Texture2D>(asset));
        }

        public void SetLayerTexture(int layerIndex, Texture2D texture)
        {
            if (layerIndex < 0 || layerIndex >= _layers.Count)
                return;

            _layers[layerIndex] = texture;
        }

        public void RegisterAnimation(string name, int startingFrame, int endingFrame, float animationSpeed, Point origin)
        {
            _animations.Add(name, new Animation { startingFrame = startingFrame, endFrame = endingFrame, speed = animationSpeed, origin = origin });
        }

        public void RegisterAnimation(string name, int startingFrame, int endingFrame, float animationSpeed)
        {
            RegisterAnimation(name, startingFrame, endingFrame, animationSpeed, _defaultOrigin);
        }

        public bool HasAnimation(string name)
        {
            return !string.IsNullOrEmpty(name) && _animations.ContainsKey(name);
        }

        public bool TryGetAnimation(string name, out Animation animation)
        {
            return _animations.TryGetValue(name, out animation);
        }

        public int GetAnimationFrameCount(string animationName)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                return animation.FrameCount;
            }

            return -1;
        }

        public int GetAnimationDirection(string animationName)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                return animation.AnimationDirection;
            }

            return 1;
        }

        public float GetAnimationSpeed(string animationName)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                return animation.speed;
            }

            return 0f;
        }

        private int GetTextureIndex(int frameIndex, int x, int y)
        {
            Rectangle frameRectangle = allFrames[frameIndex];
            return x + frameRectangle.X + (y + frameRectangle.Y) * _layers[0].Width;
        }

        public Color GetPixel(int frameIndex, int x, int y, int layer = 0)
        {
            if (x >= 0 && x < FrameWidth && y >= 0 && y < FrameHeight)
            {
                int textureIndex = GetTextureIndex(frameIndex, x, y);
                if (textureIndex >= 0 && textureIndex < _layers[layer].Width * _layers[layer].Height)
                {
                    Color[] color = new Color[1];
                    _layers[layer].GetData(color, textureIndex, 1);
                    return color[0];
                }
            }

            return new Color(0, 0, 0, 0);
        }

        public void SetPixel(int frameIndex, int x, int y, Color newColor, int layer = 0)
        {
            int textureIndex = GetTextureIndex(frameIndex, x, y);
            if (textureIndex >= 0 && textureIndex < _layers[layer].Width * _layers[layer].Height)
            {
                Color[] color = new Color[] { newColor };
                _layers[layer].SetData(color, textureIndex, 1);
            }
        }

        public void DrawAnimationFrame(string animationName, int frameIndex, SpriteBatch spriteBatch, Vector2 position, float rotation, Vector2 scale, Color color)
        {
            Color[] colors = new Color[_layers.Count];
            for (int i = 0; i < _layers.Count; i++)
            {
                colors[i] = color;
            }
            DrawAnimationFrame(animationName, frameIndex, spriteBatch, position, rotation, scale, colors);
        }
        public void DrawAnimationFrame(string animationName, int frameIndex, SpriteBatch spriteBatch, Vector2 position, float rotation, Vector2 scale, Color[] colors)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                int frame = GetAbsoluteFrameIndex(animation, frameIndex);

                if (frame >= 0)
                {
                    DrawFrame(frame, spriteBatch, position, animation.origin, rotation, scale, colors);
                }
            }
        }

        public int GetAbsoluteFrameIndex(string animationName, int frameIndex)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                return GetAbsoluteFrameIndex(animation, frameIndex);
            }

            return -1;
        }

        public static int GetAbsoluteFrameIndex(Animation animation, int frameIndex)
        {
            return animation.startingFrame + frameIndex * animation.AnimationDirection;
        }

        public void DrawFrame(int frameIndex, SpriteBatch spriteBatch, Vector2 position, Point origin, float rotation, Vector2 scale, Color color)
        {
            Color[] colors = new Color[_layers.Count];
            for (int i = 0; i < _layers.Count; i++)
            {
                colors[i] = color;
            }
            DrawFrame(frameIndex, spriteBatch, position, origin, rotation, scale, colors);
        }

        public void DrawFrame(int frameIndex, SpriteBatch spriteBatch, Vector2 position, Point origin, float rotation, Vector2 scale, Color[] colors)
        {
            if (frameIndex < allFrames.Length)
            {
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (scale.X < 0)
                {
                    spriteEffects |= SpriteEffects.FlipHorizontally;
                    scale.X = -scale.X;
                }
                if (scale.Y < 0)
                {
                    spriteEffects |= SpriteEffects.FlipVertically;
                    scale.Y = -scale.Y;
                }
                for (int i = 0; i < _layers.Count; i++)
                {
                    spriteBatch.Draw(_layers[i], position, allFrames[frameIndex], colors[i], rotation, origin.ToVector2(), scale, spriteEffects, 0);
                }
            }
        }
    }
}