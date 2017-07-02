using System;

namespace BMAPI.v1
{
    public class TimingPoint
    {
        private static float lastBpm = 0;
        public float Time { get; set; }
        public float BpmDelay
        {
            get
            {
                return this.bpmDelay;
            }
            set
            {
                this.bpmDelay = value;
                if (this.InheritsBPM)
                {
                    this.sliderBpm = -100 * TimingPoint.lastBpm / value;
                }
                else
                {
                    this.sliderBpm = 60000 / value;
                    TimingPoint.lastBpm = this.sliderBpm;
                }
            }
        }
        private float bpmDelay = 0;
        public float SliderBpm { get { return this.sliderBpm; } }
        private float sliderBpm = 0;
        public int TimeSignature = 4;
        public int SampleSet = 0;
        public int CustomSampleSet = 0;
        public double velocity = 1;
        public double beatLength;
        public double bpm;
        public int VolumePercentage = 100;
        public bool InheritsBPM = false;
        public TimingPointOptions VisualOptions;
    }
}
