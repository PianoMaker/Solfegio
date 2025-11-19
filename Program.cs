app.Use(async (context, next) =>
{
    await next();
    var ct = context.Response.ContentType;
    if (!string.IsNullOrEmpty(ct) && ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase) && !ct.Contains("charset", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.ContentType = ct + "; charset=utf-8";
    }
});