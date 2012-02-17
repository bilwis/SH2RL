using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using libtcod;

namespace ShootyShootyRL.Systems
{
    [Serializable()]
    public class ParticleEmitter
    {
        [field: NonSerialized()]
        public List<Particle> particles;

        public int abs_x, abs_y, abs_z;
        int lifespan;
        float spawn_prob;
        float int_start, int_end;
        float veloc_min, veloc_max;

        [field:NonSerialized()]
        TCODColor color;

        Random rand;
        bool initialized = false;

        public ParticleEmitter(int abs_x, int abs_y, int abs_z, int lifespan, float spawn_prob, float int_start, float int_end, float veloc_min, float veloc_max)
        {
            this.abs_x = abs_x;
            this.abs_y = abs_y;
            this.abs_z = abs_z;

            this.lifespan = lifespan;
            this.int_end = int_end;
            this.int_start = int_start;

            this.veloc_max = veloc_max;
            this.veloc_min = veloc_min;

            this.spawn_prob = spawn_prob;

            rand = new Random();
        }

        public void Init(TCODColor color)
        {
            this.color = color;
            this.particles = new List<Particle>();
            initialized = true;
        }

        public void Tick()
        {
            if (!initialized)
                return;

            if (rand.NextDouble() < spawn_prob)
            {
                float vel_x, vel_y;
                vel_x = veloc_min + ((float)rand.NextDouble() * (veloc_max - veloc_min));
                vel_y = veloc_min + ((float)rand.NextDouble() * (veloc_max - veloc_min));
                //Spawn new particle
                particles.Add(new Particle(abs_x, abs_y, vel_x, vel_y, int_start, color));
            }

            List<Particle> kill_list = new List<Particle>();

            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].abs_x += particles[i].veloc_x;
                particles[i].abs_y += particles[i].veloc_y;

                particles[i].lifetime += 1;
                if (particles[i].lifetime >= lifespan)
                    kill_list.Add(particles[i]);

                particles[i].intensity = int_end + ((particles[i].lifetime / lifespan) * (int_start - int_end));
            }

            foreach (Particle p in kill_list)
            {
                particles.Remove(p);
            }

        }
    }

}
