using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Heartbeat.Hubs
{
    public class DataHub : Hub
    {
        //public async Task SendDeviceList(Dictionary<string, object> connectedDevice)
        //{
        //    await Clients.All.SendAsync("ReceiveDeviceList", "hello world");
        //}
        public override Task OnConnectedAsync()
        {
            HeartbeatData data = new HeartbeatData(this);
            data.GetData();
            return base.OnConnectedAsync();
        }
    }
}
