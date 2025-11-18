using Discord.WebSocket;

namespace Notifier
{
    public static class Permissions
    {
        public static bool IsBotAdminstrator(SocketUser socketUser) => socketUser.Id.Equals(Globals.GetAs<ulong>("HostUserId"));
    }
}