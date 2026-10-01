using System;
using DOL.Events;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Écoute les events du serveur liés aux joueurs.
    /// Rôle actuel : supprimer les bots d'un joueur quand il quitte ou perd la connexion.
    /// </summary>
    public class BotaVent
    {
        /// <summary>
        /// Appelée automatiquement au chargement des scripts : branche les handlers.
        /// </summary>
        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.AddHandler(GamePlayerEvent.Quit, OnPlayerQuit);
            GameEventMgr.AddHandler(GamePlayerEvent.Linkdeath, OnPlayerQuit);
        }

        /// <summary>
        /// Appelée au déchargement des scripts : débranche les handlers
        /// (évite les doublons en cas de rechargement).
        /// </summary>
        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GamePlayerEvent.Quit, OnPlayerQuit);
            GameEventMgr.RemoveHandler(GamePlayerEvent.Linkdeath, OnPlayerQuit);
        }

        /// <summary>
        /// Déclenchée quand un joueur quitte (Quit) ou est déconnecté brutalement (Linkdeath).
        /// Supprime tous ses bots.
        /// </summary>
        private static void OnPlayerQuit(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is GamePlayer player)
                BotManager.DestroyBots(player);
        }
    }
}