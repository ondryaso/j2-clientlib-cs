using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SIClient.Net
{
    /// <summary>
    /// Class used for communication with J2 Server using TCP
    /// </summary>
    public class TcpNetClient : INetClient
    {
        /// <summary>
        /// IP of the server
        /// </summary>
        public IPAddress ServerAddress { get; set; }

        /// <summary>
        /// Port to communicate on
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Request tail. Default is { 23, 3, 4 }. These bytes are used for the server to determine the end of the request.
        /// </summary>
        public byte[] RequestTail { get; set; } = { 23, 3, 4 };

        /// <summary>
        /// Maximum size of the response buffer. Default is 1024 bytes.
        /// </summary>
        public int ResponseBufferLength { get; set; } = 1024;

        /// <summary>
        /// If the server uses other than default ResponseManager, client can be configured to use non-default ResponseManager ID in the request
        /// </summary>
        public byte DefaultResponseManagerId { get; set; }

        public TcpNetClient(String hostname, int port)
        {
            this.ServerAddress = Dns.GetHostAddresses(hostname)[0];
            this.ServerPort = port;
        }

        public TcpNetClient()
        {
        }

        /// <summary>
        /// See <see cref="INetClient.UploadImage(byte[])" />
        /// </summary>
        /// <exception cref="BadImageFormatException">Thrown if server decided that passed bytes are not a PNG image.</exception>
        /// <exception cref="PushException">Thrown if unknown exception was thrown when processing the image on the server.</exception>
        /// <exception cref="BadProtocolFormatException">Thrown if the server uses different protocol version.</exception>
        public String UploadImage(byte[] imageBytes)
        {
            var t = Task.Run(() => this.UploadImageAsync(imageBytes));
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// See <see cref="INetClient.UploadImageAsync(byte[])"/>
        /// </summary>
        /// <exception cref="BadImageFormatException">Thrown if server decided that passed bytes are not a PNG image.</exception>
        /// <exception cref="PushException">Thrown if unknown exception was thrown when processing the image on the server.</exception>
        /// <exception cref="BadProtocolFormatException">Thrown if the server uses different protocol version or request trail.</exception>
        public async Task<String> UploadImageAsync(byte[] imageBytes)
        {
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(this.ServerAddress, this.ServerPort);

                var stream = client.GetStream();

                // byte array containing first three bytes to send (push command, default response manager, zero control byte)
                byte[] zero = new byte[3];
                zero[1] = this.DefaultResponseManagerId;

                await stream.WriteAsync(zero, 0, 3);
                await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
                await stream.WriteAsync(this.RequestTail, 0, this.RequestTail.Length);

                byte[] data = new byte[this.ResponseBufferLength];

                int b, c = 0, e;
                while ((b = stream.ReadByte()) != -1)
                {
                    if (c == 0)
                    {
                        e = b;
                    }
                    else
                    {
                        data[c - 1] = (byte)b;
                    }

                    c++;
                }

                String respStr;
                if (c == 0 || !(c > this.RequestTail.Length && data.Skip(c - this.RequestTail.Length - 1).Take(this.RequestTail.Length).SequenceEqual(this.RequestTail)))
                {
                    e = 4;
                    respStr = "Server didn't respond.";
                }
                else
                {
                    respStr = Encoding.UTF8.GetString(data.Take(c - this.RequestTail.Length - 1).ToArray()).Trim();
                }

                switch (b)
                {
                    case 0:
                        break;

                    case 2:
                        throw new BadImageFormatException(respStr);
                    case 3:
                        throw new PushException(respStr);
                    case 4:
                        throw new BadProtocolFormatException(respStr);
                }

                client.Close();

                return respStr;
            }
        }

        /// <summary>
        /// See <see cref="INetClient.GetImage(string, bool, out bool)"/>
        /// </summary>
        /// <exception cref="ImageNotFoundException">Thrown if the requested image doesn't exist on the server.</exception>
        /// <exception cref="BadProtocolFormatException">Thrown if the server uses different protocol version or request trail.</exception>
        public byte[] GetImage(String name, bool preferJpg, out bool isJpg)
        {
            var t = Task.Run(() => this.GetImageAsync(name, preferJpg));
            t.Wait();
            isJpg = (t.Result?.Item2).Value;
            return t.Result?.Item1;
        }

        /// <summary>
        /// See <see cref="INetClient.GetImageAsync(string, bool)"/>
        /// </summary>
        /// <exception cref="ImageNotFoundException">Thrown if the requested image doesn't exist on the server.</exception>
        /// <exception cref="BadProtocolFormatException">Thrown if the server uses different protocol version or request trail.</exception>
        public async Task<Tuple<byte[], bool>> GetImageAsync(String name, bool preferJpg)
        {
            if (name.StartsWith("i"))
                name = name.Substring(1);

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, this.ServerPort);

                var stream = client.GetStream();

                // byte array containing first three bytes to send (pull command, default response manager, jpg/png preference)
                byte[] zero = new byte[3] { 1, this.DefaultResponseManagerId, (byte)(preferJpg ? 1 : 0) };
                // byte array containing UTF-8 bytes of the name of required image
                byte[] nameBytes = Encoding.UTF8.GetBytes(name);

                await stream.WriteAsync(zero, 0, 3);
                await stream.WriteAsync(nameBytes, 0, nameBytes.Length);
                await stream.WriteAsync(this.RequestTail, 0, this.RequestTail.Length);

                byte[] data = new byte[this.ResponseBufferLength];

                int b, c = 0, e = 0;
                bool isJpg = false;
                while ((b = stream.ReadByte()) != -1)
                {
                    if (c == 0)
                    {
                        e = b;
                    }
                    else if (c == 1)
                    {
                        isJpg = (b == 1);
                    }
                    else
                    {
                        data[c - 2] = (byte)b;
                        if (c == data.Length - 1)
                        {
                            Array.Resize(ref data, data.Length * 2);
                        }
                    }

                    c++;
                }

                if (c == 0 || !(c > this.RequestTail.Length && data.Skip(c - this.RequestTail.Length - 2).Take(this.RequestTail.Length).SequenceEqual(this.RequestTail)))
                {
                    e = 4;
                    throw new BadProtocolFormatException("Server didn't respond.");
                }
                else
                {
                    data = data.Take(c - RequestTail.Length - 2).ToArray();
                    switch (e)
                    {
                        case 0:
                            return new Tuple<byte[], bool>(data, isJpg);

                        case 1:
                            throw new ImageNotFoundException(name, Encoding.UTF8.GetString(data));
                    }
                }

                client.Close();

                return null;
            }
        }
    }
}