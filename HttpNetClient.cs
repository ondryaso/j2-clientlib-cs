using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SIClient.Net
{
    /// <summary>
    /// Class used for communication with J2 Server
    /// </summary>
    public class HttpNetClient : INetClient
    {
        private String addr;
        private Uri uri;

        /// <summary>
        /// Base address of the J2 Server (e.g. "http://localhost/")
        /// </summary>
        public String ServerAddress
        {
            get
            {
                return this.addr;
            }

            set
            {
                if (!value.EndsWith("/"))
                    value += "/";

                this.addr = value;
                this.uri = new Uri(value);
            }
        }

        /// <summary>
        /// If the server uses other than default ResponseManager, client can be configured to use non-default request addresses using this
        /// </summary>
        public String DefaultResponseManagerName { get; set; }

        public HttpNetClient(String serverAddr)
        {
            this.ServerAddress = serverAddr;
        }

        public HttpNetClient()
        {
        }

        /// <summary>
        /// Synchronously uploads a PNG image. Returns image URL name (that's the one you can add after the base URL). It's recommended for lower server load
        /// to check if your file is a PNG image on the client side before sending it.
        /// </summary>
        /// <param name="imageBytes">Image bytes</param>
        /// <exception cref="System.Exception">Thrown if the server returns error. The message contains a response from the server.</exception>
        /// <exception cref="System.FormatException">Thrown if server refuses the file because it's not an PNG image.</exception>
        /// <returns>Image URL name</returns>
        public String UploadImage(byte[] imageBytes)
        {
            var t = Task.Run(() => this.UploadImageAsync(imageBytes));
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// Asynchronously uploads a PNG image. Returns image URL name (that's the one you can add after the base URL). It's recommended for lower server load
        /// to check if your file is a PNG image on the client side before sending it.
        /// </summary>
        /// <param name="imageBytes">Image bytes</param>
        /// <exception cref="System.Exception">Thrown if the server returns error. The message contains a response from the server.</exception>
        /// <exception cref="System.FormatException">Thrown if server refuses the file because it's not an PNG image.</exception>
        /// <returns>Image URL name</returns>
        public async Task<String> UploadImageAsync(byte[] imageBytes)
        {
            using (HttpClient c = new HttpClient())
            {
                c.BaseAddress = this.uri;

                //Data passed here are the same as Fiddler uses, because default implementation of the server uses NanoHTTPD library, which is kinda picky about data coming.
                var multipart = new MultipartFormDataContent("-------------------------acebdf13572468");

                var bac = new ByteArrayContent(imageBytes);
                bac.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                bac.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "fieldNameHere",
                    FileName = "screenshot.png"
                };

                multipart.Add(bac, "fieldNameHere");

                var post = await c.PostAsync("push/" + this.DefaultResponseManagerName, multipart);

                String res = await post.Content.ReadAsStringAsync();

                if (post.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return res;
                }
                else if (post.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new FormatException("The server has refused this file because it's not an PNG image.");
                }
                else
                {
                    throw new Exception(res);
                }
            }
        }

        public byte[] GetImage(String name, bool preferJpg, out bool isJpg)
        {
            var t = Task.Run(() => this.GetImageAsync(name, preferJpg));
            t.Wait();
            isJpg = t.Result.Item2;
            return t.Result.Item1;
        }

        /// <summary>
        /// Asynchronously downloads image from the server. Accepts either image URL name and image file name (e.g. i123456.png and 123456.png).
        /// </summary>
        /// <param name="name">Image name</param>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the requested image doesn't exist on the server.</exception>
        /// <returns>Byte array containing the PNG image</returns>
        public async Task<Tuple<byte[], bool>> GetImageAsync(String name, bool preferJpg)
        {
            var i = await this.GetImageAsObjectAsync(name, preferJpg);
            return new Tuple<byte[], bool>(i.Item2, ImageFormat.Png.Equals(i.Item1.RawFormat));
        }

        public async Task<Tuple<Image, byte[]>> GetImageAsObjectAsync(String name, bool preferJpg)
        {
            if (!name.StartsWith("i"))
                name = "i" + name;

            using (HttpClient c = new HttpClient())
            {
                c.BaseAddress = this.uri;
                var post = await c.GetAsync(name + this.DefaultResponseManagerName);

                if (post.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new Tuple<Image, byte[]>(Image.FromStream(await post.Content.ReadAsStreamAsync()), await post.Content.ReadAsByteArrayAsync());
                }
                else
                {
                    throw new FileNotFoundException(await post.Content.ReadAsStringAsync());
                }
            }
        }
    }
}