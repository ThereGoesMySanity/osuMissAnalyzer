namespace BMAPI.v1.Events
{
    class BackgroundColourEvent : EventBase
    {
        public BackgroundColourEvent()
        {
        }
        public BackgroundColourEvent(EventBase baseInstance) : base(baseInstance) { }

        public Colour Colour
        {
            get; set;
        }
    }
}
