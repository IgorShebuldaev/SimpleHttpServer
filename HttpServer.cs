using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace HttpListenerExample
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static string login = null;
        public static string password = null;
            
        public static async Task HandleIncomingConnections()
        {
            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/authorization"))
                {
                    Stream body = req.InputStream;
                    Encoding encoding = req.ContentEncoding;
                    StreamReader reader = new StreamReader(body, encoding);
      
                    Console.WriteLine("Start of client data:");
                    // Convert the data to a string and display it on the console.
                    string request = reader.ReadToEnd();
                    string[] info= request.Split('&');
                    login = info[0].Split('=')[1];
                    password = info[1].Split('=')[1];

                    if (login.Equals("admin") && password.Equals("123"))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(GetAccountPage());
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;
                        Cookie cookie = new Cookie();
                        cookie.Value = login+"&"+password;
                        resp.Cookies.Add(cookie);

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();
                    }
                    else {
                        byte[] data = Encoding.UTF8.GetBytes(GetInvalidPage());
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();
                    }

                    Console.WriteLine(request);
                    Console.WriteLine("End of client data:");
                    body.Close();
                    reader.Close();
                    // If you are finished with the request, it should be closed also.
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/account"))
                {
                    if (login != null && password != null) {
                        byte[] data = Encoding.UTF8.GetBytes(GetAccountPage());
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();
                    } else {
                        byte[] data = Encoding.UTF8.GetBytes(Get404Page());
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();
                    }
                    
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/"))
                {
                    // Write the response info
                    byte[] data = Encoding.UTF8.GetBytes(GetIndexPage());
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }   
            }
        }

        public static string GetIndexPage() {
           return File.ReadAllText(System.Environment.CurrentDirectory + "/index.html");
        }
        public static string GetAccountPage()
        {
            return File.ReadAllText(System.Environment.CurrentDirectory + "/account.html");
        }

        public static string Get404Page()
        {
            return File.ReadAllText(System.Environment.CurrentDirectory + "/404.html");
        }

        public static string GetInvalidPage()
        {
            return File.ReadAllText(System.Environment.CurrentDirectory + "/invalid.html");
        }

        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}
