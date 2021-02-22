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
            (this._next, this._keys) = (next, keys);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post && context.Request.Headers[Constants.SecureHeader].Equals("true"))
            {
                using MemoryStream clone = new();
                await context.Request.Body.CopyToAsync(clone).ConfigureAwait(false);
                if (clone.Length != default)
                {
                    clone.Position = default;
                    context.Request.Body = PgpMethods.Decrypt(clone, this._keys.PrivateKey);
                }
            }
            await this._next(context).ConfigureAwait(false);
        }

        private static class Constants
        {
            public const string SecureHeader = "x-neeo-secure";
        }
    }
}
