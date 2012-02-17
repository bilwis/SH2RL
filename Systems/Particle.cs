using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

namespace ShootyShootyRL.Systems
{
    public class Particle
    {
        public float abs_x;
        public float abs_y;
        public float veloc_x;
        public float veloc_y;
        public int lifetime;
        public float intensity;
        public TCODColor color;

        public Particle(int abs_x, int abs_y, float veloc_x, float veloc_y, float intensity, TCODColor color)
        {
            this.abs_x = (float)abs_x;
            this.abs_y = (float)abs_y;

            this.veloc_x = veloc_x;
            this.veloc_y = veloc_y;

            this.intensity = intensity;
            this.color = color;

            this.lifetime = 0;
        }

        
    }
}
