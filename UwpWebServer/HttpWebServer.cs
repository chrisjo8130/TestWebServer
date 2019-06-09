using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace UwpWebServer
{
    public sealed class HttpWebServer 
    {
        private StreamSocketListener listener; 
        private const uint BufferSize = 1024; // Increase size of buffer according to web page to be served
        private readonly string reply;        
        public string CommandString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        // object for counter lock
        private readonly object o = new object();
        // counter for number of web requests
        private int counter = 0;
        public int Counter
        {
            get
            {
                return counter;
            }
            set
            {
                lock (o)
                {
                    counter = value;
                };
            }
        }

        /// <summary>
        /// Opens a port and listens for http GET request
        /// </summary>
        /// <param name="reply">String containing the Http web page to be loaded</param>
        public HttpWebServer(string reply)
        {
            this.reply = reply;
            Counter = 0;
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
            await listener.BindServiceNameAsync("5000");
        }

        public async void HandleRequest(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            StringBuilder request = new StringBuilder();
            string responseHTML = "<html><body>ERROR</body></html>";
            Counter++;

            // Handle a incoming request
            // First read the request 
            using (IInputStream input = args.Socket.InputStream)
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            responseHTML = PrepareResponse(ParseRequest(request.ToString()));
 
            request = null;

            if (responseHTML != "no")
            {
                using (IOutputStream response = args.Socket.OutputStream)
                {
                    // Encodes the response string with UTF and converts it to a Byte array
                    byte[] bodyArray = Encoding.UTF8.GetBytes(responseHTML);

                    // Standard HTTP header for valid http response
                    var header = "HTTP/1.1 200 OK\r\n" +
                                $"Content-Length: {bodyArray.Length}\r\n" +
                                    "Connection: close\r\n\r\n";
  
                    var headerBuffer = Windows.Security.Cryptography
                        .CryptographicBuffer.CreateFromByteArray(Encoding.UTF8.GetBytes(header));
                    
                    // write the header and the body to the IOutputStream
                    await response.WriteAsync(headerBuffer);
                    await response.WriteAsync(Windows.Security.Cryptography
                        .CryptographicBuffer.CreateFromByteArray(bodyArray));                    
                    await response.FlushAsync();
                }
            }
        }

        private string PrepareResponse(string request) 
        {
            string response = "ERROR";

            response = reply;

            if (request == "")
            {
                // root call
            }
            else if (request.Contains("data"))
            {
                response = "148";
            }
            else if (request.Contains("pos"))
            {
                response = $"{Counter},2345678.8,123456.77,1234567899.0,2345353546.8";
                
                 // this can return when a URL that is not expected is requested
            }
            else if (request.Contains("surveystart"))
            { 
                response = "Survey Started";
            }
            else if (request.Contains("surveystatus"))
            {

            }
            return response;
        }

        /// <summary>
        /// Checks if it is a GET request. If not returns Error
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private string ParseRequest(string buffer)
        {
            string request = "ERROR";

            string[] tokens = buffer.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // checks if it is a GET request
            if (tokens[0] == "GET")
            {
                request = tokens[1];
                request = request.Replace("/", "");
                request = request.ToLower();
            }

            return request;
        }

    }
}
