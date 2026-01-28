namespace Raldi.NPCs
{
    public class MrBeast_StateBase : NpcState
    {
        public MrBeast beast;
        public NPC character;

        public MrBeast_StateBase(NPC chara, MrBeast mrBeast) : base(chara)
        {
            character = chara;
            beast = mrBeast;
        }

        public virtual void StartRedCircleChallenge(MrBeast beast) { }

        public virtual void OnEndChallenge(MrBeast beast, bool won) { }

        public virtual void PlayerEnteredCircle(PlayerManager pm, MrBeast beast) { }

        public virtual void SpecialFunction() { }

        public virtual ItemObject GetRandomItem(ItemManager itm) { return null; }
    }
}
