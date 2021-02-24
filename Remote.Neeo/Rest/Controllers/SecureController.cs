using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Remote.Neeo.Rest.Controllers
{
    [ApiController, Route("[controller]")]
    internal sealed class SecureController : ControllerBase
    {
        private readonly string _publicKey;

        public SecureController(PgpKeys keys) => this._publicKey = Encoding.ASCII.GetString(PgpMethods.GetKeyBytes(keys.PublicKey.Encode));

        [HttpGet("pubkey")]
        public ActionResult<object> GetPublicKey() => new(new { Publickey = this._publicKey });
    }
}