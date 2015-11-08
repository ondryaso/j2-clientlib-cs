using System;
using System.Threading.Tasks;

namespace SIClient.Net
{
    public interface INetClient
    {
        String UploadImage(byte[] imageBytes);

        Task<String> UploadImageAsync(byte[] imageBytes);

        byte[] GetImage(String name, bool preferJpg, out bool isJpg);

        Task<Tuple<byte[], bool>> GetImageAsync(String name, bool preferJpg);
    }
}