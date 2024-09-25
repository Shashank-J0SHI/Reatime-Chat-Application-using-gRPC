using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Timers;

namespace GrpcService.Services
{
    public class ChatServices : ChatService.ChatServiceBase
    {
        private static ConcurrentDictionary<string, User> _UserList = new();
        private static ConcurrentDictionary<IServerStreamWriter<ChatMessage>, ServerCallContext> Observers = new();
        private static ConcurrentDictionary<IServerStreamWriter<UserList>, ServerCallContext> Subscribers = new();
        private static ConcurrentDictionary<string, string> AuthStore = new();

        private static System.Timers.Timer aTimer;
        private static bool isChanged = false;
        private static bool isTimer = false;

        private static void SetTimer()
        {
            aTimer = new System.Timers.Timer(2000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public override async Task<JoinResponse> join(User user, ServerCallContext context)
        {
            if (user.Name == "")
            {
                return await Task.FromResult(new JoinResponse { Error = 2, Msg = "Empty username not allowed." });
            }
            else if (!_UserList.Keys.Contains(user.Name))
            {
                isChanged = _UserList.TryAdd(user.Name, user);

                if (!isTimer)
                {
                    SetTimer();
                    isTimer = true;
                }

                var token = GenerateToken(user.Name, [user.Id]);
                AuthStore.TryAdd(token, user.Id);
                Console.WriteLine($"{user.Name}: Joined Successfully.");
                return await Task.FromResult(new JoinResponse { Error = 0, Msg = token });
            }
            else
            {
                return await Task.FromResult(new JoinResponse { Error = 1, Msg = "User Already Exist." });
            }
        }

        public override async Task getAllUsers(Empty e, IServerStreamWriter<UserList> responseStream, ServerCallContext context)
        {
            string key = context.RequestHeaders.GetValue("Authorization");
            if (!auth(key, ""))
            {
                await Task.Delay(1, context.CancellationToken);
            }

            Subscribers.TryAdd(responseStream, context);
            Console.WriteLine($"getUsers subscription created.");

            try
            {
                await Task.Delay(-1, context.CancellationToken);
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                Subscribers.TryRemove(responseStream, out context);
                Console.WriteLine($"getUsers subscription cancelled.");
            }
        }

        public override async Task<Empty> sendMsg(ChatMessage cmessage, ServerCallContext context)
        {
            string key = context.RequestHeaders.GetValue("Authorization");
            if (!auth(key, "Participant"))
            {
                return await Task.FromResult(new Empty());
            }

            await broadcastMsg(cmessage);
            return await Task.FromResult(new Empty());
        }

        public override async Task receiveMsg(ReceiveMsgRequest rmr, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
        {
            string key = context.RequestHeaders.GetValue("Authorization");
            if (!auth(key, ""))
            {
                await Task.Delay(1, context.CancellationToken);
            }

            var x = rmr.User;
            Console.WriteLine($"{x}: Receive Subscription Created. {Observers.TryAdd(responseStream, context)}");

            try
            {
                await Task.Delay(-1, context.CancellationToken);
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                Observers.TryRemove(responseStream, out context);
                User y = _UserList[x];
                Console.WriteLine($"{x}: Receive Subscription Cancelled. {_UserList.TryRemove(x, out y)}");;
                isChanged = true;
            }
        }

        private async Task broadcastMsg(ChatMessage cmessage)
        {
            foreach (var observer in Observers)
            {
                await observer.Key.WriteAsync(cmessage, observer.Value.CancellationToken);
            }
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (isChanged)
            {
                isChanged = false;
                var userlist = new UserList();
                userlist.Users.AddRange(_UserList.Values.ToArray());
                var x = Subscribers;
                foreach (var subscriber in x)
                {
                     await subscriber.Key.WriteAsync(userlist, subscriber.Value.CancellationToken);
                }
            }
        }

        private string GenerateToken(string userId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHere"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private bool auth(string key, string role)
        {
            if (role == "")
            {
                return AuthStore.Keys.Contains(key);
            }
            else
            {
                if (!AuthStore.Keys.Contains(key))
                { return false; }
                else
                {
                    return AuthStore[key] == role;
                }
            }
        }
    }
}