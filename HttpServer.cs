using System.Text;
using System.Net;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;

namespace HttpListenerExample
{   
    struct User{
        public string login;
        public string password;
        public User(string login, string password) { 
            this.login = login;
            this.password = password;
        }
    }
    class HttpServer
    {
        public static HttpListener listener;
        public const string URL = "http://localhost:8000/";
        public const string COOKIE_KEY = "localhost";

        public static async Task HandleIncomingConnections()
        {
            string page = GetPage("/index.html");
            IDictionary<string, User> users = new Dictionary<string, User>();
            
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

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/"))
                {
                    if (IsAuthorized(req))
                    {
                        resp.Redirect("/account");
                    }
                    else
                    {
                        page = GetPage("/index.html");
                    }   
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/account"))
                {
                    if (IsAuthorized(req))
                    {
                        page = GetPage("/account.html");
                    }
                    else {
                        resp.Redirect(URL);
                    } 
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/signin"))
                {
                    if (IsAuthorized(req))
                    {
                        resp.Redirect("/account");
                    }
                    else
                    {
                        page = GetPage("/signin.html");
                    }
                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/signin"))
                {
                    Stream body = req.InputStream;
                    Encoding encoding = req.ContentEncoding;
                    StreamReader reader = new(body, encoding);
                    bool found = false;

                    string request = reader.ReadToEnd();
                    string[] credential = request.Split('&');
                    string login = credential[0].Split('=')[1];
                    string password = credential[1].Split('=')[1];
                    body.Close();
                    reader.Close();

                    foreach (KeyValuePair<string, User> entry in users)
                    {
                        if (entry.Value.login.Equals(login))
                        {
                            if (entry.Value.password.Equals(password))
                            {
                                Cookie cookie = new(COOKIE_KEY, GenerateSession(login));
                                resp.Cookies.Add(cookie);
                                found = true;
                                resp.Redirect("/account");
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        resp.Redirect("/invalid");
                    }      
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/signup"))
                {
                    if (IsAuthorized(req))
                    {
                        page = GetPage("/account.html");
                    }
                    else
                    {
                        page = GetPage("/signup.html");
                    }
                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/signup"))
                {
                    Stream body = req.InputStream;
                    Encoding encoding = req.ContentEncoding;
                    StreamReader reader = new(body, encoding);

                    string request = reader.ReadToEnd();
                    string[] credential = request.Split('&');
                    string login = credential[0].Split('=')[1];
                    string password = credential[1].Split('=')[1];
                    body.Close();
                    reader.Close();

                    User newUser = new(login, password);
                    string sessionId = GenerateSession(login);

                    users.Add(sessionId, newUser);
                    Cookie cookie = new(COOKIE_KEY, sessionId);
                    resp.Cookies.Add(cookie);

                    resp.Redirect("/account");
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/logout"))
                {
                    Cookie cookie = new(COOKIE_KEY, null)
                    {
                        Expires = DateTime.Now.AddDays(-1)
                    };
                    resp.SetCookie(cookie);

                    resp.Redirect("/");
                }

                //TODO close direct access
                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/invalid"))
                {
                    page = GetPage("/invalid.html");
                }

                byte[] data = Encoding.UTF8.GetBytes(page);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                await resp.OutputStream.WriteAsync(data);
                resp.Close();
            } 
        }

        public static string GenerateSession(string login)
        {
            return DateTime.Now.ToString() + login;
        }

        public static bool IsAuthorized(HttpListenerRequest req)
        {
            return req.Cookies[COOKIE_KEY] != null;
        }   

        public static string GetPage(string path) 
        {
           return File.ReadAllText(Environment.CurrentDirectory + path);
        }

        public static void Main()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(URL);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", URL);

            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            listener.Close();
        }
    }
}
