namespace BMAPI.v1.Events
{
    public class BreakEvent : EventBase
    {
        public BreakEvent()
        {
        }
        public BreakEvent(EventBase baseInstance) : base(baseInstance) { }

        public float EndTime
        {
            get; set;
        }
    }
}
