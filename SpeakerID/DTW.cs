using System;
using Recorder.MFCC;    // for Sequence and its Frames

namespace Recorder.SpeakerID
{
    public static class DTW
    {
        // Euclidean distance between two 13-dim feature vectors
        /*  public static double FrameDistance(double[] a, double[] b)
          {
              double sum = 0;
              for (int k = 0; k < 13; k++)
              {
                  double d = a[k] - b[k];
                  sum += d * d;
              }
        de built in function n3ml update fe el frame nfsh a7sn
              return Math.Sqrt(sum);
          }*/

        
        public static double FrameDistance(double[] a, double[] b)
        {   
            double sum = 0;
            for (int k = 0; k < 13; k++)
            {
                double d = a[k] - b[k];
                sum += d * d;
            }
            return sum;  // NO  built in functuin y aliiiiiiiii Math.Sqrt
        }

        // 1) Plain DTW (no pruning) — O(N×M) time & space WEWOAAA
        public static double Compute(Sequence s, Sequence t)
        {
            int n = s.Frames.Length;
            int m = t.Frames.Length;
            var dtw = new double[n + 1, m + 1];
            const double INF = 1e12;

            // init EL TABLE
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= m; j++)
                    dtw[i, j] = INF;
            dtw[0, 0] = 0;

            // fill EL TABLEEEE
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    double cost = FrameDistance(s.Frames[i - 1].Features,t.Frames[j - 1].Features);

                    double best = dtw[i - 1, j];

                    if (dtw[i, j - 1] < best) 
                        best = dtw[i, j - 1];
                    if (dtw[i - 1, j - 1] < best) 
                        best = dtw[i - 1, j - 1];


                    dtw[i, j] = cost + best;
                }
            }

            return dtw[n, m];
        }

        // 2) DTWWWW pruning of width W — O(N×W) time & O(W) space
        public static double ComputePruned(Sequence s, Sequence t, int W)
        {
            int n = s.Frames.Length;
            int m = t.Frames.Length;
            var prev = new double[m + 1];
            var cur = new double[m + 1];
            const double INF = 1e12;

            // init first row
            for (int j = 0; j <= m; j++) prev[j] = INF;
            prev[0] = 0;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 0; j <= m; j++) cur[j] = INF;

                int j0;
                if (i - W < 1)
                { j0 = 1; }
                else
                { j0 = i - W;}

                /////////
                int j1;
                if (i + W > m)
                { j1 = m; }
                else
                { j1 = i + W;}


                for (int j = j0; j <= j1; j++)
                {
                    double cost = FrameDistance(s.Frames[i - 1].Features,t.Frames[j - 1].Features);

                    double best = prev[j];
                    if (prev[j - 1] < best) best = prev[j - 1];
                    if (cur[j - 1] < best) best = cur[j - 1];

                    cur[j] = cost + best;
                }

                // swapwapwapwapwapwapwap rows
                var tmp = prev; prev = cur; cur = tmp;
            }

            return prev[m];
        }




        // 3) DTW with band pruning + early‐abandon threshold INF
        public static double ComputePruned(Sequence s,Sequence t,int W,double threshold )
        {
            int n = s.Frames.Length;
            int m = t.Frames.Length;
            var prev = new double[m + 1];
            var cur = new double[m + 1];
            const double INF = 1e12;

            // init first row
            for (int j = 0; j <= m; j++) prev[j] = INF;
            prev[0] = 0;

            for (int i = 1; i <= n; i++)
            {
                bool anyBelow = false;
                for (int j = 0; j <= m; j++) cur[j] = INF;
                int j0;
                if (i - W < 1)
                { j0 = 1; }
                else
                { j0 = i - W; }

                /////////
                int j1;
                if (i + W > m)
                { j1 = m; }
                else
                { j1 = i + W; }

                for (int j = j0; j <= j1; j++)
                {
                    double cost = FrameDistance(s.Frames[i - 1].Features,t.Frames[j - 1].Features);

                    double best = prev[j];
                    if (prev[j - 1] < best) best = prev[j - 1];
                    if (cur[j - 1] < best) best = cur[j - 1];

                    double val = cost + best;
                    if (val <= threshold)
                    {
                        cur[j] = val;
                        anyBelow = true;
                    }
                }

                if (anyBelow == false)
                {
                    return INF;
                }   // all paths exceeded thred

                // hnswaaaaaappppp

                var tmp = prev; prev = cur; cur = tmp;
            }

            return prev[m];
        }
    }
}
