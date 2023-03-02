using System.Text;
using System.Net;
using System;

namespace HttpListenerExample
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static string login = "admin";
        public static string password = "123";

        public static async Task HandleIncomingConnections()
        {
            string page = getPage("/index.html");

            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/authorization"))
                {
                    Stream body = req.InputStream;
                    Encoding encoding = req.ContentEncoding;
                    StreamReader reader = new StreamReader(body, encoding);

                    Console.WriteLine("Start of client data:");
                    string request = reader.ReadToEnd();
                    string[] info = request.Split('&');
                    login = info[0].Split('=')[1];
                    password = info[1].Split('=')[1];
                    Console.WriteLine(request);
                    Console.WriteLine("End of client data:");
                    body.Close();
                    reader.Close();

                    if (login.Equals("admin") && password.Equals("123"))
                    {
                        Cookie cookie = new Cookie("admin", "adminauth");
                        resp.Cookies.Add(cookie);

                        page = getPage("/account.html");
                        resp.Redirect(url+"account");
                    }
                    else
                    {
                        page = getPage("/invalid.html");
                    }
                }

      
                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/account"))
                {
                    if (isAuthorized(req))
                    {
                        page = getPage("/account.html");
                        resp.Redirect(url + "account");
                    }
                    else {
                        resp.Redirect(url);
                    } 
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/"))
                {
                    page = getPage("/index.html");
                }
                

                byte[] data = Encoding.UTF8.GetBytes(page);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            } 
        }

        public static bool isAuthorized(HttpListenerRequest req)
        {
            if (req.Cookies["admin"] != null)
            {
                return true;
            }

            return false;
        }   

        public static string getPage(string path) 
        {
           return File.ReadAllText(Environment.CurrentDirectory + path);
        }

        public static void Main(string[] args)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            listener.Close();
        }
    }
}
