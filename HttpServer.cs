using System.Text;
using System.Net;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;

namespace HttpListenerExample
{
    class HttpServer
    {
        public static HttpListener listener;
        public const string URL = "http://localhost:8000/";
        public const string COOKIE_KEY = "localhost";

        public static async Task HandleIncomingConnections()
        {
            string page = GetPage("/index.html");
            
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

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/login"))
                {   if (IsAuthorized(req))
                    {
                        resp.Redirect("account");
                    }
                    else {
                        page = GetPage("/login.html");
                    }              
                }

                //TODO close direct access
                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/invalid"))
                {
                    page = GetPage("/invalid.html"); 
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

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/logout"))
                {
                    Cookie cookie = new(COOKIE_KEY, null)
                    {
                        Expires = DateTime.Now.AddDays(-1)
                    };
                    resp.SetCookie(cookie);

                    resp.Redirect("/");
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/form_regestration"))
                {
                    if (IsAuthorized(req))
                    {
                        page = GetPage("/account.html");
                    }
                    else
                    {
                        page = GetPage("/regestration.html");
                    }
                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/regestration"))
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

                    Model.User newUser = new(login, password);
                    string sessionId = GenerateSession(login);

                    Database.TableUsers.users.Add(sessionId, newUser);
                    Cookie cookie = new(COOKIE_KEY, sessionId);
                    resp.Cookies.Add(cookie);

                    resp.Redirect("/account");
                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/authorization"))
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

                    foreach (KeyValuePair<string, Model.User> entry in Database.TableUsers.users)
                    {
                        if (entry.Value.Login.Equals(login))
                        {
                            if (entry.Value.Password.Equals(password))
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
