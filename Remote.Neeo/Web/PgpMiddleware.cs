using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Remote.Neeo.Web
{
    internal sealed class PgpMiddleware
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
                if (clone.Length != 0L)
                {
                    clone.Position = 0L;
                    context.Request.Body = PgpMethods.Decrypt(clone, this._keys.PrivateKey);
                }
            }
            await this._next(context).ConfigureAwait(false);
        }
    }
}
