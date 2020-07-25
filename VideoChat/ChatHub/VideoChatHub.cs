using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChatHub
{
    public class VideoChatHub : Hub<IVideoChatClient> 
    {

        public async Task UploadStream(byte[] stream)
        {
            await Clients.All.DownloadStream(stream);
        }
    }
}
