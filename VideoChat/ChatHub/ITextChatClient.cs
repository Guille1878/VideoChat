using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatHub
{
    public interface ITextChatClient
    {
        Task GetMessage(string name, string message);
    }
}
