using System;
using System.Drawing;
using System.Drawing.Imaging;
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
        /// Name of the default response manager on the server
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
        /// See <see cref="INetClient.UploadImage(byte[])" />
        /// </summary>
        /// <exception cref="PushException">Thrown if the server returns error. The message contains a response from the server.</exception>
        /// <exception cref="BadImageFormatException">Thrown if the server refuses the file because it's not an PNG image.</exception>
        public String UploadImage(byte[] imageBytes)
        {
            var t = Task.Run(() => this.UploadImageAsync(imageBytes));
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// See <see cref="INetClient.UploadImageAsync(byte[])"/>
        /// </summary>
        /// <exception cref="PushException">Thrown if the server returns error. The message contains a response from the server.</exception>
        /// <exception cref="BadImageFormatException">Thrown if server refuses the file because it's not an PNG image.</exception>
        public async Task<String> UploadImageAsync(byte[] imageBytes)
        {
            using (HttpClient c = new HttpClient())
            {
                c.BaseAddress = this.uri;

                //Data passed here are the same as Fiddler uses, because default implementation of the server uses NanoHTTPD library, which is kinda picky about the data coming.
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
                    throw new BadImageFormatException(res);
                }
                else
                {
                    throw new PushException(res);
                }
            }
        }

        /// <summary>
        /// See <see cref="INetClient.GetImage(string, bool, out bool)"/>
        /// </summary>
        /// <exception cref="ImageNotFoundException">Thrown if the requested image doesn't exist on the server.</exception>
        public byte[] GetImage(String name, bool preferJpg, out bool isJpg)
        {
            var t = Task.Run(() => this.GetImageAsync(name, preferJpg));
            t.Wait();
            isJpg = t.Result.Item2;
            return t.Result.Item1;
        }

        /// <summary>
        /// See <see cref="INetClient.GetImageAsync(string, bool)"/>
        /// </summary>
        /// <exception cref="ImageNotFoundException">Thrown if the requested image doesn't exist on the server.</exception>
        public async Task<Tuple<byte[], bool>> GetImageAsync(String name, bool preferJpg)
        {
            var i = await this.GetImageAsObjectAsync(name, preferJpg);
            return new Tuple<byte[], bool>(i.Item2, ImageFormat.Png.Equals(i.Item1.RawFormat));
        }

        /// <summary>
        /// Synchronously downloads an image from the server. Accepts either the image URL name ot the image file name (e.g. i123456.png and 123456.png).
        /// Returns <see cref="System.Drawing"/> image and boolean determining if the returned image is in JPG format.
        /// </summary>
        /// <param name="name">Image name</param>
        /// <param name="preferJpg">Requests the JPG version of the image if true. Remember that the server doesn't have to send you the image in the format you want.</param>
        /// <exception cref="ImageNotFoundException">Thrown if the requested image doesn't exist on the server.</exception>
        /// <returns>Tuple containing <see cref="System.Drawing"/> image and boolean determining if the returned image is in JPG format</returns>
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
                    throw new ImageNotFoundException(name, await post.Content.ReadAsStringAsync());
                }
            }
        }
    }
}