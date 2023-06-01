using FamilyLocator.Api.Controllers;
using FamilyLocator.DataLayer.DataBase;
using FamilyLocator.DataLayer.DataBase.Entities;
using FamilyLocator.Models;
using FamilyLocator.Models.Requests;
using FamilyLocator.Models.Responses;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FamilyLocator.Api.UDP
{
    public class UdpReciever
    {
        private readonly UdpClient _udpClient;
        private IPEndPoint _udpEndPoint;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IHubContext<CoordinatesHub, ICoordinates> _coordinateHub;
        private List<XIdentityUser> _userCache = new();
        public UdpReciever(IHubContext<CoordinatesHub, ICoordinates> coordinateHub, IServiceScopeFactory scopeFactory)
        {
            _coordinateHub = coordinateHub;
            this.scopeFactory = scopeFactory;
            _udpClient = new UdpClient(24446);
            //_udpClient.Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpEndPoint = null;
        }

        public void Init()
        {
            Console.WriteLine("UDP init");
            new Thread(() => Start()).Start();
        }

        public async void Start()
        {
            Console.WriteLine("UDP Listening on 0.0.0.0:24446");
            while (true)
            {

                if (_udpClient.Receive(ref _udpEndPoint) is { } result)
                {
                    try
                    {
                        //Console.WriteLine("Received");
                        var buffer = Encoding.UTF8.GetString(result);
                        var data = JsonConvert.DeserializeObject<Coordinates>(buffer);
                        if (data != null)
                        {
                            //Console.WriteLine(data.UserId);
                            await Callback(data);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        public async Task Callback(Coordinates data)
        {
            using (var dbContext = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApiDbContext>())
            {
                var user = dbContext.Users.Include(x => x.Family).ThenInclude(x=>x.Users).Include(x => x.Coordinates).ToList().FirstOrDefault(x => x.Id == data.UserId || x.FireBaseId == data.UserId);

                if (user == null) return;
                var nuser = await dbContext.Users.FindAsync(user.Id);
                var coord = new XUCoordinates()
                {
                    Battery = data.Battery,
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    Time = DateTime.UtcNow,
                    Speed = data.Speed,
                    User = nuser
                };

                dbContext.XUCoordinates.Add(coord);
                //dbContext.Update(user);
                await dbContext.SaveChangesAsync();
                Task.Run(() => CheckZone(user, coord));
                if (string.IsNullOrWhiteSpace(user.Family?.Id))
                {
                    return;
                }

                await _coordinateHub.Clients.Users(user.Family.Users.Where(x => x.Id != user.Id).Select(x => x.Id)).Coordinates(data);
                //await _coordinateHub.Clients.Group(user.Family.Name).Coordinates(data);
                //Console.WriteLine($"Received {JsonConvert.SerializeObject(data)}");
            }
        }

        public async Task CheckZone(XIdentityUser user, XUCoordinates xuc)
        {
            using var dbContext = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApiDbContext>();
            var family = dbContext.Families.Include(x => x.Zones).First(x => x.Id == user.Family.Id);
            var polygons = family.Zones.Select(zone =>
            {
                GraphicsPath polygon = new GraphicsPath();
                polygon.AddPolygon(zone.Points.ToArray());
                return (polygon, zone.ZoneType);
            });
            var tokens = dbContext.Users.Find(user.Id).FirePushTokens.Select(x => x.Token).ToList();
            MulticastMessage message;
            switch (user.CurrentZoneType)
            {
                case ZoneType.Safe:
                    {
                        if (polygons.Any(x =>
                                x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                                x.ZoneType == ZoneType.Safe))
                        {
                            break;
                        }
                        if (polygons.Any(x =>
                                x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                                x.ZoneType == ZoneType.Danger))
                        {
                            await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Danger });
                            message = new MulticastMessage()
                            {
                                Tokens = tokens,
                                Data = new Dictionary<string, string>()
                                {
                                    {"title", "Пользователь перешел в другую зону"},
                                    {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Danger} зону"},
                                },
                                Notification = new Notification()
                                {
                                    Title = "Пользователь перешел в другую зону",
                                    Body = $"Пользователь {user.UserName} перешел в {ZoneType.Danger} зону"
                                }
                            };
                            await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                            user.CurrentZoneType = ZoneType.Danger;
                            break;
                        }
                        await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Else });
                        message = new MulticastMessage()
                        {
                            Tokens = tokens,
                            Data = new Dictionary<string, string>()
                            {
                                {"title", "Пользователь перешел в другую зону"},
                                {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Else} зону"},
                            },
                            Notification = new Notification()
                            {
                                Title = "Пользователь перешел в другую зону",
                                Body = $"Пользователь {user.UserName} перешел в {ZoneType.Else} зону"
                            }
                        };
                        await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                        user.CurrentZoneType = ZoneType.Else;
                        break;
                    }
                case ZoneType.Danger:
                    if (polygons.Any(x =>
                            x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                            x.ZoneType == ZoneType.Safe))
                    {
                        await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Safe });
                        message = new MulticastMessage()
                        {
                            Tokens = tokens,
                            Data = new Dictionary<string, string>()
                            {
                                {"title", "Пользователь перешел в другую зону"},
                                {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Safe} зону"},
                            },
                            Notification = new Notification()
                            {
                                Title = "Пользователь перешел в другую зону",
                                Body = $"Пользователь {user.UserName} перешел в {ZoneType.Safe} зону"
                            }
                        };
                        await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                        user.CurrentZoneType = ZoneType.Safe;
                        break;
                    }
                    if (polygons.Any(x =>
                            x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                            x.ZoneType == ZoneType.Danger))
                    {
                        break;
                    }
                    await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Else });
                    message = new MulticastMessage()
                    {
                        Tokens = tokens,
                        Data = new Dictionary<string, string>()
                        {
                            {"title", "Пользователь перешел в другую зону"},
                            {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Else} зону"},
                        },
                        Notification = new Notification()
                        {
                            Title = "Пользователь перешел в другую зону",
                            Body = $"Пользователь {user.UserName} перешел в {ZoneType.Else} зону"
                        }
                    };
                    await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                    user.CurrentZoneType = ZoneType.Else;
                    break;
                case ZoneType.Else:
                    if (polygons.Any(x =>
                            x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                            x.ZoneType == ZoneType.Safe))
                    {
                        await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Safe });
                        message = new MulticastMessage()
                        {
                            Tokens = tokens,
                            Data = new Dictionary<string, string>()
                            {
                                {"title", "Пользователь перешел в другую зону"},
                                {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Safe} зону"},
                            },
                            Notification = new Notification()
                            {
                                Title = "Пользователь перешел в другую зону",
                                Body = $"Пользователь {user.UserName} перешел в {ZoneType.Safe} зону"
                            }
                        };
                        await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                        user.CurrentZoneType = ZoneType.Safe;
                        break;
                    }
                    if (polygons.Any(x =>
                            x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                            x.ZoneType == ZoneType.Danger))
                    {
                        await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Danger });
                        message = new MulticastMessage()
                        {
                            Tokens = tokens,
                            Data = new Dictionary<string, string>()
                            {
                                {"title", "Пользователь перешел в другую зону"},
                                {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Danger} зону"},
                            },
                            Notification = new Notification()
                            {
                                Title = "Пользователь перешел в другую зону",
                                Body = $"Пользователь {user.UserName} перешел в {ZoneType.Danger} зону"
                            }
                        };
                        await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                        user.CurrentZoneType = ZoneType.Danger;
                        break;
                    }
                    break;
                default:
                    if (polygons.Any(x =>
                            x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                            x.ZoneType == ZoneType.Safe))
                    {
                        await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Safe });
                        message = new MulticastMessage()
                        {
                            Tokens = tokens,
                            Data = new Dictionary<string, string>()
                            {
                                {"title", "Пользователь перешел в другую зону"},
                                {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Safe} зону"},
                            },
                            Notification = new Notification()
                            {
                                Title = "Пользователь перешел в другую зону",
                                Body = $"Пользователь {user.UserName} перешел в {ZoneType.Safe} зону"
                            }
                        };
                        await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                        user.CurrentZoneType = ZoneType.Safe;
                        break;
                    }
                    if (polygons.Any(x =>
                            x.polygon.IsVisible(new PointF((float)xuc.Latitude, (float)xuc.Longitude)) &&
                            x.ZoneType == ZoneType.Danger))
                    {
                        await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Danger });
                        message = new MulticastMessage()
                        {
                            Tokens = tokens,
                            Data = new Dictionary<string, string>()
                            {
                                {"title", "Пользователь перешел в другую зону"},
                                {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Danger} зону"},
                            },
                            Notification = new Notification()
                            {
                                Title = "Пользователь перешел в другую зону",
                                Body = $"Пользователь {user.UserName} перешел в {ZoneType.Danger} зону"
                            }
                        };
                        await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                        user.CurrentZoneType = ZoneType.Danger;
                        break;
                    }
                    await _coordinateHub.Clients.Group(family.Id).ZoneChanged(new NewZone { UserId = user.Id, ZoneType = ZoneType.Else });
                    message = new MulticastMessage()
                    {
                        Tokens = tokens,
                        Data = new Dictionary<string, string>()
                        {
                            {"title", "Пользователь перешел в другую зону"},
                            {"body", $"Пользователь {user.UserName} перешел в {ZoneType.Else} зону"},
                        },
                        Notification = new Notification()
                        {
                            Title = "Пользователь перешел в другую зону",
                            Body = $"Пользователь {user.UserName} перешел в {ZoneType.Else} зону"
                        }
                    };
                    await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                    user.CurrentZoneType = ZoneType.Else;
                    break;
            }
            dbContext.Update(user);
            await dbContext.SaveChangesAsync();
        }

    }
}
