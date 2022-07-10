using NAudio.Wave;
using System.IO;
using System.Net;
using System.Text;

namespace audioStream
{
    internal class Program
    {
        static MemoryStream audioBuf = new();
        static object audioStreamLock = new object();
        static AutoResetEvent audioDataEvent = new(true);

        static Stream clientS;

        static void Main(string[] args)
        {
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
            Directory.CreateDirectory(outputFolder);
            var outputFilePath = Path.Combine("S:\\", "recorded.wav");
            var capture = new WasapiLoopbackCapture();
            // optionally we can set the capture waveformat here: e.g. capture.WaveFormat = new WaveFormat(44100, 16,2);
            //var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);
            //var rawWriter = new FileStream("S:\\raw.wav", FileMode.Create);
            Console.WriteLine(capture.WaveFormat.SampleRate);
            Console.WriteLine(capture.WaveFormat.Channels);
            Console.WriteLine(capture.WaveFormat.BitsPerSample);
            capture.DataAvailable += (s, a) =>
            {
                //writer.Write(a.Buffer, 0, a.BytesRecorded);
                //rawWriter.Write(a.Buffer, 0, a.BytesRecorded);
                //Console.WriteLine("[WAV] Got " + a.BytesRecorded);
                lock (audioStreamLock)
                {
                    if (clientS != null)
                    {
                        clientS.Write(a.Buffer, 0, a.BytesRecorded);
                    }
                    audioBuf = new MemoryStream(a.BytesRecorded);
                    audioBuf.Write(a.Buffer, 0, a.BytesRecorded);
                    audioDataEvent.Set();
                    audioDataEvent.Reset();
                }
                //if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 10)
                //{
                //    capture.StopRecording();
                //}
            };

            capture.RecordingStopped += (s, a) =>
            {
                //writer.Dispose();
                //writer = null;
                capture.Dispose();
            };

            new Thread(() => { RunServer(); }).Start();

            capture.StartRecording();
            while (capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
            {
                Thread.Sleep(500);
            }

            Console.WriteLine("Ended");
            Console.ReadKey();
        }

        static void RunServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Prefixes.Add("http://192.168.1.102:8080/");
            listener.Start();
            var fs = new FileStream("S:\\a.wav", FileMode.Open);
            Console.WriteLine(fs.Length);
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


                    //try
                    //{
                    //    fs.Seek(0, SeekOrigin.Begin);
                    //    fs.CopyTo(response.OutputStream);
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine(e);
                    //}
                    var header = RiffHeader((int)audioBuf.Length).ToArray();
                    response.OutputStream.Write(header, 0, header.Length);
                    clientS = response.OutputStream;
                    continue;
                    while (true)
                    {
                        audioDataEvent.WaitOne();
                        lock (audioStreamLock)
                        {
                            var buf = new byte[audioBuf.Length];
                            audioBuf.GetBuffer().CopyTo(buf, 0);
                            try
                            {
                                
                                //Console.WriteLine("Header size: " + header.Length);
                                //Console.WriteLine(BitConverter.ToString(header));

                                
                                response.OutputStream.Write(buf);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[HTTP] Cannot write to client stream");
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                                break;
                            }
                        }
                    }
                    try { response.OutputStream.Close(); }
                    catch { }
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

        static List<byte> RiffHeader2(int size)
        {
            List<byte> header = new List<byte>();
            header.AddRange(Encoding.ASCII.GetBytes("RIFF"));
            header.AddRange(BitConverter.GetBytes(size + 36).Reverse().ToArray());
            header.AddRange(Encoding.ASCII.GetBytes("WAVE"));
            header.AddRange(Encoding.ASCII.GetBytes("fmt "));
            header.AddRange(BitConverter.GetBytes(16).Reverse().ToArray());
            header.AddRange(BitConverter.GetBytes((short)1).Reverse().ToArray());
            header.AddRange(BitConverter.GetBytes((short)2).Reverse().ToArray());
            header.AddRange(BitConverter.GetBytes(44100).Reverse().ToArray());
            header.AddRange(BitConverter.GetBytes(176400).Reverse().ToArray());
            header.AddRange(BitConverter.GetBytes((short)4).Reverse().ToArray());
            header.AddRange(BitConverter.GetBytes((short)16).Reverse().ToArray());
            header.AddRange(Encoding.ASCII.GetBytes("data"));
            header.AddRange(BitConverter.GetBytes(size).Reverse().ToArray());
            return header;
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