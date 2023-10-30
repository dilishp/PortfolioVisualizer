using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:6666");
var app = builder.Build();

app.UseRouting();
app.UseWebSockets();
app.Map("/ws", async context =>
{
    try
    {
        ArgumentNullException.ThrowIfNull(nameof(context));

        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var rand = new Random();

            string[] stockList = { "S1", "S2", "S3", "S4", "S5", "S6", "S7" };
            long[] tickPrice = { 1000, 450, 560, 980, 1020, 2400, 3600 };


            while (true)
            {
                long r = rand.NextInt64(-10, 10);
                long stockIndex = (r % stockList.Count());
                tickPrice[stockIndex] = tickPrice[stockIndex] + r;
                byte[] data = Encoding.ASCII.GetBytes($"{stockList[stockIndex]} : {tickPrice[stockIndex]}");
                await webSocket.SendAsync(data, WebSocketMessageType.Text,
                    true, CancellationToken.None);

                await Task.Delay(1000);
                //if (r == 7)
                //{
                //    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                //        "random closing", CancellationToken.None);

                //    return;
                //}
            }
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
    catch(ArgumentNullException ex) when (ex.InnerException != null)
    {

    }
    catch  (HttpRequestException hexp) when(hexp.StatusCode == HttpStatusCode.Accepted)
    {

    }
    catch(Exception e) 
    {

    }
});

await app.RunAsync();


