using Cjo.Uwp.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Cjo.Uwp.Survey
{
    public class HttpWebServer : IWebCommand
    {
        private StreamSocketListener listener; // the socket listner to listen for TCP requests
                                               // Note: this has to stay in scope!
        private const uint BufferSize = 1024;//8192; // this is the max size of the buffer in bytes 

        private readonly string reply;
        private IComDevice comDevice;
        private Position position;
        private ReadWrite readWrite;
        private CancellationTokenSource readCancellationTokenSource;
        public string CommandString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public HttpWebServer(string reply, Position position, IComDevice comDevice, ReadWrite readWrite)
        {
            this.reply = reply;
            this.position = position;
            this.comDevice = comDevice;
            this.readWrite = readWrite;
            readCancellationTokenSource = new CancellationTokenSource();
        }

        public async void Initialise()
        {
            listener = new StreamSocketListener();
            var currentSetting = listener.Control.QualityOfService;
            listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
            
            listener.ConnectionReceived += async (sender, args) => //async 
            {
                Task task;
                // call the handle request function when a request comes in
                task = Task.Run(() => HandleRequest(sender, args));
                await task;
                task.Dispose();
            };

            // listen on port 80, this is the standard HTTP port 
            //(use a different port if you have a service already running on 80)
            await listener.BindServiceNameAsync("80");
        }

        public async void HandleRequest(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            StringBuilder request = new StringBuilder();
            string responseHTML = "<html><body>ERROR</body></html>";

            // Handle a incoming request
            // First read the request
            /*********************************** Original receiver *************/
            //using (IInputStream input = args.Socket.InputStream)
            //{ 
            //    byte[] data = new byte[BufferSize];
            //    IBuffer buffer = data.AsBuffer();
            //    uint dataRead = BufferSize;
            //    while (dataRead == BufferSize)
            //    {
            //        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial); // 
            //        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
            //        dataRead = buffer.Length;
            //    }
            //    if (input != null)
            //    {
            //        input.Dispose();
            //    }


            //}

            /************ Experimental receiver ************/

            using (Stream input = args.Socket.InputStream.AsStreamForRead())
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {                    
                    await input.AsInputStream().ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    //await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial); // 
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
                data = null;
                buffer = null;
                if (input != null)
                {
                    input.Flush();
                    input.Dispose();
                }


            }

            //var t = Task<string>.Run(() => PrepareResponse(ParseRequest(request.ToString())));
            //request.Clear();
            responseHTML = PrepareResponse(ParseRequest(request.ToString()));
            //responseHTML = t.Result;
            request = null;

            // Send a response back
            //using (IOutputStream output = args.Socket.OutputStream)
            //{

            if (responseHTML != "no")
            {
                using (Stream response = args.Socket.OutputStream.AsStreamForWrite())
                {
                    // For now we are just going to reply to anything with Hello World!
                    byte[] bodyArray = Encoding.UTF8.GetBytes(responseHTML);

                    var bodyStream = new MemoryStream(bodyArray);

                    // This is a standard HTTP header so the client browser knows the bytes returned are a valid http response
                    var header = "HTTP/1.1 200 OK\r\n" +
                                $"Content-Length: {bodyStream.Length}\r\n" +
                                    "Connection: close\r\n\r\n";

                    byte[] headerArray = Encoding.UTF8.GetBytes(header);

                    // send the header with the body included to the client
                    await response.WriteAsync(headerArray, 0, headerArray.Length);
                    //response.WriteBytes(headerArray);
                    await bodyStream.CopyToAsync(response);
                    //await response.StoreAsync();
                    await response.FlushAsync();
                    if (response != null)
                    {
                        response.Dispose();
                    }
                    //bodyStream.Dispose();

                } 
            }

                //if (output != null)
                //{
                //    output.Dispose();
                //}
                //args.Socket.Dispose();
            //}
                    
        }

        private string PrepareResponse(string request) //async Task<string>
        {
            string response = "ERROR";

            response = reply;

            if (request == "")
            {
                // this will be called when the Root (http://minwinpc/) is requested
            }
            else if (request.Contains("data"))
            {

                Task.Run(async () => await readWrite.Listen(readCancellationTokenSource.Token));
                response = "148";
            }
            else if (request.Contains("pos"))
            {
                //response = "no";
                response = $"{readWrite.MyPosition.Lat},{readWrite.MyPosition.Lon},{readWrite.MyPosition.Height}";
                // this can return when a URL that is not expected is requested
            }
            else if (request.Contains("surveystart"))
            {
                //await comDevice.FindDevice();
                Task.Run(async () => await readWrite.MessageWrite(new Tmode3Msg(200000, 360).FullMessage));
                response = "Survey Started";
            }
            else if (request.Contains("surveystatus"))
            {
                response = $"{readWrite.MySvin.Accuracy},{readWrite.MySvin.SurveyTime},{readWrite.MySvin.Active},{readWrite.MySvin.Valid}";
            }
            return response;
        }

        private string ParseRequest(string buffer)
        {
            string request = "ERROR";

            string[] tokens = buffer.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // ensure that this is a GET request
            if (tokens[0] == "GET")
            {
                request = tokens[1];
                request = request.Replace("/", "");
                request = request.ToLower();
            }

            return request;
        }

        /***************** Chaos commands *****************/
        //private RelayCommand connectCommand;
        //public RelayCommand ConnectCommand
        //{
        //    get { return connectCommand ?? (connectCommand = new RelayCommand(async () => await comDevice.FindDevice(), () => !EnableButton())); } // removed can execute: , () => EnableButton()

        //}

        //private RelayCommand readDataCommand;
        //public RelayCommand ReadDataCommand
        //{
        //    get
        //    {
        //        return readDataCommand ?? (readDataCommand = new RelayCommand(async () => await readWrite.Listen(readCancellationTokenSource.Token), () => EnableButton()));
        //    }
        //}

        //public bool EnableButton()
        //{
        //    if (comDevice.GetInputStream() == null) // selectedDevice == null
        //    {
        //        return false;
        //    }
        //    else { return true; }
        //}
    }
}
