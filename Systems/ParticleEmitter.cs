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
        float lifespan;   //in seconds
        float spawn_prob; //particles per second
        float int_start, int_end;
        float veloc_min, veloc_max; //movement in tiles per second
        int color_variation;

        [field:NonSerialized()]
        TCODColor color;

        Random rand;
        bool initialized = false;

        public ParticleEmitter(int abs_x, int abs_y, int abs_z, float lifespan, float spawn_prob, float int_start, float int_end, float veloc_min, float veloc_max)
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

        public void Init(TCODColor color, int var)
        {
            this.color = color;
            this.particles = new List<Particle>();
            this.color_variation = var;
            initialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elapsed">The time since the last function call in milliseconds.</param>
        public void Tick(int elapsed)
        {
            float time_factor = (float)elapsed / 1000f;

            if (!initialized)
                return;

            float new_spawn_prob = spawn_prob * time_factor;
            TCODColor new_color;
            int n_red, n_green, n_blue;

            do
            {
                if (rand.NextDouble() < new_spawn_prob)
                {
                    float vel_x, vel_y;
                    vel_x = veloc_min + ((float)rand.NextDouble() * (veloc_max - veloc_min));
                    vel_y = veloc_min + ((float)rand.NextDouble() * (veloc_max - veloc_min));
                    n_red = color.Red + rand.Next(-color_variation / 2, color_variation / 2);
                    n_green = color.Green + rand.Next(-color_variation / 2, color_variation / 2);
                    n_blue = color.Blue + rand.Next(-color_variation / 2, color_variation / 2);

                    new_color = new TCODColor(n_red > 0 && n_red < 255 ? n_red : color.Red,
                        n_green > 0 && n_green < 255 ? n_green : color.Green,
                        n_blue > 0 && n_blue < 255 ? n_blue : color.Blue);

                    //Spawn new particle
                    particles.Add(new Particle(abs_x, abs_y, vel_x, vel_y, int_start, new_color));
                }
                new_spawn_prob -= 1.0f;
            } while (new_spawn_prob > 1.0f);

            List<Particle> kill_list = new List<Particle>();

            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].abs_x += particles[i].veloc_x * time_factor;
                particles[i].abs_y += particles[i].veloc_y * time_factor;

                particles[i].lifetime += 1.0f * time_factor;
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
