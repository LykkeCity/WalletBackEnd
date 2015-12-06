
namespace Common
{

    public interface IMailSenderSetup
    {
        ClientSocketSetup SocketSetup { get; }
        string Password { get; }
        string FromMail { get; }
        string FromName { get; }

    }

    public class MailSenderSetup : IMailSenderSetup
    {

        public MailSenderSetup(ClientSocketSetup socketSetup, string password, string fromMail, string fromName)
        {
            FromName = fromName;
            FromMail = fromMail;
            Password = password;
            SocketSetup = socketSetup;
        }

        public ClientSocketSetup SocketSetup { get; }
        public string Password { get; }
        public string FromMail { get; }
        public string FromName { get; }


        private static void ParseFrom(string data, out string mail, out string name)
        {
            var from = data.IndexOf('<');

            if (from <= 0)
            {
                mail = data;
                name = string.Empty;
                return;
            }


            var to = data.IndexOf('>');

            if (to <= 0)
                to = data.Length;

            mail = data.Substring(0, from);
            name = data.Substring(from + 1, to - from - 1);
        }

        public static MailSenderSetup Parse(string connectionString)
        {

            ClientSocketSetup socketSetup = null;
            var password = string.Empty;
            var fromMail = string.Empty;
            var fromName = string.Empty;

            var strings = connectionString.Split(';');

            foreach (var str in strings)
            {

                var pair =  str.Split('=');

                switch (pair[0].ToLower())
                {
                    case  "host":
                        socketSetup = ClientSocketSetup.ParseHostPort(pair[1]);
                        break;

                    case "from":
                        ParseFrom(pair[1], out fromMail, out fromName);
                        break;

                    case "password":
                        password = pair[1];
                        break;
                }


            }

            return new MailSenderSetup(socketSetup, password, fromMail, fromName);


        }
    }


}
