using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Remote.Neeo.Server
{
    public class PgpMiddleware
    {
        private readonly PgpKeys _keys;
        private readonly RequestDelegate _next;

        public PgpMiddleware(RequestDelegate next, PgpKeys keys)
        {
            this._next = next;
            this._keys = keys;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (MemoryStream clone = new())
            {
                await context.Request.Body.CopyToAsync(clone).ConfigureAwait(false);

                if (clone.Length != 0)
                {
                    clone.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = PgpMethods.Decrypt(clone, this._keys.PrivateKey);
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}
