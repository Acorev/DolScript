using System;
using DOL.AI.Brain;
using DOL.GS.Commands;
using DOL.GS.Geometry;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Commande /bot : interprète ce que tape le joueur et délègue à BotManager.
    /// Les ordres acceptent un numéro de bot optionnel : /bot stay 2.
    /// </summary>
    [Cmd(
        "&bot",
        ePrivLevel.Player,
        "Bot Commands",
        "/bot create - créer un bot",
        "/bot destroy - supprimer vos bots",
        "/bot list - lister vos bots (état de déplacement / agressivité)",
        "/bot stay [n] - rester sur place",
        "/bot follow [n] - vous suivre",
        "/bot comehere [n] - venir à votre position puis attendre",
        "/bot goto [n] - aller à la position de votre cible puis attendre",
        "/bot attack [n] - attaquer votre cible",
        "/bot aggressive [n] - attaque tout ennemi à portée",
        "/bot defensive [n] - attaque ce qui vous attaque",
        "/bot passive [n] - n'attaque que sur ordre")]
    public class BotCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /// <summary>
        /// Point d'entrée : args[0] = "&bot", args[1] = sous-commande, args[2] = numéro de bot (optionnel).
        /// </summary>
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

            string command = args[1].ToLowerInvariant();

            switch (command)
            {
                case "create":
                    if (BotManager.SpawnBot(player) == null)
                        DisplayMessage(client, "Impossible de créer le bot (groupe plein ?).");
                    break;

                case "destroy":
                    BotManager.DestroyBots(player);
                    break;

                case "list":
                    ListBots(client, player);
                    break;

                // --- Ordres de déplacement ---
                case "stay":
                    SendOrder(client, player, args, command, b => b.Stay());
                    break;

                case "follow":
                    SendOrder(client, player, args, command, b => b.Follow(player));
                    break;

                case "comehere":
                    SendOrder(client, player, args, command, b => b.ComeHere());
                    break;

                case "goto":
                    {
                        // Position de la cible au sol posée par le joueur
                        Coordinate destination = player.GroundTargetPosition.Coordinate;

                        if (destination.Equals(Coordinate.Nowhere))
                        {
                            DisplayMessage(client, "Posez d'abord une cible au sol.");
                            break;
                        }

                        SendOrder(client, player, args, command, b => (b as BotBrain)?.GotoGround(destination));
                        break;
                    }

                // --- Ordre d'attaque ---
                case "attack":
                    {
                        if (player.TargetObject is not GameLiving target)
                        {
                            DisplayMessage(client, "Sélectionnez d'abord une cible à attaquer.");
                            break;
                        }

                        SendOrder(client, player, args, command, b => b.Attack(target));
                        break;
                    }

                // --- Niveaux d'agressivité ---
                case "aggressive":
                    SendOrder(client, player, args, command, b => b.SetAggressionState(eAggressionState.Aggressive));
                    break;

                case "defensive":
                    SendOrder(client, player, args, command, b => b.SetAggressionState(eAggressionState.Defensive));
                    break;

                case "passive":
                    SendOrder(client, player, args, command, b => b.SetAggressionState(eAggressionState.Passive));
                    break;

                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        /// <summary>
        /// Envoie un ordre à tous les bots, ou à un seul si un numéro est donné (args[2]),
        /// puis confirme au joueur combien de bots l'ont reçu.
        /// </summary>
        private void SendOrder(GameClient client, GamePlayer player, string[] args, string label, Action<IControlledBrain> order)
        {
            int index = args.Length > 2 && int.TryParse(args[2], out int n) ? n : 0;
            int count = BotManager.Order(player, index, order);

            DisplayMessage(client, count == 0
                ? "Aucun bot concerné."
                : $"Ordre « {label} » donné à {count} bot(s).");
        }

        /// <summary>
        /// Affiche la liste numérotée des bots avec leur niveau,
        /// leur état de déplacement (Follow, Stay, ComeHere, GoTarget)
        /// et leur agressivité (Aggressive, Defensive, Passive).
        /// </summary>
        private void ListBots(GameClient client, GamePlayer player)
        {
            var bots = BotManager.GetBots(player);

            if (bots.Count == 0)
            {
                DisplayMessage(client, "Vous n'avez aucun bot.");
                return;
            }

            for (int i = 0; i < bots.Count; i++)
            {
                GameNPC bot = bots[i];
                string state = bot.Brain is IControlledBrain brain
                    ? $"{brain.WalkState} / {brain.AggressionState}"
                    : "?";

                DisplayMessage(client, $"{i + 1}. {bot.Name} (niv {bot.Level}) - {state}");
            }
        }
    }
}