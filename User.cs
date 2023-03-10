using System;


namespace Model
{
	public class User
	{
        private readonly string login;
        private readonly string password;
		public string Login
		{
			get;
		} = string.Empty;

		public string Password
		{
			get;
		} = string.Empty;

		public User(string login, string password)
		{
			this.login = login;
			this.password = password;
		}
	}
}
