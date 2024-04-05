using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Lemmings2024
{
    public class Level
    {
        private const string MASK_SUFFIX = "-mask";
        public enum WaterTypes { none, water, acid, lava }

        private Texture2D _texture;
        public Texture2D Texture => _texture;
        private Color[] _textureData;

        private Texture2D _maskTexture;
        public Texture2D MaskTexture => _maskTexture;
        private Color[] _maskTextureData;
        public Color[] MaskTextureData => _maskTextureData;
        private int _lemmingsCount;
        public int LemmingsCount => _lemmingsCount;
        private int _lemmingsRate;
        public int LemmingsRate => _lemmingsRate;
        private int _winRate;
        public int WinRate => _winRate;
        private float _duration;
        public float Duration => _duration;

        private int _trapType;
        public int TrapType => _trapType;
        private int _exitType;
        public float ExitType => _exitType;

        private Point[] _hatchPositions;
        public Point[] HatchPositions => _hatchPositions;
        private Point _exitPosition;
        public Point ExitPosition => _exitPosition;

        private int _startScrollOffset;
        public int StartScrollOffset => _startScrollOffset;

        private int[] _availableActions = new int[8];
        public int[] AvailableActions => _availableActions;
        private Color _dirtColor;
        public Color DirtColor => _dirtColor;
        private Color _maskColor = Color.Black;

        private WaterTypes _waterType = WaterTypes.none;
        public WaterTypes WaterType => _waterType;

        private ContentManager _content;
        private string _textureName;

        public Level(ContentManager content, string dataPath)
        {
            _content = content;
            LoadData(dataPath);
        }

        public void LoadData(string dataPath)
        {
            if (!System.IO.File.Exists(dataPath))
                return;

            string[] lines = System.IO.File.ReadAllLines(dataPath);

            try
            {
                foreach (string line in lines)
                {
                    string[] split = line.Split('=');
                    string dataName = split[0].Trim();
                    string dataValue = split[1].Trim();

                    switch (dataName)
                    {
                        case nameof(_texture):
                            _textureName = dataValue;
                            break;
                        case nameof(_trapType):
                            _trapType = int.Parse(dataValue);
                            break;
                        case nameof(_exitType):
                            _exitType = int.Parse(dataValue);
                            break;
                        case nameof(_hatchPositions):
                            string[] positions = dataValue.Split(';');
                            _hatchPositions = new Point[positions.Length];
                            for (int i = 0; i < positions.Length; i++)
                            {
                                string[] position = positions[i].Split(',');
                                Point newPoint = new Point(int.Parse(position[0].Trim()), int.Parse(position[1].Trim()));
                                _hatchPositions[i] = newPoint;
                            }
                            break;
                        case nameof(_exitPosition):
                            string[] positionData = dataValue.Split(',');
                            Point positionPoint = new Point(int.Parse(positionData[0].Trim()), int.Parse(positionData[1].Trim()));
                            _exitPosition = positionPoint;
                            break;
                        case nameof(_lemmingsCount):
                            _lemmingsCount = int.Parse(dataValue);
                            break;
                        case nameof(_lemmingsRate):
                            _lemmingsRate = int.Parse(dataValue);
                            break;
                        case nameof(_duration):
                            _duration = float.Parse(dataValue);
                            break;
                        case nameof(_winRate):
                            _winRate = int.Parse(dataValue);
                            break;
                        case nameof(_startScrollOffset):
                            _startScrollOffset = int.Parse(dataValue);
                            break;
                        case nameof(_availableActions):
                            string[] actions = dataValue.Split(',');
                            _availableActions = actions.Select(action => int.Parse(action)).ToArray();
                            break;
                        case nameof(_dirtColor):
                            string[] color = dataValue.Split(',');
                            _dirtColor = new Color(int.Parse(color[0].Trim()), int.Parse(color[1].Trim()), int.Parse(color[2].Trim()), 255);
                            break;
                        case nameof(_waterType):
                            _waterType = (WaterTypes)Enum.Parse(typeof(WaterTypes), dataValue.ToLower());
                            break;
                    }
                }

                ReloadTexture();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private bool CanDig(Texture2D digTexture, Color[] digTextureData, Point position, bool forceDig, bool flipHorizontal)
        {
            if (forceDig)
                return true;

            bool hasDirtToDig = false;

            int startIndex = position.Y * _texture.Width + position.X;
            for (int i = 0; i < digTextureData.Length; i++)
            {
                int x = i % digTexture.Width;
                if (flipHorizontal)
                    x = -x;
                int textureRelativeIndex = x + (i / digTexture.Width) * _texture.Width;
                int index = startIndex + textureRelativeIndex;
                if (index >= 0 && index < _maskTextureData.Length)
                {
                    if (digTextureData[i].A > 0)
                    {
                        Color color = _maskTextureData[index];
                        if (!IsDiggable(color, flipHorizontal))
                        {
                            return false;
                        }
                        else if (color.IsWalkable())
                        {
                            hasDirtToDig = true;
                        }
                    }
                }
            }

            return hasDirtToDig;
        }

        public bool Dig(Texture2D digTexture, Color[] digTextureData, Point position, bool forceDig = false, bool flipHorizontal = false)
        {
            if (!CanDig(digTexture, digTextureData, position, forceDig, flipHorizontal))
                return false;

            int startIndex = position.Y * _texture.Width + position.X;
            for (int i = 0; i < digTextureData.Length; i++)
            {
                int x = i % digTexture.Width;
                if (flipHorizontal)
                    x = -x;
                int textureRelativeIndex = x + (i / digTexture.Width) * _texture.Width;
                int index = startIndex + textureRelativeIndex;
                if (index >= 0 && index < _textureData.Length && digTextureData[i].A > 0)
                {
                    Color color = _maskTextureData[index];
                    if (startIndex + textureRelativeIndex < _textureData.Length)
                    {
                        if (IsDiggable(color, flipHorizontal))
                        {
                            _textureData[index] = Color.Transparent;
                            _maskTextureData[index] = Color.Transparent;
                        }
                    }
                }
            }
            UpdateTexture();
            return true;
        }

        public void Dig(Point position, int length)
        {
            if (position.Y >= _texture.Height)
                return;

            int endPosition = Math.Min(_texture.Width, position.X + length);
            length = endPosition - position.X;
            int startIndex = position.Y * _texture.Width + position.X;
            for (int i = 0; i < length; i++)
            {
                _textureData[startIndex + i] = Color.Transparent;
                _maskTextureData[startIndex + i] = Color.Transparent;
            }
            UpdateTexture();
        }

        public static bool IsDiggable(Color color, bool leftDirection)
        {
            return color.R == 0 && (color.G == 0 || leftDirection) && (color.B == 0 || !leftDirection);
        }

        public void Build(Point position, int length)
        {
            if (position.Y >= _texture.Height)
                return;

            int endPosition = Math.Min(_texture.Width, position.X + length);
            length = endPosition - position.X;
            int startIndex = position.Y * _texture.Width + position.X;
            for (int i = 0; i < length; i++)
            {
                _textureData[startIndex + i] = _dirtColor;
                _maskTextureData[startIndex + i] = _maskColor;
            }
            UpdateTexture();
        }

        public Color GetMaskPixel(Point position)
        {
            if (position.X < 0 || position.Y < 0 || position.X >= Texture.Width || position.Y >= Texture.Height)
                return Color.Black;

            int startIndex = position.Y * _texture.Width + position.X;
            return _maskTextureData[startIndex];
        }

        private void UpdateTexture()
        {
            _texture.SetData(_textureData);
            _maskTexture.SetData(_maskTextureData);
        }

        public void ReloadTexture()
        {
            if (_texture != null)
            {
                _content.UnloadAsset(_textureName);
                _content.UnloadAsset(_textureName + MASK_SUFFIX);
            }
            _texture = _content.Load<Texture2D>(_textureName);
            _textureData = new Color[_texture.Width * _texture.Height];
            _texture.GetData(_textureData);

            _maskTexture = _content.Load<Texture2D>(_textureName + MASK_SUFFIX);
            _maskTextureData = new Color[_texture.Width * _texture.Height];
            _maskTexture.GetData(_maskTextureData);
        }

    }
}
