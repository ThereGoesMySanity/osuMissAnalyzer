namespace BMAPI.v1
{
    public class Combo
    {
        public Combo()
        {
        }
        public Combo(Colour baseInstance)
        {
            Colour.R = baseInstance.R;
            Colour.G = baseInstance.G;
            Colour.B = baseInstance.B;
        }

        public Colour Colour = new Colour();
        public int ComboNumber = 0;
    }
}
