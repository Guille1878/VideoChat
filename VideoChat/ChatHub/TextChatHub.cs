using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatHub
{
    public class TextChatHub : Hub<ITextChatClient>
    {
        public async Task SendMessage(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            await Clients.All.GetMessage(name, message);
        }
    }
}