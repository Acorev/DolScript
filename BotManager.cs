using System;
using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Gère le cycle de vie des bots : création, suivi par joueur,
    /// ordres (stay / follow) et destruction.
    /// Classe statique : utilisable partout (commandes, events) sans instance.
    /// </summary>
    public static class BotManager
    {
        // Nombre maximum de membres dans le groupe (joueur inclus)
        private const int MAX_MBR = 3;

        // Bots actifs, classés par joueur propriétaire
        private static readonly Dictionary<GamePlayer, List<GameNPC>> _bots = [];

        // Bots auxquels on a donné l'ordre de rester sur place (cerveau arrêté)
        private static readonly HashSet<GameNPC> _staying = [];

        // Verrou protégeant _bots et _staying (accès depuis plusieurs threads)
        private static readonly Lock _lock = new();

        /// <summary>
        /// Crée un bot à côté du joueur, l'ajoute à son groupe,
        /// lui donne un cerveau "suiveur" et le place dans le monde.
        /// Retourne null si le groupe est plein ou si la création échoue.
        /// </summary>
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
                // Décalé de 60 unités pour ne pas apparaître sur le joueur
                Position = player.Position.With(
                    x: player.Position.X + 60,
                    y: player.Position.Y + 60,
                    z: player.Position.Z),
            };

            // Si le groupe est plein ou impossible à créer, on abandonne
            if (!AddToGroup(player, bot))
                return null;

            // Le cerveau suit le propriétaire et respecte les ordres stay / follow
            BotBrain brain = new(player);
            bot.SetOwnBrain(brain);
            brain.Start();

            bot.AddToWorld();

            // Enregistre le bot dans la liste du joueur
            lock (_lock)
            {
                if (!_bots.TryGetValue(player, out var list))
                    _bots[player] = list = [];
                list.Add(bot);
            }

            return bot;
        }

        /// <summary>
        /// Retourne une copie de la liste des bots du joueur
        /// (copie = on peut la parcourir sans risque pendant que d'autres threads la modifient).
        /// </summary>
        public static List<GameNPC> GetBots(GamePlayer player)
        {
            lock (_lock)
            {
                return _bots.TryGetValue(player, out var list) ? [.. list] : [];
            }
        }

        /// <summary>
        /// Applique un ordre aux bots du joueur.
        /// index = 0 : tous les bots ; index = n : uniquement le bot n°n (numérotation de /bot list).
        /// Retourne le nombre de bots qui ont reçu l'ordre.
        /// </summary>
        public static int Order(GamePlayer player, int index, Action<IControlledBrain> order)
        {
            List<GameNPC> bots = GetBots(player);
            int count = 0;

            for (int i = 0; i < bots.Count; i++)
            {
                if (index > 0 && index != i + 1)
                    continue;

                if (bots[i].Brain is IControlledBrain brain)
                {
                    order(brain);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Supprime tous les bots du joueur : sortie du groupe, arrêt de l'IA,
        /// retrait du monde. Appelée par /bot destroy et par les events de déconnexion.
        /// Peut être appelée plusieurs fois sans danger (ne fait rien s'il n'y a plus de bots).
        /// </summary>
        public static void DestroyBots(GamePlayer player)
        {
            List<GameNPC> list;

            // Le lock reste : il protège le dictionnaire _bots
            lock (_lock)
            {
                if (!_bots.Remove(player, out list))
                    return;
            }

            Group group = player.Group;

            foreach (GameNPC bot in list)
            {
                // 1. sortir du groupe
                if (group != null && group.IsInTheGroup(bot))
                    group.RemoveMember(bot);

                // 2. arrêter l'IA
                bot.StopAttack();
                bot.Brain?.Stop();

                // 3. retirer du monde
                bot.Delete();
            }
        }

        /// <summary>
        /// Ajoute le bot au groupe du joueur (crée le groupe s'il n'existe pas).
        /// Retourne false si le groupe est plein ou si l'ajout échoue.
        /// </summary>
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
    }
}