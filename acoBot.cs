using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Commands;


namespace DOL.GS.Scripts
{
    [Cmd(
       "&bot",
       ePrivLevel.Player,
       "Bot Commands",
       "/bot create - créer un bot",
       "/bot destroy - supprimer vos bots")]
    public class AcoBot : AbstractCommandHandler, ICommandHandler
    {
        private const int MAX_MBR = 3;

        // Bots actifs par joueur
        private static readonly Dictionary<GamePlayer, List<GameNPC>> _bots = [];
        private static readonly object _lock = new();

        public static GameNPC SpawnBot(GamePlayer player)
        {
            GameNPC bot = new()
            {
                Name = "Bot Test",
                Model = 40,
                Level = player.Level,
                Realm = player.Realm,
                Orientation = player.Orientation,
                CurrentRegionID = player.CurrentRegionID,
                Position = player.Position.With(
                x: player.Position.X + 60,
                y: player.Position.Y + 60,
                z: player.Position.Z),
            };

            if (!AddToGroup(player, bot))
                return null;

            FollowOwnerBrain brain = new(player);
            bot.SetOwnBrain(brain);
            brain.Start();

            bot.AddToWorld();

            lock (_lock)
            {
                if (!_bots.TryGetValue(player, out var list))
                    _bots[player] = list = [];
                list.Add(bot);
            }

            return bot;
        }

        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            if (player == null)
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLowerInvariant())
            {
                case "create":
                    SpawnBot(player);
                    break;

                case "destroy":
                    DestroyBots(player);
                    break;


                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        private static bool AddToGroup(GamePlayer player, GameNPC bot)
        {
            if (player.Group == null)
            {
                var group = new Group(player);

                if (!GroupMgr.AddGroup(group))
                    return false;

                group.AddMember(player);
            }
            return player.Group.GetMembersInTheGroup().Count < MAX_MBR && player.Group.AddMember(bot);
        }

        public void DestroyBots(GamePlayer player)
        {
            List<GameNPC> list;

            lock (_lock)
            {
                if (!_bots.Remove(player, out list))
                    return;
            }

            foreach (GameNPC bot in list)
            {
                // 1. sortir du groupe
                Group group = player.Group;
                if (group != null && group.IsInTheGroup(bot))
                    group.RemoveMember(bot);

                // 2. arrêter l'IA
                bot.StopAttack();
                bot.Brain?.Stop();

                // 3. retirer du monde
                bot.Delete();
            }
        }
    }
}