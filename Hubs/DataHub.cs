using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace samplecdc.Hubs
{
    public class DataHub : Hub
    {
        public async Task SendDeviceList(Dictionary<string, object> connectedDevice)
        {
            await Clients.All.SendAsync("ReceiveDeviceList", "hello world");
        }
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
    }
}
