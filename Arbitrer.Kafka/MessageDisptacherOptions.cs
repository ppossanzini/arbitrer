using Newtonsoft.Json;

namespace Arbitrer.Kafka
{
    public class MessageDispatcherOptions
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string TopicName { get; set; }

        public string GroupID { get; set; } = "teamdev-service1";

        public bool AutoDelete = false;

        public bool Durable = true;
        public int MessageMaxBytes { get; set; } = 100000000;
        public int MessageTimeoutMs { get; set; } = 180000;
        public int? Ack { get; set; } = -1;

        public int DeDuplicationTTL { get; set; } = 5000;
        public bool DeDuplicationEnabled { get; set; } = true;
        internal bool EnableServiceWorker { get; set; } = false;

        public string? certDir { get; set; } = null;

        public JsonSerializerSettings SerializerSettings { get; set; }

        public MessageDispatcherOptions()
        {
            SerializerSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc
            };
            SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }
    }
}
