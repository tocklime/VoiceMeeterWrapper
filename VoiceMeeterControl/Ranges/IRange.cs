using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceMeeterControl.Ranges
{
    public static class Extensions
    {
        public static int Clamp(this int x, int l, int h) => x < l ? l : x > h ? h : x;

        public static float Clamp(this float x, float l, float h) => x < l ? l : x > h ? h : x;
    }
    public interface IRange
    {
        float ToFloat(int value);
        int FromFloat(float value);
    }
    public class SimpleRange : IRange
    {
        private readonly int low;
        private readonly int high;

        public SimpleRange(Tuple<int,int> r) : this(r.Item1, r.Item2) { }
        public SimpleRange(int low, int high)
        {
            this.low = low;
            this.high = high;
        }
        public float ToFloat(int value) => (((float) value - low) / (high - low)).Clamp(0,1);
        public int FromFloat(float value) => ((int) (value * (high - low) + low)).Clamp(low,high);
    }
    public class SteppedRange : IRange
    {
        private readonly int low;
        private readonly int high;
        private readonly int step;
        private int Remainder(int val) => val - (val % step - low % step);

        public SteppedRange(int low, int high,int step)
        {
            this.low = low;
            this.high = high;
            this.step = step;
        }
        public float ToFloat(int value) => (((float) value - low) / (high - low)).Clamp(0,1);
        public int FromFloat(float value) => Remainder(((int) (value * (high - low) + low)).Clamp(low,high));
    }
    public class ListRange : IRange
    {
        private readonly List<int> vals;
        private readonly SimpleRange ixRange;

        public ListRange(List<int> vals)
        {
            this.vals = vals;
            ixRange = new SimpleRange(0,vals.Count);
        }

        public int FromFloat(float value) => vals[ixRange.FromFloat(value).Clamp(0,vals.Count-1)];

        public float ToFloat(int value)
        {
            for(int i = 0; i < vals.Count; i++)
            {
                if(vals[i] == value) return ixRange.ToFloat(i);
                if(i < vals.Count - 1)
                {
                    if(vals[i] < value && vals[i+1] > value)
                    {
                        return ixRange.ToFloat(i);
                    }
                    if(vals[i] > value && vals[i+1] < value)
                    {
                        return ixRange.ToFloat(i);
                    }
                }
            }
            return 0;

        }
    }
}
