using Auth.API.Interfaces;

namespace Auth.API.Logic
{
    public class SecretGenerator : ISecretGenerator
    {
        private readonly static Random random = new Random();

        public string Generate128CharSecret()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 128)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
