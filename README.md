# Pecan
A lightweight web server for Meadow F7. The aim is to reproduce the features of [Maple](https://github.com/WildernessLabs/Meadow.Foundation/tree/main/Source/Meadow.Foundation.Libraries_and_Frameworks/Web.Maple), with emphasis on API design and performance.

```c#
await Device.InitWiFiAdapter();
await Device.WiFiAdapter.Connect("[SSID]", "[PASSWORD]");

var server = new WebServerBuilder()
    .ListenOn(Device.WiFiAdapter.IpAddress)
    .MapGet("/hi", _ => "Hello, Pecan!")
    .Build();

Console.WriteLine($"Pecan listening at: http://{Device.WiFiAdapter.IpAddress}/hi");

await server.RunAsync();
```