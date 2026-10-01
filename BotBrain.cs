using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Geometry;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Cerveau des bots : comme FollowOwnerBrain (suit son propriétaire, passif par défaut),
    /// mais respecte les ordres du joueur et défend son propriétaire en mode défensif.
    /// Hérite de ControlledNpcBrain : Stay(), Follow(), ComeHere(), Goto(), Attack(),
    /// SetAggressionState(), WalkState.
    /// </summary>
    public class BotBrain(GameLiving owner) : FollowOwnerBrain(owner)
    {

        /// <summary>
        /// FollowOwnerBrain force le suivi (et un StopAttack) à chaque Think().
        /// Ici on ne suit que si l'ordre actuel est "Follow" et si le bot n'est pas en train
        /// d'attaquer : sinon les ordres stay / comehere / goto / attack seraient écrasés.
        /// </summary>
        public override void FollowOwner()
        {
            if (WalkState != eWalkState.Follow)
                return;

            if (Body.AttackState)
                return;

            base.FollowOwner();
        }

        /// <summary>
        /// Ordre "goto" vers une position précise (cible au sol) :
        /// le bot s'y rend puis attend sur place (état GoTarget).
        /// </summary>
        public void GotoGround(Coordinate destination)
        {
            tempPosition = Body.Coordinate;
            WalkState = eWalkState.GoTarget;
            Body.StopFollowing();
            Body.PathTo(destination, Body.MaxSpeed);
        }

        /// <summary>
        /// Appelée quand le propriétaire est attaqué. La version d'origine ignore l'attaque
        /// si ce brain n'est pas le "familier principal" du joueur, ce qui est le cas des bots.
        /// Ici : le bot ajoute l'attaquant à sa liste d'ennemis et riposte
        /// (AttackMostWanted ne fait rien en mode passif).
        /// </summary>
        protected override void OnOwnerAttacked(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs args)
                return;

            switch (args.AttackData.AttackResult)
            {
                case GameLiving.eAttackResult.Blocked:
                case GameLiving.eAttackResult.Evaded:
                case GameLiving.eAttackResult.Fumbled:
                case GameLiving.eAttackResult.HitStyle:
                case GameLiving.eAttackResult.HitUnstyled:
                case GameLiving.eAttackResult.Missed:
                case GameLiving.eAttackResult.Parried:
                    AddToAggroList(
                        args.AttackData.Attacker,
                        args.AttackData.Attacker.EffectiveLevel + args.AttackData.Damage + args.AttackData.CriticalDamage);
                    break;
            }

            AttackMostWanted();
        }

        /// <summary>
        /// Désactive l'envoi de la fenêtre de familier au joueur
        /// (un bot n'est pas un pet, ça évite des mises à jour inutiles).
        /// </summary>
        public override void UpdatePetWindow() { }
    }
}