namespace Projeto_SEGUES.Models.Payment
{
    public class StripeSettings
    {
        public required string SecretKey { get; set; }
        public required string PublicKey { get; set; }
    }
}
