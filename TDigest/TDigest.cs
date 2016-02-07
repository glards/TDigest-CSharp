using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TDigest
{
    class Centroid
    {
        public double mean;
        public double count;

        public Centroid(double mean, double count)
        {
            this.mean = mean;
            this.count = count;
        }

        public void Update(double x, double weight)
        {
            count += weight;
            mean += weight*(x - mean)/count;
        }
    }

    class TDigest
    {
        private static Random Rng = new Random();

        private RBTree<double, Centroid> C;
        private double n;
        private double delta;
        private int K;

        public TDigest() : this(0.01, 25)
        {
        }

        public TDigest(double delta, int K)
        {
            Initialize(delta, K);
        }

        private void Initialize(double delta, int K)
        {
            n = 0;
            this.delta = delta;
            this.K = K;
            C = new RBTree<double, Centroid>();
        }

        public void Merge(TDigest other)
        {
            
        }

        public void AddCentroid(Centroid centroid)
        {
            C.Put(centroid.mean, centroid);
        }

        public void UpdateCentroid(Centroid centroid, double x, double weight)
        {
            C.Delete(centroid.mean);
            centroid.Update(x, weight);
            AddCentroid(centroid);
        }

        private Centroid FindClosestCentroid(double x)
        {
            var ceil = C.Ceiling(x);
            var floor = C.Floor(x);

            if (!ceil.HasValue)
            {
                return floor.Value.Value;
            }

            if (!floor.HasValue)
            {
                return ceil.Value.Value;
            }

            double e = Math.Abs(floor.Value.Key - x) - Math.Abs(ceil.Value.Key - x);
            if (e < 0)
            {
                return ceil.Value.Value;
            }

            if (e > 0)
            {
                return floor.Value.Value;
            }

            return Rng.Next(100) > 50 ? ceil.Value.Value : floor.Value.Value;
        }

        private double ComputeCentroidQuantile(Centroid c)
        {
            double sum = 0;
            foreach (var c_i in C)
            {
                sum += c_i.Value.count;
                if (c_i.Key >= c.mean) break;
            }

            return (c.count/2.0 + sum)/n;
        }

        private double Threshold(double q)
        {
            return 4*n*delta*q*(1 - q);
        }

        public void Update(double x, double weight = 1)
        {
            n += weight;

            if (C.IsEmpty())
            {
                AddCentroid(new Centroid(x, weight));
                return;
            }

            var S = FindClosestCentroid(x);

            if (S != null)
            {
                var q = ComputeCentroidQuantile(S);

                var tq = Threshold(q);

                if (S.count + weight <= tq)
                {
                    double delta_w = Math.Min(tq - S.count, weight);
                    UpdateCentroid(S, x, delta_w);
                    weight -= delta_w;
                }
            }

            if (weight > 0)
            {
                AddCentroid(new Centroid(x, weight));
            }

            if (C.Count > K/delta)
            {
                Compress();
            }
        }

        private void Compress()
        {
            List<Centroid> list = C.Select(x => x.Value).ToList();
            list.Shuffle();

            Initialize(delta, K);
            foreach (var c in list)
            {
                Update(c.mean, c.count);
            }
        }
    }
}
