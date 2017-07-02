namespace BMAPI.v1.Events
{
    public class EventBase
    {
        public EventBase()
        {
        }
        public EventBase(EventBase baseInstance)
        {
            StartTime = baseInstance.StartTime;
        }

        public float StartTime
        {
            get; set;
        }
    }
}
