using System.Threading.Tasks;

namespace ChatHub
{
    public interface IVideoChatClient
    {
        Task DownloadStream(byte[] stream);
    }
}