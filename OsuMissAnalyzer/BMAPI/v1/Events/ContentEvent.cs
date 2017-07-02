namespace BMAPI.v1.Events
{
    public class ContentEvent : EventBase
    {
        public ContentEvent()
        {
        }
        public ContentEvent(EventBase baseInstance) : base(baseInstance) { }

        public ContentType Type = ContentType.Image;
        public string Filename
        {
            get; set;
        }
    }
}
