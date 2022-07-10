using NAudio.Wave;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;

namespace audioStream
{
    internal class Program
    {
        static Collection<Stream> clientS = new Collection<Stream>();

        static void Main(string[] args)
        {
            var capture = new WasapiLoopbackCapture();
            capture.DataAvailable += (s, a) =>
            {
                if (clientS.Count > 0)
                {
                    // Reverse looping
                    for (int i = clientS.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            clientS[i].Write(a.Buffer, 0, a.BytesRecorded);
                        }
                        catch
                        {
                            clientS[i].Dispose();
                            clientS.RemoveAt(i);
                            Console.WriteLine("[STREAM] Disposed one client stream");
                        }
                    }
                }
            };

            capture.StartRecording();
            RunServer();

            Console.WriteLine("Ended");
            Console.ReadKey();
        }

        static void RunServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Prefixes.Add("http://192.168.1.102:8080/");
            listener.Start();
            Console.WriteLine("[HTTP] Server start");
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                Console.WriteLine("[HTTP] Request " + request.RawUrl);

                if (request.Url.LocalPath == "/audio")
                {
                    response.ContentType = "audio/x-wav;codec=pcm";
                    response.SendChunked = true;

                    var header = RiffHeader(0).ToArray();
                    response.OutputStream.Write(header, 0, header.Length);
                    clientS.Add(response.OutputStream);
                    Console.WriteLine("[HTTP] Added one client to stream");
                }
                else
                {
                    response.ContentType = "text/html";
                    string responseString = @"
<audio controls preload=""none"">
    <source src=""/audio"" type=""audio/x-wav;codec=pcm"">
</audio>
";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
        }

        static List<byte> RiffHeader(int size)
        {
            List<byte> header = new List<byte>();
            header.AddRange(Encoding.ASCII.GetBytes("RIFF"));
            header.AddRange(BitConverter.GetBytes(Int32.MaxValue));
            header.AddRange(Encoding.ASCII.GetBytes("WAVE"));
            header.AddRange(Encoding.ASCII.GetBytes("fmt "));
            header.AddRange(BitConverter.GetBytes(16));
            header.AddRange(BitConverter.GetBytes((ushort)3));
            header.AddRange(BitConverter.GetBytes((ushort)2));
            header.AddRange(BitConverter.GetBytes(48000));
            header.AddRange(BitConverter.GetBytes(384000));
            header.AddRange(BitConverter.GetBytes((ushort)4));
            header.AddRange(BitConverter.GetBytes((ushort)32));
            header.AddRange(Encoding.ASCII.GetBytes("data"));
            header.AddRange(BitConverter.GetBytes(Int32.MaxValue));
            return header;
        }
    }
}