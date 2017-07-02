namespace BMAPI
{
    public class Colour
    {
        public Colour()
        {
        }
        public Colour(Colour baseInstance)
        {
            R = baseInstance.R;
            G = baseInstance.G;
            B = baseInstance.B;
        }

        public int R
        {
            get; set;
        }
        public int G
        {
            get; set;
        }
        public int B
        {
            get; set;
        }
    }
}
