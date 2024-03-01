using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Oudidon
{
    public class Particles
    {
        public struct Particle
        {
            public Color color;
            public Vector2 Position;
            public Vector2 Velocity;
            public bool useGravity;
            public float lifeTime;
            public float timeToLive;
            public float timeScale;
        }

        private static List<Particle> _particles = new();
        private static Vector2 _gravity = new Vector2(0, 9.81f);

        public static void SpawnParticle(Color color, Vector2 position, Vector2 initialVelocity, float timeToLive, bool useGravity, float timeScale = 1f)
        {
            _particles.Add(new Particle { color = color, Position = position, Velocity = initialVelocity, timeToLive = timeToLive, useGravity = useGravity, timeScale = timeScale });
        }

        public static void Update(float deltaTime)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle particle = _particles[i];
                particle.lifeTime += deltaTime;
                //if (particle.lifeTime > particle.timeToLive)
                //{ 
                //    _particles.RemoveAt(i);
                //    continue;
                //}
                UpdateParticle(ref particle, deltaTime);

                _particles[i] = particle;
            }

            _particles.RemoveAll(p => p.lifeTime >= p.timeToLive);
        }

        private static void UpdateParticle(ref Particle particle, float deltaTime)
        {
            deltaTime *= particle.timeScale;
            if (particle.useGravity)
            {
                particle.Position = 0.5f * deltaTime * deltaTime * _gravity + deltaTime * particle.Velocity + particle.Position;
                particle.Velocity = deltaTime * _gravity + particle.Velocity;
            }
            else
            {
                particle.Position += particle.Velocity * deltaTime;
            }
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            for(int i = 0; i < _particles.Count; i++)
            {
                spriteBatch.DrawRectangle(_particles[i].Position, Vector2.Zero, _particles[i].color);
            }
        }
    }
}
