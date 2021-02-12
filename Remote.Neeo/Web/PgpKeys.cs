using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Remote.Neeo.Web
{
    internal sealed class PgpKeys
    {
        public PgpKeys() => (this.PrivateKey, this.PublicKey) = PgpMethods.GenerateKeys();

        public PgpPrivateKey PrivateKey { get; }

        public PgpPublicKey PublicKey { get; }
    }
}
