﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lemmings2024
{
    public class Level
    {
        private Texture2D _texture;
        public Texture2D Texture => _texture;
        private Color[] _textureData;
        public Color[] TextureData => _textureData;
        private int _lemmingsCount;
        public int LemmingsCount => _lemmingsCount;
        private float _lemmingsRate;
        public float LemmingRate => _lemmingsRate;
        private float _winRate;
        public float WinRate => _winRate;
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
                            _lemmingsRate = float.Parse(dataValue);
                            break;
                        case nameof(_duration):
                            _duration = float.Parse(dataValue);
                            break;
                        case nameof(_winRate):
                            _winRate = float.Parse(dataValue) / 100f;
                            break;
                        case nameof(_startScrollOffset):
                            _startScrollOffset = int.Parse(dataValue);
                            break;
                        case nameof(_availableActions):
                            string[] actions = dataValue.Split(',');
                            _availableActions = actions.Select(action => int.Parse(action)).ToArray();
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

        public void Dig(Texture2D digTexture, Color[] digTextureData, Point position)
        {
            int startIndex = position.Y * _texture.Width + position.X;
            for (int i = 0; i < digTextureData.Length; i++)
            {
                int textureRelativeIndex = i % digTexture.Width + (i / digTexture.Width) * _texture.Width;
                if (digTextureData[i].A > 0)
                {
                    if (startIndex + textureRelativeIndex < _textureData.Length)
                    {
                        _textureData[startIndex + textureRelativeIndex] = Color.Transparent;
                    }
                }
            }
            UpdateTexture();
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
            }
            UpdateTexture();
        }

        private void UpdateTexture()
        {
            _texture.SetData(_textureData);
        }

        public void ReloadTexture()
        {
            if (_texture != null)
            {
                _content.UnloadAsset(_textureName);
            }
            _texture = _content.Load<Texture2D>(_textureName);
            _textureData = new Color[_texture.Width * _texture.Height];
            _texture.GetData(_textureData);
        }

    }
}