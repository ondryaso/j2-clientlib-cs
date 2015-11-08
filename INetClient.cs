using System;
using System.Threading.Tasks;

namespace SIClient.Net
{
    public interface INetClient
    {
        /// <summary>
        /// Synchronously uploads a PNG image. It's recommended for lower server load
        /// to check if your file is a PNG image on the client side before sending it.
        /// </summary>
        /// <param name="imageBytes">Image bytes</param>
        /// <returns>Image name</returns>
        String UploadImage(byte[] imageBytes);

        /// <summary>
        /// Asynchronously uploads a PNG image. It's recommended for lower server load
        /// to check if your file is a PNG image on the client side before sending it.
        /// </summary>
        /// <param name="imageBytes">Image bytes</param>
        /// <returns>Image name</returns>
        Task<String> UploadImageAsync(byte[] imageBytes);

        /// <summary>
        /// Synchronously downloads an image from the server. Accepts either the image URL name or the image file name (e.g. i123456.png and 123456.png).
        /// Returns a tuple containing a byte array with the image and a boolean determining if the returned image is in JPG format.
        /// </summary>
        /// <param name="name">Image name</param>
        /// <param name="preferJpg">Requests the JPG version of the image if true. Remember that the server doesn't have to send you the image in the format you want.</param>
        /// <param name="isJpg">Out parameter, true if the image got is a JPG image.</param>
        /// <returns>Byte array containing the image</returns>
        byte[] GetImage(String name, bool preferJpg, out bool isJpg);

        /// <summary>
        /// Asynchronously downloads an image from the server. Accepts either the image URL name or the image file name (e.g. i123456.png and 123456.png).
        /// Returns a tuple containing a byte array with the image and a boolean determining if the returned image is in JPG format.
        /// </summary>
        /// <param name="name">Image name</param>
        /// <param name="preferJpg">Requests the JPG version of the image if true. Remember that the server doesn't have to send you the image in the format you want.</param>
        /// <returns>Tuple containing byte array with the image and boolean determining if the returned image is in JPG format</returns>
        Task<Tuple<byte[], bool>> GetImageAsync(String name, bool preferJpg);
    }
}