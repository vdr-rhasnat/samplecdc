using System;

namespace samplecdc
{
    public class HeartbeatRecord : IComparable<HeartbeatRecord> {
        public string DeviceId { get; set; }
        public string Tag { get; set; }

        public string CustomerId { get; set; }

        public string SiteId { get; set; }

        public DateTime LastHeartBeat { get; set; }

        public int CompareTo(HeartbeatRecord other)
        {
            return this.Tag.CompareTo(other);
        }
    }
}