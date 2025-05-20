namespace Auth.API.Interfaces
{
    public interface ISecretGenerator
    {
        public string Generate128CharSecret();
    }
}
